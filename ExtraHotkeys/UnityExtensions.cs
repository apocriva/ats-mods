using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ExtraHotkeys;

public static class UnityExtensions
{
    public static void DumpObject(this GameObject go, Action<object> log, int maxDepth, bool includeComponents = false)
    {
        go.transform.DumpObject(log, maxDepth, includeComponents);
    }

    public static void DumpObject(this Transform t, Action<object> log, int maxDepth, bool includeComponents = false, int depth = 0)
    {
        if (depth > maxDepth)
            return;

        var components = new List<string>();
        if (includeComponents)
        {
            foreach (var c in t.GetComponents<Component>())
            {
                components.Add(c.GetType().Name);
            }
        }
        
        var indent = new string(' ', depth * 4);
        log($"{indent}{t.name} {(components.Count > 0 ? " - (" + string.Join("; ", components) + ")" : "")}");
        if (t.childCount > 0 && depth + 1 <= maxDepth)
        {
            for (var i = 0; i < t.childCount; i++)
            {
                var child = t.GetChild(i);
                DumpObject(child, log, maxDepth, includeComponents, depth + 1);
            }
        }
    }
}
