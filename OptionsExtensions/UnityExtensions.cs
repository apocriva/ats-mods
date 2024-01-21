using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace OptionsExtensions;

public static class UnityExtensions
{
    public static void DumpTree(this GameObject go, Action<object> log, int maxDepth, bool includeComponents = false)
    {
        go.transform.DumpTree(log, maxDepth, includeComponents);
    }

    public static void DumpTree(this Transform t, Action<object> log, int maxDepth, bool includeComponents = false, int depth = 0)
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
                DumpTree(child, log, maxDepth, includeComponents, depth + 1);
            }
        }
    }

    public static void Dump(this RectTransform r, Action<object> log)
    {
        log($"Rect ({r.name}) anchor=({r.anchorMin}, {r.anchorMax}) anchoredPosition={r.anchoredPosition} sizeDelta={r.sizeDelta}");
    }

    public static void DumpInfo(this GameObject o, Action<object> log)
    {
        log($"Detailed dump on {o.name}:");

        log($"    activeSelf={o.activeSelf} activeInHierarchy={o.activeInHierarchy}");

        foreach (var c in o.GetComponents<Component>())
        {
            if (c is RectTransform r)
            {
                log($"    Rect: anchor=({r.anchorMin}, {r.anchorMax}) anchoredPosition={r.anchoredPosition} sizeDelta={r.sizeDelta}");
            }
            else
            {
                log($"    {c.GetType()}");
            }
        }
    }
}
