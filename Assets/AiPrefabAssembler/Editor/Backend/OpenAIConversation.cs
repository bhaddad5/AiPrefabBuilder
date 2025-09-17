using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Globalization;
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

		public static ChatClient BuildClient(string modelId)
		{
			if (String.IsNullOrWhiteSpace(EditorPrefs.GetString("OPENAI_API_KEY")))
			{
				Debug.LogError("No API Key Provided!");
				return null;
			}

			return new ChatClient(model: modelId, apiKey: EditorPrefs.GetString("OPENAI_API_KEY"));
		}

		public void InitConversation(string modelId, List<string> systemPrompts, List<ICommand> tools)
		{
			client = BuildClient(modelId);

			this.tools = tools;

			foreach (var systemPrompt in systemPrompts)
				currentConversation.Add(new SystemChatMessage(systemPrompt));

			foreach (var tool in tools)
			{
				string descr = $"{tool.CommandDescription}";
				options.Tools.Add(ToChatTool(tool));
			}
		}

		public void SendMsg(List<UserToAiMsg> msgs, List<string> transientContextMsgs)
        {
			if (IsProcessingMsg)
			{
				Debug.LogError("Cannot send new chat msg while still processing an old one!");
				return;
			}

			var chatMsgs = ToChatContent(msgs);

			currentConversation.Add(new UserChatMessage(chatMsgs));

			foreach(var msg in msgs)
			{
				if (msg is UserToAiMsgText t)
				{
					Debug.Log($"Sending Msg: {t.Text}");
					ChatMsgAdded?.Invoke(new IConversation.ChatHistoryEntry(true, t.Text));
				}
				else if (msg is UserToAiMsgImage i)
				{
					Debug.Log($"Sending Msg: {i.Image.name}");
				}
			}

			foreach (var tcMsg in transientContextMsgs)
				Debug.Log($"Context Msg: {tcMsg}");

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

					List<ICommand> toolsUsed = new List<ICommand>();

					foreach (var call in completion.ToolCalls)
					{
						var toolToUse = tools.FirstOrDefault(t => t.CommandName == call.FunctionName);

						if (toolToUse == null)
						{
							Debug.LogError($"Tool {call.FunctionName} not implemented!");
							tmpConversation.Add(new ToolChatMessage(call.Id, $"Tool {call.FunctionName} not implemented."));
							continue;
						}

						toolsUsed.Add(toolToUse);

						CurrentThinkingStatus = $"Calling: {toolToUse.CommandName}";

						//TODO: how to respond to successful no-response calls
						var toolMsg = HandleToolCall(toolToUse, call);
						tmpConversation.Add(toolMsg);
					}

					// We satisfied tool calls; loop to let the model use the tool results
					loop = toolsUsed.Any(t => !t.EndConversation);
				}
			}
		}

		#region ICommand Adapters

		private static JsonObject BuildParamSchema(Parameter p)
		{
			switch (p.Type)
			{
				case Parameter.ParamType.Int:
					return new JsonObject
					{
						["type"] = "integer",
						["description"] = string.IsNullOrWhiteSpace(p.Description) ? "" : p.Description
					};

				case Parameter.ParamType.Vector3:
					// Represent a Unity-style Vector3 as an object { x:number, y:number, z:number }
					var vecProps = new JsonObject
					{
						["x"] = new JsonObject { ["type"] = "number" },
						["y"] = new JsonObject { ["type"] = "number" },
						["z"] = new JsonObject { ["type"] = "number" }
					};

					var vecReq = new JsonArray { "x", "y", "z" };

					var vec = new JsonObject
					{
						["type"] = "object",
						["properties"] = vecProps,
						["required"] = vecReq,
						["additionalProperties"] = false,
						["description"] = string.IsNullOrWhiteSpace(p.Description) ? "" : p.Description
					};

					return vec;

				case Parameter.ParamType.StringList:
					var arr = new JsonObject
					{
						["type"] = "array",
						["items"] = new JsonObject { ["type"] = "string" },
						["description"] = string.IsNullOrWhiteSpace(p.Description) ? "" : p.Description
					};

					return arr;

				case Parameter.ParamType.String:
				default:
					return new JsonObject
					{
						["type"] = "string",
						["description"] = string.IsNullOrWhiteSpace(p.Description) ? "" : p.Description
					};
			}
		}

		private static ChatTool ToChatTool(ICommand command)
		{
			if (command == null) throw new ArgumentNullException(nameof(command));
			if (string.IsNullOrWhiteSpace(command.CommandName))
				throw new ArgumentException("CommandName is required.", nameof(command));

			// Build JSON Schema for parameters
			var properties = new JsonObject();

			if (command.Parameters != null)
			{
				foreach (var p in command.Parameters)
				{
					if (p == null || string.IsNullOrWhiteSpace(p.Name)) continue;

					properties[p.Name] = BuildParamSchema(p);
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

			Debug.Log($"Function: {command.CommandName}, descr={command.CommandDescription}, schema={schema.ToString()}");

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
			var args = ParseTypedArgs(call.FunctionArguments, command.Parameters);

			string argsStr = "";
			foreach(var arg in args.Values)
			{
				argsStr += $"{arg.Key}:{arg.Value},";
			}
			if (argsStr.EndsWith(','))
				argsStr = argsStr.Substring(0, argsStr.Length - 1);

			Debug.Log($"Calling tool {call.FunctionName}({argsStr})");

			var result = command.ParseArgsAndExecute(args);

			var respContent = ToChatContent(result);

			if (respContent.Count == 0)
				respContent.Add(ChatMessageContentPart.CreateTextPart("Done"));

			Debug.Log($"Tool Response: {String.Join(',', result)}");

			return new ToolChatMessage(call.Id, respContent);
		}

		private static List<ChatMessageContentPart> ToChatContent(List<UserToAiMsg> msgs)
		{
			List<ChatMessageContentPart> res = new List<ChatMessageContentPart>();
			foreach (var commandRes in msgs)
			{
				if (commandRes is UserToAiMsgText t)
					res.Add(ChatMessageContentPart.CreateTextPart(t.Text));
				else if (commandRes is UserToAiMsgImage i)
					res.Add(ChatMessageContentPart.CreateImagePart(
					BinaryData.FromBytes(i.GetImageBytes()), // Base64-encoded bytes
					"image/jpg" // MIME type
				));
			}
			return res;
		}

		// Example: produce typed values (int, Vector3, string) from JsonNode
		private static TypedArgs ParseTypedArgs(BinaryData data, IEnumerable<Parameter> paramDefs)
		{
			JsonNode input = JsonNode.Parse(data.ToString());

			var result = new TypedArgs();
			if (input is not JsonObject obj) return result;

			// index param defs by name
			var byName = paramDefs?.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase)
						 ?? new Dictionary<string, Parameter>(StringComparer.OrdinalIgnoreCase);

			foreach (var (key, node) in obj)
			{
				if (string.IsNullOrWhiteSpace(key)) continue;

				object value = node;

				if (byName.TryGetValue(key, out var def))
				{
					switch (def.Type)
					{
						case Parameter.ParamType.Int:
							if (node is JsonValue jv && jv.TryGetValue<long>(out var l))
								value = (int)l;
							break;

						case Parameter.ParamType.Vector3:
							if (node is JsonObject vo &&
								vo.TryGetPropertyValue("x", out var nx) &&
								vo.TryGetPropertyValue("y", out var ny) &&
								vo.TryGetPropertyValue("z", out var nz) &&
								float.TryParse(nx.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var fx) &&
								float.TryParse(ny.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var fy) &&
								float.TryParse(nz.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var fz))
							{
								value = new Vector3(fx, fy, fz);
							}
							break;

						case Parameter.ParamType.StringList:
							if (node is JsonArray ja)
							{
								var stringList = new List<string>();
								foreach (var item in ja)
								{
									if (item is JsonValue slv && slv.TryGetValue<string>(out var sl))
										stringList.Add(sl);
									else
										stringList.Add(item?.ToString() ?? "");
								}
								value = stringList;
							}
							break;

						case Parameter.ParamType.String:
						default:
							if (node is JsonValue sv && sv.TryGetValue<string>(out var s))
								value = s;
							else
								value = node.ToJsonString();
							break;
					}
				}

				result.Values[key] = value;
			}

			return result;
		}

		#endregion
	}
}