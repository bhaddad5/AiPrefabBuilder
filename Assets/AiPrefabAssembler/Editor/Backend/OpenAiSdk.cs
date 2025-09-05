using OpenAI.Chat;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Text.Json;
using System.Linq;
using UnityEditor;
using UnityEngine;
using log4net.Core;

namespace AiRequestBackend
{
	public static class OpenAISdk
	{
		private static ChatClient BuildClient(Model level) 
		{ 
			if(String.IsNullOrWhiteSpace(EditorPrefs.GetString("OPENAI_API_KEY")))
			{
				Debug.LogError("No API Key Provided!");
				return null;
			}

			string modelStr = "gpt-5";
			if (level == Model.micro)
				modelStr = "gpt-5-micro";
			else if (level == Model.mini)
				modelStr = "gpt-5-mini";

				return new ChatClient(model: modelStr, apiKey: EditorPrefs.GetString("OPENAI_API_KEY"));
		}

		public enum Model
		{
			micro,
			mini,
			standard,
		}

		public static async Task<string> AskAsync(List<string> systemPrompts, string userPrompt, Model model = Model.mini)
		{
			var client = BuildClient(model);

			if (client == null)
				return null;

			List<ChatMessage> prompts = new List<ChatMessage>();
			foreach (var systemPrompt in systemPrompts)
				prompts.Add(new SystemChatMessage(systemPrompt));
			prompts.Add(new UserChatMessage(userPrompt));

			var completion = await client.CompleteChatAsync(prompts);

			return completion.Value.Content[0].Text;
		}

		public static async Task<string> AskImagesAsync(string prompt, Dictionary<string, BinaryData> imageData, Model model = Model.mini)
		{
			var client = BuildClient(model);

			if (client == null)
				return null;

			List<ChatMessageContentPart> msgs = new List<ChatMessageContentPart>();
			msgs.Add(ChatMessageContentPart.CreateTextPart(prompt));
			foreach (var id in imageData)
			{
				msgs.Add(ChatMessageContentPart.CreateTextPart(id.Key));
				msgs.Add(ChatMessageContentPart.CreateImagePart(
					id.Value, // Base64-encoded bytes
					"image/jpg" // MIME type
				));
			}

			var message = new UserChatMessage(msgs);

			var completion = await client.CompleteChatAsync(
				new List<ChatMessage>()
				{
					message
				});

			return completion.Value.Content[0].Text;
		}

