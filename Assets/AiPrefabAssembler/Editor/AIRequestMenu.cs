using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AIRequestMenu : EditorWindow
{
	private string prompt = "";
	public List<IConversation.ChatHistoryEntry> CurrentChatHistory = new List<IConversation.ChatHistoryEntry>();

	// Scroll position for chat history
	private Vector2 chatScroll;
	private bool scrollToBottom;

	private IConversation Conversation;

	const string generalUnityPrompt = "You are helping a developer perform actions in Unity. " +
		"You can respond to the user with Questions if you need clarification, or you can use the provided Tools to interact with the scene and perform their request, then provide a helpful description of what you did. " +
		"Keep your final response brief and to-the-point, and use object/prefab Names and plain english, not ID numbers. " +
		"Always request Context to understand the objects and prefabs you are using! " +
		"Always try to figure out what they need and call the InteractWithScene tool to do it, before responding! " +
		"Always try to batch together as many tool calls as you can, to save time/cycles. " +
		"Unity is a Y-up coordinate system where a Distance of 1 = 1 meter. " +
		"If placing objects contextually to another object, always try to place it under the same parent! " +
		"Any requests to generate metadata for a prefab should call RequestRendersForPrefabDescription followed by AssignMetadataToPrefab.";

	[MenuItem("Forge of Realms/AI Assistant", false, 200)]
	static void Init()
	{
		var window = GetWindow<AIRequestMenu>();
		window.titleContent = new GUIContent("AI Request");
		window.minSize = new Vector2(420, 260);
		window.Show();

		List<ICommand> commands = new List<ICommand>()
		{
			new GetPrefabContextCommand(),
			new SearchPrefabsContextCommand(),
			new GetObjectContextCommand(),
			new SearchObjectsContextCommand(),
			new CreateObjectCommand(),
			new DeleteObjectCommand(),
			new SetObjectParentCommand(),
			new SetObjectTransformCommand(),
		};

		var model = ModelFetcher.GetCurrentModel();

		if(model == null)
		{
			Debug.LogError("No AI Model Selected!!!");
			window.Close();
		}	

		window.Conversation = AiBackendHelpers.GetConversation(model, new List<string>() { generalUnityPrompt }, commands);

		window.Conversation.ChatMsgAdded += window.AddChat;
		window.Conversation.IsProcessingMsgChanged += p => window.TriggerRepaint();
	}

	void OnEnable()
	{
		// Open with history scrolled to bottom
		scrollToBottom = true;

		// Keep spinner animating while thinking
		EditorApplication.update -= RepaintWhileThinking;
		EditorApplication.update += RepaintWhileThinking;
	}

	void OnDisable()
	{
		EditorApplication.update -= RepaintWhileThinking;
	}

	private void RepaintWhileThinking()
	{
		if (Conversation != null && Conversation.IsProcessingMsg)
			Repaint();
	}

	public void AddChat(IConversation.ChatHistoryEntry e)
	{
		CurrentChatHistory.Add(e);
		scrollToBottom = true; // auto-jump when new messages arrive
		Repaint();
	}

	public void TriggerRepaint()
	{
		Repaint();
	}

	void OnGUI()
	{
		// Styles for chat bubbles
		var leftStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
		{
			alignment = TextAnchor.UpperLeft,
			richText = true,
			padding = new RectOffset(8, 8, 6, 6),
		};

		var rightStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
		{
			alignment = TextAnchor.UpperRight,
			richText = true,
			padding = new RectOffset(8, 8, 6, 6),
		};

		// A subtle container style for each message (optional)
		var bubble = new GUIStyle(EditorStyles.helpBox)
		{
			padding = new RectOffset(10, 10, 8, 8),
			margin = new RectOffset(6, 6, 4, 4)
		};

		GUILayout.BeginVertical();

		// --- Chat history (fills remaining space) ---
		EditorGUILayout.LabelField("Chat History", EditorStyles.boldLabel);

		chatScroll = EditorGUILayout.BeginScrollView(chatScroll, GUILayout.ExpandHeight(true));
		{
			float viewWidth = EditorGUIUtility.currentViewWidth - 40f; // account for margins/scrollbar

			foreach (var entry in CurrentChatHistory)
			{
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace(); // allows nice right/left sizing

				// Constrain bubble width a bit so long lines wrap nicely (roughly 70% of view width)
				float targetWidth = Mathf.Clamp(viewWidth * 0.9f, 240f, 800f);

				GUILayout.BeginVertical(bubble, GUILayout.MaxWidth(targetWidth), GUILayout.ExpandWidth(false));
				GUILayout.Label(entry.Text ?? string.Empty, entry.IsFromUser ? rightStyle : leftStyle, GUILayout.ExpandWidth(true));
				GUILayout.EndVertical();

				GUILayout.FlexibleSpace();

				// Push AI bubbles left, user bubbles right
				if (!entry.IsFromUser)
					GUILayout.FlexibleSpace();

				GUILayout.EndHorizontal();
			}
		}
		EditorGUILayout.EndScrollView();

		// After layout has completed, jump to bottom once
		if (scrollToBottom && Event.current.type == EventType.Repaint)
		{
			chatScroll.y = float.MaxValue;
			scrollToBottom = false;
			Repaint();
		}

		GUILayout.Space(6);

		// --- Bottom area: either spinner OR input+button ---
		if (Conversation != null && Conversation.IsProcessingMsg)
		{
			DrawSpinnerRow();
		}
		else
		{
			EditorGUILayout.LabelField("Prompt", EditorStyles.boldLabel);

			var textStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
			prompt = EditorGUILayout.TextArea(
				prompt ?? string.Empty,
				textStyle,
				GUILayout.MinHeight(100),
				GUILayout.ExpandWidth(true)
			);

			GUILayout.Space(6);

			if (GUILayout.Button("Go!", GUILayout.Height(30)))
			{
				if (string.IsNullOrWhiteSpace(prompt))
					return;

				string objectSelectionPrompt =
					"Here are the UniqueIds of the user's selected Objects: " + SceneDescriptionBuilder.BuildSelectionString();

				string prefabSelectionPrompt =
					"Here are the UniqueIds of the user's selected Prefabs: " + SceneDescriptionBuilder.BuildPrefabSelectionString();

				Conversation.SendMsg(new List<UserToAiMsg>() { new UserToAiMsgText(prompt) }, new List<string>() { objectSelectionPrompt, prefabSelectionPrompt });
				prompt = "";
				scrollToBottom = true;
			}
		}

		GUILayout.EndVertical();
	}

	// Spinner UI (animated using EditorApplication.timeSinceStartup)
	private void DrawSpinnerRow()
	{
		// Built-in spinner frames: "WaitSpin00".."WaitSpin11"
		int frame = (int)(EditorApplication.timeSinceStartup * 10f) % 12;
		string frameName = frame < 10 ? $"WaitSpin0{frame}" : $"WaitSpin{frame}";
		var icon = EditorGUIUtility.IconContent(frameName);

		var dim = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft };
		using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
		{
			GUILayout.Space(4);
			GUILayout.Label(icon, GUILayout.Width(18), GUILayout.Height(18));
			GUILayout.Space(6);
			GUILayout.Label(Conversation.CurrentThinkingStatus, dim, GUILayout.Height(20));
			GUILayout.FlexibleSpace();
		}
	}
}
