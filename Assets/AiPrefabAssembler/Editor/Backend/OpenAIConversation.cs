using OpenAI.Chat;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using UnityEditor;
using UnityEngine;

namespace AiRequestBackend
{
    public class OpenAIConversation
    {
		public struct ChatHistoryEntry
		{
			public bool IsFromUser;
			public string Text;

			public ChatHistoryEntry(bool isFromUser, string text)
			{
				IsFromUser = isFromUser;
				Text = text;
			}
		}

		public bool IsProcessingMsg;
		public string CurrentThinkingStatus;

		public event Action<ChatHistoryEntry> ChatMsgAdded;
		public event Action<bool> IsProcessingMsgChanged;

		private ChatClient client;

		private ChatCompletionOptions options = new ChatCompletionOptions();

		private List<ChatMessage> currentConversation = new List<ChatMessage>();

		private List<ITool> tools;

		public OpenAIConversation(List<string> systemPrompts, OpenAISdk.Model model, List<ITool> tools)
        {
			client = OpenAISdk.BuildClient(model);

			this.tools = tools;

			foreach (var systemPrompt in systemPrompts)
				currentConversation.Add(new SystemChatMessage(systemPrompt));

			foreach (var tool in tools)
			{
				string descr = $"{tool.FunctionDescription}" + "Available Commands: ";
				foreach (var command in tool.AvailableCommands)
					descr += command.CommandFormattingString;

				BinaryData paramsStr = BinaryData.FromString("{\"type\": \"object\",\"properties\": {\"commands\": {\"type\": \"string\",\"description\": \"All of the Commands you wish to execute.\"}},\"required\": [ \"commands\" ]}");

				options.Tools.Add(ChatTool.CreateFunctionTool(functionName: tool.FunctionName, functionDescription: descr, functionParameters: paramsStr));
			}
		}

        public void SendMsg(string msg, List<string> transientContextMsgs)
        {
			if (IsProcessingMsg)
			{
				Debug.LogError("Cannot send new chat msg while still processing an old one!");
				return;
			}

			//Debug.Log($"Sending Msg: {msg}");

			currentConversation.Add(new UserChatMessage(msg));

			ChatMsgAdded?.Invoke(new ChatHistoryEntry(true, msg));

			ProcessCurrentConversation(transientContextMsgs);
		}

		//We don't wanna overload the model with context, so we don't preserve the tool-call back-and-forth between queries.
		public async void ProcessCurrentConversation(List<string> transientContextMsgs)
		{
			IsProcessingMsg = true;
			IsProcessingMsgChanged?.Invoke(IsProcessingMsg);

			CurrentThinkingStatus = "Thinking...";

			List<ChatMessage> tmpConversation = new List<ChatMessage>(currentConversation);

			foreach(var context in transientContextMsgs)
			{
				tmpConversation.Add(new SystemChatMessage(context));
			}

			bool loop = true;
			while (loop)
			{
				loop = false;
				ChatCompletion completion = await client.CompleteChatAsync(tmpConversation, options);

				if (completion.FinishReason == ChatFinishReason.Stop)
				{
					//Debug.Log($"Recieved Msg Response: {completion.Content[0].Text}");

					// Normal assistant message
					currentConversation.Add(new AssistantChatMessage(completion));
					IsProcessingMsg = false;
					IsProcessingMsgChanged?.Invoke(IsProcessingMsg);

					ChatMsgAdded?.Invoke(new ChatHistoryEntry(false, completion.Content[0].Text));
				}
				else if (completion.FinishReason == ChatFinishReason.ToolCalls)
				{
					// Add assistant message (which contains tool calls) to history
					tmpConversation.Add(new AssistantChatMessage(completion));

					foreach (var call in completion.ToolCalls)
					{
						//Debug.Log($"Calling tool {call.FunctionName} - {JsonDocument.Parse(call.FunctionArguments).RootElement.GetProperty("commands")}");

						var toolToUse = tools.FirstOrDefault(t => t.FunctionName == call.FunctionName);

						if (toolToUse == null)
						{
							Debug.LogError($"Tool {call.FunctionName} not implemented!");
							tmpConversation.Add(new ToolChatMessage(call.Id, "Tool not implemented."));
							continue;
						}

						CurrentThinkingStatus = $"{toolToUse.ActionProgressDescription}...";

						var args = JsonDocument.Parse(call.FunctionArguments);

						string responseToAi = toolToUse.Execute(args.RootElement.GetProperty("commands").ToString());

						tmpConversation.Add(new ToolChatMessage(call.Id, responseToAi));
					}

					// We satisfied tool calls; loop to let the model use the tool results
					loop = true;
				}
			}
		}
	}
}