		public static async void AskContinuous(string apiKey, string prompt, Model model, Action<string> progressCallback, Action<string> finalCallback)
		{
			var client = BuildClient(model);

			if (client == null)
				return;

			progressCallback($"Prompt: {prompt}");

			var messages = new List<ChatMessage>
			{
				new SystemChatMessage("You are helping build Prefabs in Unity."),
				new UserChatMessage(prompt),
			};

			ChatTool getPartsMetadata = ChatTool.CreateFunctionTool(
				functionName: nameof(ToolsImplementation.GetPrefabsMetadata),
				functionDescription: "Retrieves additional metadata and context for specific prefabs",
				functionParameters: BinaryData.FromString("{\"type\": \"object\",\"properties\": {\"prefabs\": {\"type\": \"array\",\"description\": \"The list of parts needing metadata\", \"items\": { \"type\": \"string\" }, \"minItems\": 1}},\"required\": [ \"prefabs\" ]}"));

			ChatTool analyzeInstructions = ChatTool.CreateFunctionTool(
				functionName: nameof(ToolsImplementation.AnalyzeInstructions),
				functionDescription: "Try assembling parts together with instructions formatted as [assetName,pos:(x;y;z),euler:(x;y;z)] for each part. This will return a series of renderings of the object which you can visually analyze to see if it looks like what you are trying to build.",
				functionParameters: BinaryData.FromString("{\"type\": \"object\",\"properties\": {\"instructions\": {\"type\": \"string\",\"description\": \"The prefab creation instructions formatted as [assetName,pos:(x;y;z),euler:(x;y;z)] for each part.\"}},\"required\": [ \"instructions\" ]}"));

			ChatTool informUserOfCurrentReasoning = ChatTool.CreateFunctionTool(
				functionName: nameof(ToolsImplementation.InformUserOfCurrentReasoning),
				functionDescription: "Inform the user in plaintext of your current reasoning.",
				functionParameters: BinaryData.FromString("{\"type\": \"object\",\"properties\": {\"currentReasoning\": {\"type\": \"string\",\"description\": \"Plaintext for the user to understand your current thought process.\"}},\"required\": [ \"currentReasoning\" ]}"));

			var options = new ChatCompletionOptions
			{
				Tools = { getPartsMetadata, informUserOfCurrentReasoning, /*buildSubPrefab, analyzeInstructions*/ }
			};

			bool loop;
			do
			{
				loop = false;
				ChatCompletion completion = await client.CompleteChatAsync(messages, options);

				if (completion.FinishReason == ChatFinishReason.Stop)
				{
					// Normal assistant message
					messages.Add(new AssistantChatMessage(completion));
					finalCallback(completion.Content[0].Text);
				}
				else if (completion.FinishReason == ChatFinishReason.ToolCalls)
				{
					// Add assistant message (which contains tool calls) to history
					messages.Add(new AssistantChatMessage(completion));

					foreach (var call in completion.ToolCalls)
					{
						switch (call.FunctionName)
						{
							case nameof(ToolsImplementation.GetPrefabsMetadata):
								{
									var args = JsonDocument.Parse(call.FunctionArguments);

									progressCallback($"GetPartsMetadata Resq: {args.RootElement.GetProperty("parts").ToString()}");

									var partsArray = args.RootElement.GetProperty("parts").EnumerateArray().Select(e => e.GetString()).ToList();
									string responseToAi = ToolsImplementation.GetPrefabsMetadata(partsArray);

									progressCallback($"GetPartsMetadata Resp: {responseToAi}");

									messages.Add(new ToolChatMessage(call.Id, responseToAi));
									break;
								}

							case nameof(ToolsImplementation.BuildPrefabSubAssembly):
								{
									var args = JsonDocument.Parse(call.FunctionArguments);

									progressCallback($"Prefab Build Resq: {args.RootElement.GetProperty("instructions").ToString()}");

									var responseToAi = ToolsImplementation.BuildPrefabSubAssembly(args.RootElement.GetProperty("instructions").ToString());

									progressCallback($"Prefab Build Resp: {responseToAi}");

									List<ChatMessageContentPart> msgs = new List<ChatMessageContentPart>();
									msgs.Add(ChatMessageContentPart.CreateTextPart(responseToAi));

									messages.Add(new ToolChatMessage(call.Id, msgs));
									break;
								}
							case nameof(ToolsImplementation.AnalyzeInstructions):
								{
									var args = JsonDocument.Parse(call.FunctionArguments);

									progressCallback($"AnalyzeInstructions Resq: {args.RootElement.GetProperty("instructions").ToString()}");

									var responseToAi = ToolsImplementation.AnalyzeInstructions(args.RootElement.GetProperty("instructions").ToString());

									progressCallback($"AnalyzeInstructions Resp: {responseToAi.Info}");

									List<ChatMessageContentPart> msgs = new List<ChatMessageContentPart>();
									msgs.Add(ChatMessageContentPart.CreateTextPart(responseToAi.Info));
									foreach (var id in responseToAi.Renders)
									{
										msgs.Add(ChatMessageContentPart.CreateTextPart(id.Key));
										msgs.Add(ChatMessageContentPart.CreateImagePart(
											id.Value, // Base64-encoded bytes
											"image/jpg" // MIME type
										));
									}

									messages.Add(new ToolChatMessage(call.Id, msgs));
									break;
								}
							default:
								messages.Add(new ToolChatMessage(call.Id, "Tool not implemented."));
								break;
						}
					}

					// We satisfied tool calls; loop to let the model use the tool results
					loop = true;
				}
			}
			while (loop);
		}
	}
}