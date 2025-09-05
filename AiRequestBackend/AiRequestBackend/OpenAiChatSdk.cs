using OpenAI.Chat;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Text.Json;
using System.Linq;

namespace AiRequestBackend
{
	public static class OpenAIChatSdk
	{
		private static ChatClient BuildClient(string apiKey) { return new ChatClient(model: "gpt-5-mini", apiKey: apiKey); }
		
		public enum ModelLevel
		{
			micro,
			mini,
			standard,
		}

		public static async Task<string> AskAsync(string apiKey, List<string> systemPrompts, string userPrompt, ModelLevel level = ModelLevel.mini)
		{
			var client = BuildClient(apiKey);

			List<ChatMessage> prompts = new List<ChatMessage>();
			foreach (var systemPrompt in systemPrompts)
				prompts.Add(new SystemChatMessage(systemPrompt));
			prompts.Add(new UserChatMessage(userPrompt));

			var completion = await client.CompleteChatAsync(prompts);

			return completion.Value.Content[0].Text;
		}

		public static async Task<string> AskImagesAsync(string apiKey, string prompt, Dictionary<string, BinaryData> imageData)
		{
			var client = BuildClient(apiKey);

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

		public static void AskContinuous(string apiKey, string prompt, IToolsImplementation impl, ModelLevel level, Action<string> progressCallback, Action<string> finalCallback)
		{
			AskContinuousImpl(apiKey, prompt, impl, progressCallback, finalCallback);
		}

		private static async void AskContinuousImpl(string apiKey, string prompt, IToolsImplementation impl, Action<string> progressCallback, Action<string> finalCallback)
		{
			var client = BuildClient(apiKey);

			progressCallback($"Prompt: {prompt}");

			var messages = new List<ChatMessage>
			{
				new SystemChatMessage("You are helping build Prefabs in Unity."),
				new UserChatMessage(prompt),
			};

			ChatTool getPartsMetadata = ChatTool.CreateFunctionTool(
				functionName: nameof(Tools.GetPartsMetadata),
				functionDescription: "Retrieves additional metadata and context for specific parts",
				functionParameters: BinaryData.FromString("{\"type\": \"object\",\"properties\": {\"parts\": {\"type\": \"array\",\"description\": \"The list of parts needing metadata\", \"items\": { \"type\": \"string\" }, \"minItems\": 1}},\"required\": [ \"parts\" ]}"));

			ChatTool analyzeInstructions = ChatTool.CreateFunctionTool(
				functionName: nameof(Tools.AnalyzeInstructions),
				functionDescription: "Try assembling parts together with instructions formatted as [assetName,pos:(x;y;z),euler:(x;y;z)] for each part. This will return a series of renderings of the object which you can visually analyze to see if it looks like what you are trying to build.",
				functionParameters: BinaryData.FromString("{\"type\": \"object\",\"properties\": {\"instructions\": {\"type\": \"string\",\"description\": \"The prefab creation instructions formatted as [assetName,pos:(x;y;z),euler:(x;y;z)] for each part.\"}},\"required\": [ \"instructions\" ]}"));

			ChatTool informUserOfCurrentReasoning = ChatTool.CreateFunctionTool(
				functionName: nameof(Tools.InformUserOfCurrentReasoning),
				functionDescription: "Inform the user in plaintext of your current reasoning.",
				functionParameters: BinaryData.FromString("{\"type\": \"object\",\"properties\": {\"currentReasoning\": {\"type\": \"string\",\"description\": \"Plaintext for the user to understand your current thought process.\"}},\"required\": [ \"currentReasoning\" ]}"));
			/*
			ChatTool buildSubPrefab = ChatTool.CreateFunctionTool(
				functionName: nameof(Tools.BuildPrefabSubAssembly),
				functionDescription: "Build a prefab sub-assembly with instructions formatted as [assetName,pos:(x;y;z),euler:(x;y;z)] for each part.",
				functionParameters: BinaryData.FromString("{\"type\": \"object\",\"properties\": {\"instructions\": {\"type\": \"string\",\"description\": \"The prefab creation instructions formatted as [assetName,pos:(x;y;z),euler:(x;y;z)] for each part.\"}},\"required\": [ \"instructions\" ]}"));
			*/
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
							case nameof(Tools.GetPartsMetadata):
								{
									var args = JsonDocument.Parse(call.FunctionArguments);

									progressCallback($"GetPartsMetadata Resq: {args.RootElement.GetProperty("parts").ToString()}");

									var partsArray = args.RootElement.GetProperty("parts").EnumerateArray().Select(e => e.GetString()).ToList();
									string responseToAi = Tools.GetPartsMetadata(impl, partsArray);

									progressCallback($"GetPartsMetadata Resp: {responseToAi}");

									messages.Add(new ToolChatMessage(call.Id, responseToAi));
									break;
								}

							case nameof(Tools.BuildPrefabSubAssembly):
								{
									var args = JsonDocument.Parse(call.FunctionArguments);

									progressCallback($"Prefab Build Resq: {args.RootElement.GetProperty("instructions").ToString()}");

									var responseToAi = Tools.BuildPrefabSubAssembly(impl, args.RootElement.GetProperty("instructions").ToString());

									progressCallback($"Prefab Build Resp: {responseToAi}");

									List<ChatMessageContentPart> msgs = new List<ChatMessageContentPart>();
									msgs.Add(ChatMessageContentPart.CreateTextPart(responseToAi));

									messages.Add(new ToolChatMessage(call.Id, msgs));
									break;
								}
							case nameof(Tools.AnalyzeInstructions):
								{
									var args = JsonDocument.Parse(call.FunctionArguments);

									progressCallback($"AnalyzeInstructions Resq: {args.RootElement.GetProperty("instructions").ToString()}");

									var responseToAi = Tools.AnalyzeInstructions(impl, args.RootElement.GetProperty("instructions").ToString());

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