using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class UnitTests_MetadataGeneration
{
	[MenuItem("Forge of Realms - Tests/Unit Test - Metadata Generation", false, 1000)]
	public static void TestMetadataGeneration()
	{
		Debug.Log("Testing Metadata Generation:");

		Action<bool, string, string> finishTest = (bool passed, string str, string description) =>
		{
			if (passed)
				Debug.Log($"{description}: Passed");
			else
				Debug.LogError($"{description}: Failed - {str}");
		};

		TestEnergyPistol(finishTest);
		TestPineTree(finishTest);
		TestWindow(finishTest);
	}

	public static void TestEnergyPistol(Action<bool, string, string> finishTest)
	{
		string testAsset = "Assets/AiPrefabAssembler/Editor/Tests/Unit Test Assets/pw1.prefab";
		string assetDescription = "Laser Pistol";

		TestAssetMetadataGeneration(testAsset, assetDescription, finishTest);
	}

	public static void TestPineTree(Action<bool, string, string> finishTest)
	{
		string testAsset = "Assets/AiPrefabAssembler/Editor/Tests/Unit Test Assets/pt.prefab";
		string assetDescription = "Pine Tree";

		TestAssetMetadataGeneration(testAsset, assetDescription, finishTest);
	}

	public static void TestWindow(Action<bool, string, string> finishTest)
	{
		string testAsset = "Assets/AiPrefabAssembler/Editor/Tests/Unit Test Assets/window.prefab";
		string assetDescription = "Window";

		TestAssetMetadataGeneration(testAsset, assetDescription, finishTest);
	}

	public static void TestAssetMetadataGeneration(string testAsset, string assetDescription, Action<bool, string, string> finishTest)
	{
		Debug.Log($"Testing {assetDescription}...");

		var obj = AssetDatabase.LoadAssetAtPath<GameObject>(testAsset);

		if (obj == null)
		{
			finishTest(false, $"Failed to find Prefab at path: {testAsset}", assetDescription);
			return;
		}

		int cbCounter = 0;

		PrePopulateMetadata.AddMetadataToPrefabCallback(testAsset, p =>
		{
			cbCounter++;

			//Await both metadata elements
			if(cbCounter == 2)
			{
				var flag = obj.GetComponent<AiMetadataFlag>();
				CheckMetadata(flag?.AiMetadataDescription ?? "", String.Join(',', flag.AiMetadataTags), assetDescription, finishTest);
			}
		});
	}

	public static void CheckMetadata(string aiDescr, string aiTags, string generalDescription, Action<bool, string, string> finishTest)
	{
		var model = ModelFetcher.GetCurrentModel();
		if (model == null)
		{
			finishTest(false, "No AI Model Selected!", generalDescription);
			return;
		}

		var conversation = AiBackendHelpers.GetConversation(model, new List<string>(), new List<ICommand>());

		string msgPrompt = "You are analyzing the result of another AI process.  This process analyzed an image and returned a description of it. " + 
			$"The object in the image was: {generalDescription}. " +
			$"Here is the description that was generated: {aiDescr}. " +
			$"And these are the tags: {aiTags}. " + 
			"If the AI got anywhere close to understanding the image, return ONLY the string: Success." +
			"Otherwise return a brief few words on why it failed.";

		Action<IConversation.ChatHistoryEntry> callback = null;
		callback = msg =>
		{
			if (!msg.IsFromUser)
			{
				//Unhook!
				conversation.ChatMsgAdded -= callback;

				var res = msg.Text.ToLowerInvariant().Trim();

				if (res == "success")
					finishTest(true, "", generalDescription);
				else
					finishTest(false, res, generalDescription);
			}
		};

		conversation.ChatMsgAdded += callback;

		conversation.SendMsg(new List<UserToAiMsg>() { new UserToAiMsgText(msgPrompt) }, new List<string>());
	}
}
