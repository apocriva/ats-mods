using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eremite;
using Eremite.View.Popups.GameMenu;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExtraHotkeys;

public static class OptionsExtensions
{
    private const string HEADER_PATH = "Header";

    // child 0: SectionBG
    // child 1: Header
    private static GameObject optionsPanelPrefab;

    [HarmonyPatch(typeof(OptionsPopup), nameof(OptionsPopup.Initialize))]
    [HarmonyPrefix]
    public static void OptionsPopup_PreInitialize(OptionsPopup __instance)
    {
        Plugin.LogDebug("OptionsPopup_PreInitialize");

        optionsPanelPrefab = __instance.transform.Find(__instance.VideoPath).gameObject;
        CreatePanel("Content/GeneralContent/Scroll View/Viewport/Content", 0, "Mod Options");
    }

    public static GameObject CreatePanel(string path, int index, string title)
    {
        var newPanelGameObject = UnityEngine.Object.Instantiate(optionsPanelPrefab);
        var rect = newPanelGameObject.transform as RectTransform;
        rect.SetParent(optionsPanelPrefab.transform.parent, true);
        rect.localPosition = new(0f, rect.localPosition.y, 0);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.transform.SetSiblingIndex(index);

        // Delete unnecessary children.
        for (var i = rect.childCount - 1; i > 1; i--)
        {
            var child = rect.GetChild(i);
            child.gameObject.Destroy();
        }

        var header = rect.Find(HEADER_PATH);
        header.GetComponent<TextMeshProUGUI>().text = title;

        return newPanelGameObject;
    }

    public static void DumpObject(Transform t, Action<object> log, int maxDepth, bool includeComponents, int depth = 0)
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
