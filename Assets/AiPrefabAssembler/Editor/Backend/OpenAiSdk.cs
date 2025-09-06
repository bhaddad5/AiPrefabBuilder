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
		public enum Model
		{
			GPT5Micro,
			GPT5mini,
			GPT5standard,
		}

		public static ChatClient BuildClient(Model level) 
		{ 
			if(String.IsNullOrWhiteSpace(EditorPrefs.GetString("OPENAI_API_KEY")))
			{
				Debug.LogError("No API Key Provided!");
				return null;
			}

			string modelStr = "gpt-5";
			if (level == Model.GPT5Micro)
				modelStr = "gpt-5-micro";
			else if (level == Model.GPT5mini)
				modelStr = "gpt-5-mini";

				return new ChatClient(model: modelStr, apiKey: EditorPrefs.GetString("OPENAI_API_KEY"));
		}

		

		public static async Task<string> AskAsync(List<string> systemPrompts, string userPrompt, Model model = Model.GPT5mini)
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

		public static async Task<string> AskImagesAsync(string prompt, Dictionary<string, BinaryData> imageData, Model model = Model.GPT5mini)
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

		public static async void AskContinuous(List<string> systemPrompts, string userPrompt, Model model, List<ITool> tools, Action<string> finalCallback)
		{
			var client = BuildClient(model);

			if (client == null)
				return;

			var messages = new List<ChatMessage>();
			foreach (var systemPrompt in systemPrompts)
				messages.Add(new SystemChatMessage(systemPrompt));
			messages.Add(new UserChatMessage(userPrompt));

			//Build Tools Requests
			var options = new ChatCompletionOptions();
			foreach (var tool in tools)
			{
				string descr = $"{tool.FunctionDescription}" + "Available Commands: ";
				foreach (var command in tool.AvailableCommands)
					descr += command.CommandFormattingString;

				BinaryData paramsStr = BinaryData.FromString("{\"type\": \"object\",\"properties\": {\"commands\": {\"type\": \"string\",\"description\": \"All of the Commands you wish to execute.\"}},\"required\": [ \"commands\" ]}");

				options.Tools.Add(ChatTool.CreateFunctionTool(functionName: tool.FunctionName, functionDescription: descr, functionParameters: paramsStr));
			}

			bool loop = true;
			while (loop)
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
						Debug.Log($"Calling tool {call.FunctionName} - {JsonDocument.Parse(call.FunctionArguments).RootElement.GetProperty("commands")}");

						var toolToUse = tools.FirstOrDefault(t => t.FunctionName == call.FunctionName);

						if(toolToUse == null)
						{
							Debug.LogError($"Tool {call.FunctionName} not implemented!");
							messages.Add(new ToolChatMessage(call.Id, "Tool not implemented."));
							continue;
						}

						var args = JsonDocument.Parse(call.FunctionArguments);

						string responseToAi = toolToUse.Execute(args.RootElement.GetProperty("commands").ToString());

						messages.Add(new ToolChatMessage(call.Id, responseToAi));
					}

					// We satisfied tool calls; loop to let the model use the tool results
					loop = true;
				}
			}
		}
	}
}