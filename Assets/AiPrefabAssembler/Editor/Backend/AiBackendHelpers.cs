using AiRequestBackend;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using static IConversation;

public static class AiBackendHelpers
{
    public static IConversation GetConversation(Model model, List<string> systemPrompts, List<ICommand> tools)
    {
        IConversation res = null;
        if (model == Model.GPT5standard || model == Model.GPT5mini || model == Model.GPT5Micro)
        {
            res = new OpenAIConversation();
           
        }
        else if(model == Model.ClaudeSonnet4)
        {
            res = new AnthropicConversation();
        }

        if (res == null)
            Debug.LogError($"Model {model} not yet implemented!");
        else
			res.InitConversation(model, systemPrompts, tools);

		return res;
    }
}
