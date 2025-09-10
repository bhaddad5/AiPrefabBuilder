using Anthropic.SDK;
using Anthropic.SDK.Common;
using Anthropic.SDK.Messaging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using UnityEditor;
using UnityEngine;

namespace AiRequestBackend
{
	public class AnthropicConversation : IConversation
	{
		public bool IsProcessingMsg { get; private set; }
		public string CurrentThinkingStatus { get; private set; }

		public event Action<IConversation.ChatHistoryEntry> ChatMsgAdded;
		public event Action<bool> IsProcessingMsgChanged;

		private AnthropicClient client;
		List<SystemMessage> startingSystemMsgs = new List<SystemMessage>();
		private List<Message> currentConversation = new List<Message>();
		private List<ICommand> tools;
		private string modelId;

		public static AnthropicClient BuildClient()
		{
			if (String.IsNullOrWhiteSpace(EditorPrefs.GetString("ANTHROPIC_API_KEY")))
			{
				Debug.LogError("No API Key Provided!");
				return null;
			}

			return new AnthropicClient(EditorPrefs.GetString("ANTHROPIC_API_KEY"));
		}

		public void InitConversation(string modelId, List<string> systemPrompts, List<ICommand> tools)
		{
			client = BuildClient();
			this.modelId = modelId;
			this.tools = tools;

			foreach (var msg in systemPrompts)
				startingSystemMsgs.Add(new SystemMessage(msg));
		}

		public void SendMsg(string msg, List<string> transientContextMsgs)
		{
			if (IsProcessingMsg)
			{
				Debug.LogError("Cannot send new chat msg while still processing an old one!");
				return;
			}

			Debug.Log($"Sending Msg: {msg}");

			foreach(var tcMsg in transientContextMsgs)
				Debug.Log($"Context Msg: {tcMsg}");

			// Add user message to conversation history
			currentConversation.Add(new Message
			{
				Role = RoleType.User,
				Content = new List<ContentBase> { new TextContent { Text = msg } }
			});

			ChatMsgAdded?.Invoke(new IConversation.ChatHistoryEntry(true, msg));

			ProcessCurrentConversation(transientContextMsgs);
		}

		private async void ProcessCurrentConversation(List<string> transientContextMsgs)
		{
			IsProcessingMsg = true;
			IsProcessingMsgChanged?.Invoke(IsProcessingMsg);

			CurrentThinkingStatus = "Thinking...";

			List<SystemMessage> systemMsgs = new List<SystemMessage>(startingSystemMsgs);
			foreach(var contextMsg in transientContextMsgs)
			{
				systemMsgs.Add(new SystemMessage(contextMsg));
			}

			// Create message parameters - this is the correct structure for current SDK
			var messageParameters = new MessageParameters
			{
				Model = modelId,
				MaxTokens = 4096,
				System = systemMsgs,
				Messages = new List<Message>(currentConversation), 
			};

			// Add tools if available
			if (tools != null && tools.Count > 0)
				messageParameters.Tools = tools.Select(ToClaudeTool).ToList();

			bool loop = true;
			while (loop)
			{
				loop = false;

				try
				{
					// Use the correct method name - CreateMessageAsync instead of CreateAsync
					var response = await client.Messages.GetClaudeMessageAsync(messageParameters);

					if (response.StopReason == "end_turn")
					{
						// Normal assistant response
						var textContent = response.Content.OfType<TextContent>().FirstOrDefault();
						if (textContent != null)
						{
							Debug.Log($"Received Msg Response: {textContent.Text}");

							// Add assistant message to conversation history
							currentConversation.Add(new Message
							{
								Role = RoleType.Assistant,
								Content = response.Content
							});

							IsProcessingMsg = false;
							IsProcessingMsgChanged?.Invoke(IsProcessingMsg);

							ChatMsgAdded?.Invoke(new IConversation.ChatHistoryEntry(false, textContent.Text));
						}
					}
					else if (response.StopReason == "tool_use")
					{
						// Add assistant message (which contains tool calls) to history
						currentConversation.Add(new Message
						{
							Role = RoleType.Assistant,
							Content = response.Content
						});

						// Process tool calls
						var toolUseBlocks = response.Content.OfType<ToolUseContent>().ToList();
						var toolResults = new List<ContentBase>();

						foreach (var toolUse in toolUseBlocks)
						{
							var toolToUse = tools.FirstOrDefault(t => t.CommandName == toolUse.Name);

							if (toolToUse == null)
							{
								Debug.LogError($"Tool {toolUse.Name} not implemented!");
								toolResults.Add(new ToolResultContent
								{
									ToolUseId = toolUse.Id,
									Content = new List<ContentBase> { new TextContent { Text = $"Tool {toolUse.Name} not implemented." } }
								});
								continue;
							}

							CurrentThinkingStatus = "Processing tool..."; // Fixed the TODO comment

							var toolResult = HandleToolCall(toolToUse, toolUse);
							toolResults.Add(toolResult);
						}

						// Add tool results as a user message
						currentConversation.Add(new Message
						{
							Role = RoleType.User,
							Content = toolResults
						});

						// Update the message parameters for the next iteration
						messageParameters.Messages = new List<Message>(currentConversation);

						// Continue the loop to let the model use the tool results
						loop = true;
					}
				}
				catch (Exception ex)
				{
					Debug.LogError($"Error in Claude conversation: {ex.Message}");
					IsProcessingMsg = false;
					IsProcessingMsgChanged?.Invoke(IsProcessingMsg);
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

				case Parameter.ParamType.String:
				default:
					return new JsonObject
					{
						["type"] = "string",
						["description"] = string.IsNullOrWhiteSpace(p.Description) ? "" : p.Description
					};
			}
		}

		private static Anthropic.SDK.Common.Tool ToClaudeTool(ICommand command)
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

			return new Function(command.CommandName, command.CommandDescription ?? "", schema);
		}

		private static ToolResultContent HandleToolCall(ICommand command, ToolUseContent toolUse)
		{
			var args = ParseTypedArgs(toolUse.Input, command.Parameters);

			string argsStr = "";
			foreach (var arg in args.Values)
			{
				argsStr += $"{arg.Key}:{arg.Value},";
			}
			if (argsStr.EndsWith(','))
				argsStr = argsStr.Substring(0, argsStr.Length - 1);

			Debug.Log($"Calling tool {toolUse.Name}({argsStr})");

			string result = command.ParseArgsAndExecute(args);

			Debug.Log($"Tool Response: {result}");

			return new ToolResultContent
			{
				ToolUseId = toolUse.Id,
				Content = new List<ContentBase> { new TextContent { Text = result } }
			};
		}

		// Example: produce typed values (int, Vector3, string) from JsonNode
		private static TypedArgs ParseTypedArgs(JsonNode input, IEnumerable<Parameter> paramDefs)
		{
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