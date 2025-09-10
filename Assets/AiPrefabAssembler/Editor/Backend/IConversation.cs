using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IConversation
{
	public enum Model
	{
		GPT5Micro,
		GPT5mini,
		GPT5standard,
		ClaudeSonnet4
	}

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

	public bool IsProcessingMsg { get; }
	public string CurrentThinkingStatus { get; }

	public event Action<ChatHistoryEntry> ChatMsgAdded;
	public event Action<bool> IsProcessingMsgChanged;

	public void InitConversation(string modelId, List<string> systemPrompts, List<ICommand> tools);

	public void SendMsg(string msg, List<string> transientContextMsgs);
}
