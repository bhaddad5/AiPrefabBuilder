using AiRequestBackend;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using static IConversation;

public static class AiBackendHelpers
{
    public static IConversation GetConversation(AiModel model, List<string> systemPrompts, List<ICommand> tools)
    {
        IConversation res = null;
        if (model.Provider == AiModel.ApiProvider.OpenAI)
        {
            res = new OpenAIConversation();
           
        }
        else if(model.Provider == AiModel.ApiProvider.Anthropic)
        {
            res = new AnthropicConversation();
        }

        if (res == null)
        {
            Debug.LogError($"Model {model} not yet implemented!");
            return null;
        }

        res.InitConversation(model.Id, systemPrompts, tools);

		return res;
    }
}
