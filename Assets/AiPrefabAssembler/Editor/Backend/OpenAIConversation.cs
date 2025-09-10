using OpenAI.Chat;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using UnityEditor;
using UnityEngine;

namespace AiRequestBackend
{
    public class OpenAIConversation : IConversation
	{
		public bool IsProcessingMsg { get; private set; }
		public string CurrentThinkingStatus { get; private set; }

		public event Action<IConversation.ChatHistoryEntry> ChatMsgAdded;
		public event Action<bool> IsProcessingMsgChanged;

		private ChatClient client;
		private ChatCompletionOptions options = new ChatCompletionOptions();
		private List<ChatMessage> currentConversation = new List<ChatMessage>();
		private List<ICommand> tools;

		public void InitConversation(string modelId, List<string> systemPrompts, List<ICommand> tools)
		{
			client = OpenAISdk.BuildClient(modelId);

			this.tools = tools;

			foreach (var systemPrompt in systemPrompts)
				currentConversation.Add(new SystemChatMessage(systemPrompt));

			foreach (var tool in tools)
			{
				string descr = $"{tool.CommandDescription}";
				options.Tools.Add(ToChatTool(tool));
			}
		}

		public void SendMsg(string msg, List<string> transientContextMsgs)
        {
			if (IsProcessingMsg)
			{
				Debug.LogError("Cannot send new chat msg while still processing an old one!");
				return;
			}

			Debug.Log($"Sending Msg: {msg}");

			currentConversation.Add(new UserChatMessage(msg));

			ChatMsgAdded?.Invoke(new IConversation.ChatHistoryEntry(true, msg));

			ProcessCurrentConversation(transientContextMsgs);
		}

		//We don't wanna overload the model with context, so we don't preserve the tool-call back-and-forth between queries.
		private async void ProcessCurrentConversation(List<string> transientContextMsgs)
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
					Debug.Log($"Recieved Msg Response: {completion.Content[0].Text}");

					// Normal assistant message
					currentConversation.Add(new AssistantChatMessage(completion));
					IsProcessingMsg = false;
					IsProcessingMsgChanged?.Invoke(IsProcessingMsg);

					ChatMsgAdded?.Invoke(new IConversation.ChatHistoryEntry(false, completion.Content[0].Text));
				}
				else if (completion.FinishReason == ChatFinishReason.ToolCalls)
				{
					// Add assistant message (which contains tool calls) to history
					tmpConversation.Add(new AssistantChatMessage(completion));

					foreach (var call in completion.ToolCalls)
					{
						var toolToUse = tools.FirstOrDefault(t => t.CommandName == call.FunctionName);

						if (toolToUse == null)
						{
							Debug.LogError($"Tool {call.FunctionName} not implemented!");
							tmpConversation.Add(new ToolChatMessage(call.Id, $"Tool {call.FunctionName} not implemented."));
							continue;
						}
						CurrentThinkingStatus = "TODO: FILL IN!";//$"{toolToUse.ActionProgressDescription}...";

						//TODO: how to respond to successful no-response calls
						var toolMsg = HandleToolCall(toolToUse, call);
						tmpConversation.Add(toolMsg);
					}

					// We satisfied tool calls; loop to let the model use the tool results
					loop = true;
				}
			}
		}

		#region ICommand Adapters

		private static ChatTool ToChatTool(ICommand command)
		{
			if (command == null) throw new ArgumentNullException(nameof(command));
			if (string.IsNullOrWhiteSpace(command.CommandName))
				throw new ArgumentException("CommandName is required.", nameof(command));

			// --- Build JSON Schema for parameters ---
			// {
			//   "type": "object",
			//   "properties": { "<name>": { "type": "string", "description": "..." }, ... },
			//   "required": ["..."],
			//   "additionalProperties": false
			// }
			var properties = new JsonObject();

			if (command.Parameters != null)
			{
				foreach (var p in command.Parameters)
				{
					if (p == null || string.IsNullOrWhiteSpace(p.Name)) continue;

					var paramSchema = new JsonObject
					{
						["type"] = "string"
					};

					if (!string.IsNullOrWhiteSpace(p.Description))
						paramSchema["description"] = p.Description;

					properties[p.Name] = paramSchema;
				}
			}

			var schema = new JsonObject
			{
				["type"] = "object",
				["properties"] = properties,
				["additionalProperties"] = false
			};

			var requiredNames = command.Parameters?
				.Where(p => p != null && p.Required && !string.IsNullOrWhiteSpace(p.Name))
				.Select(p => p.Name)
				.ToArray();

			if (requiredNames != null && requiredNames.Length > 0)
			{
				var req = new JsonArray();
				foreach (var name in requiredNames) req.Add(name);
				schema["required"] = req;
			}

			// The SDK expects the JSON schema as BinaryData
			var parametersJson = BinaryData.FromString(schema.ToJsonString());

			return ChatTool.CreateFunctionTool(
				functionName: command.CommandName,
				functionDescription: string.IsNullOrWhiteSpace(command.CommandDescription) ? null : command.CommandDescription,
				functionParameters: parametersJson
			);
		}


		private static ToolChatMessage HandleToolCall(ICommand command, ChatToolCall call)
		{
			var args = ParseArgs(call.FunctionArguments);

			string argsStr = "";
			foreach(var arg in args)
			{
				argsStr += $"{arg.Key}:{arg.Value},";
			}
			if (argsStr.EndsWith(','))
				argsStr = argsStr.Substring(0, argsStr.Length - 1);

			Debug.Log($"Calling tool {call.FunctionName}({argsStr})");

			string result = command.ParseArgsAndExecute(args);

			Debug.Log($"Tool Response: {result}");

			return new ToolChatMessage(call.Id, result);
		}

		private static Dictionary<string, string> ParseArgs(BinaryData data)
		{
			if (data == null) return new();

			string json = data.ToString();                // BinaryData -> UTF-8 string
			if (string.IsNullOrWhiteSpace(json)) return new();

			using var doc = JsonDocument.Parse(json);
			if (doc.RootElement.ValueKind != JsonValueKind.Object) return new();

			var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (var prop in doc.RootElement.EnumerateObject())
			{
				dict[prop.Name?.ToLowerInvariant()] = prop.Value.ValueKind switch
				{
					JsonValueKind.String => prop.Value.GetString()?.ToLowerInvariant() ?? "",
					JsonValueKind.Null => "",
					// For numbers, bools, arrays, objects: keep raw JSON so ICommand gets *something*.
					_ => prop.Value.GetRawText()
				};
			}
			return dict;
		}

		#endregion
	}
}