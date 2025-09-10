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
		

		public static ChatClient BuildClient(string modelId) 
		{ 
			if(String.IsNullOrWhiteSpace(EditorPrefs.GetString("OPENAI_API_KEY")))
			{
				Debug.LogError("No API Key Provided!");
				return null;
			}

				return new ChatClient(model: modelId, apiKey: EditorPrefs.GetString("OPENAI_API_KEY"));
		}

		

		public static async Task<string> AskAsync(List<string> systemPrompts, string userPrompt, string modelId = "gpt-5")
		{
			var client = BuildClient(modelId);

			if (client == null)
				return null;

			List<ChatMessage> prompts = new List<ChatMessage>();
			foreach (var systemPrompt in systemPrompts)
				prompts.Add(new SystemChatMessage(systemPrompt));
			prompts.Add(new UserChatMessage(userPrompt));

			var completion = await client.CompleteChatAsync(prompts);

			return completion.Value.Content[0].Text;
		}

		public static async Task<string> AskImagesAsync(string prompt, Dictionary<string, BinaryData> imageData, string modelId = "gpt-5")
		{
			var client = BuildClient(modelId);

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
	}
}