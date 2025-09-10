using Anthropic.SDK;
using Anthropic.SDK.Common;
using Anthropic.SDK.Messaging;
using System;
using System.Collections.Generic;
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

					var paramSchema = new JsonObject
					{
						["type"] = "string"
					};

					if (!string.IsNullOrWhiteSpace(p.Description))
						paramSchema["description"] = p.Description;

					properties[p.Name] = paramSchema;				}
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

			//Debug.Log($"Function Exposed: {command.CommandName}, descr = {command.CommandDescription}, schema = {schema.ToString()}");

			// Return Function instead of Tool - this is the correct type for current SDK
			return new Function(command.CommandName, command.CommandDescription ?? "", schema);
		}

		private static ToolResultContent HandleToolCall(ICommand command, ToolUseContent toolUse)
		{
			var args = ParseArgs(toolUse.Input);

			string argsStr = "";
			foreach (var arg in args)
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

		private static Dictionary<string, string> ParseArgs(JsonNode input)
		{
			if (input == null) return new Dictionary<string, string>();

			var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

			if (input is JsonObject obj)
			{
				foreach (var prop in obj)
				{
					if (prop.Key == null) continue;

					dict[prop.Key.ToLowerInvariant()] = prop.Value?.GetValue<object>() switch
					{
						string str => str.ToLowerInvariant(),
						null => "",
						_ => prop.Value.ToString()
					};
				}
			}

			return dict;
		}

		#endregion
	}
}