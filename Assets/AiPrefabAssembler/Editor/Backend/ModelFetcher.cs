using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using UnityEditor;

public class AiModel
{
	public AiModel(ApiProvider provider, string id, string displayName)
	{
		Provider = provider;
		Id = id;
		DisplayName = displayName;
	}

	public enum ApiProvider
	{
		OpenAI,
		Anthropic,
	}

	public ApiProvider Provider { get; set; }
	public string Id { get; set; }
	public string DisplayName { get; set; }

	public override string ToString()
	{
		return $"{Provider}: {Id}, {DisplayName}";
	}
}

public static class ModelFetcher
{
	public static List<AiModel> FetchAllAvailableModels()
	{
		var allModels = new List<AiModel>();

		allModels.Add(new AiModel(AiModel.ApiProvider.Anthropic, "claude-sonnet-4-20250514", "Claude Sonnet 4"));
		allModels.Add(new AiModel(AiModel.ApiProvider.OpenAI, "gpt-5", "GPT 5"));
		allModels.Add(new AiModel(AiModel.ApiProvider.OpenAI, "gpt-5-mini", "GPT 5 Mini"));

		return allModels;
	}

	public static AiModel GetCurrentModel()
	{
		string currModelId = EditorPrefs.GetString("SELECTED_MODEL_ID");

		var foundModel = FetchAllAvailableModels().FirstOrDefault(m => m.Id == currModelId);

		return foundModel;
	}
}