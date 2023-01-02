#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TownOfHost.Extensions;

namespace TownOfHost.Roles;


public static class MethodInfoExtension
{
    public static object? InvokeAligned(this MethodInfo info, object obj, params object[] parameters)
    {
        return info.Invoke(obj, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, AlignFunctionParameters(info, parameters), null);
    }


    private static object[] AlignFunctionParameters(MethodInfo method, IEnumerable<object?> allParameters)
    {
        List<object?> allParametersList = allParameters.ToList();
        List<object> functionSpecificParameters = new();

        /*allParametersList.String().DebugLog("Parameters passed into function: ");
        method.GetParameters().ToList().String().DebugLog("Required parameters: ");*/

        int i = 1;
        foreach (ParameterInfo parameter in method.GetParameters())
        {
            int matchingParamIndex = allParametersList.FindIndex(obj => obj != null && obj.GetType() == parameter.ParameterType);
            if (matchingParamIndex == -1 && !parameter.IsOptional)
                throw new ArgumentException($"Invocation of {method.Name} does not contain all required arguments. Argument {i} ({parameter.Name}) was not supplied.");
            functionSpecificParameters.Add(allParametersList.Pop(matchingParamIndex)!);
            i++;
        }

        return functionSpecificParameters.ToArray();
    }
}