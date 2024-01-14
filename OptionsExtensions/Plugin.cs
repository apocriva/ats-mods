﻿using BepInEx;
using HarmonyLib;
using Eremite;
using Eremite.Controller;
using UnityEngine;
using Eremite.View.Popups.GameMenu;
using System.Collections.Generic;
using System;
using TMPro;
using UniRx;
using UnityEngine.UI;

namespace OptionsExtensions;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public enum OptionsTabs
    {
        General,
        Gameplay,
        Alerts,
        //Keybinds
    }

    public static GameObject ModOptionsSection
    {
        get
        {
            if (modOptionsSection == null)
                modOptionsSection = CreateSection(OptionsTabs.General, 0, "Mod Options");
            return modOptionsSection;
        }
    }

    internal static Plugin Instance;
    
    internal static void LogInfo(object message) => Instance.Logger.LogInfo(message);
    internal static void LogDebug(object message) => Instance.Logger.LogDebug(message);
    internal static void LogError(object message) => Instance.Logger.LogError(message);

    private Harmony harmony;

    private static GameObject modOptionsSection;

    private const string HEADER_PATH = "Header";

    private static Dictionary<OptionsTabs, string> optionsPanelPaths = new()
    {
        { OptionsTabs.General, "Content/GeneralContent/Scroll View/Viewport/Content" },
        { OptionsTabs.Gameplay, "Content/GameplayContent/Scroll View/Viewport/Content" },
        { OptionsTabs.Alerts, "Content/AlertsContent/Scroll View/Viewport/Content" },
        //{ OptionsTabs.Keybinds, "Content/GeneralContent/Scroll View/Viewport/Content" }
    };

    private static GameObject optionsSectionPrefab; // child 0: SectionBG, child 1: Header
    private static GameObject togglePrefab;

    private struct ToggleInfo(Toggle toggle, Func<bool> getValue, Action<bool> setValue)
    {
        public Toggle toggle = toggle;
        public Func<bool> getValue = getValue;
        public Action<bool> setValue = setValue;
    }

    private static List<ToggleInfo> toggles = [];

    private void Awake()
    {
        Instance = this;
        harmony = Harmony.CreateAndPatchAll(typeof(Plugin));
        gameObject.hideFlags = HideFlags.HideAndDontSave;

        LogDebug($"Initialized!");
    }

    private void OnDestroy()
    {
        harmony?.UnpatchSelf();
        LogDebug($"Destroyed!");
    }

    public static GameObject CreateToggle(string label, Func<bool> getValue, Action<bool> setValue)
    {
        return CreateToggle(ModOptionsSection, label, getValue, setValue);
    }

    public static GameObject CreateToggle(GameObject parentSection, string label, Func<bool> getValue, Action<bool> setValue, int siblingIndex = -1)
    {
        var go = Instantiate(togglePrefab);
        var rect = go.transform as RectTransform;
        rect.SetParent(parentSection.transform);
        rect.localScale = Vector3.one;
        rect.localPosition = new(0, rect.localPosition.y, 0);
        rect.localRotation = Quaternion.identity;
        if (siblingIndex > 0)
            rect.SetSiblingIndex(siblingIndex);

        var toggle = go.GetComponentInChildren<Toggle>();
        toggles.Add(new ToggleInfo(toggle, getValue, setValue));

        go.GetComponentInChildren<TextMeshProUGUI>().text = label;

        return go;
    }

    public static GameObject CreateSection(OptionsTabs tab, int index, string title)
    {
        var newPanelGameObject = Instantiate(optionsSectionPrefab);
        var rect = newPanelGameObject.transform as RectTransform;
        rect.SetParent(optionsSectionPrefab.transform.parent, true);
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

    [HarmonyPatch(typeof(OptionsPopup), nameof(OptionsPopup.Initialize))]
    [HarmonyPrefix]
    public static void Initialize(OptionsPopup __instance)
    {
        ClearRegistry();
        CollectPrefabs(__instance);
        optionsSectionPrefab = __instance.transform.Find(__instance.VolumePath).gameObject;
    }

    private static void ClearRegistry()
    {
        toggles.Clear();
    }

    private static void CollectPrefabs(OptionsPopup __instance)
    {
        togglePrefab = __instance.autoTrackOrdersToggle.transform.parent.gameObject;
    }

    [HarmonyPatch(typeof(OptionsPopup), nameof(OptionsPopup.SetValues))]
    [HarmonyPostfix]
    private static void SetValues()
    {
        foreach (var i in toggles)
        {
            i.toggle.isOn = i.getValue();
        }
    }

    [HarmonyPatch(typeof(OptionsPopup), nameof(OptionsPopup.SetUpInputs))]
    [HarmonyPostfix]
    internal static void SetUpInputs(OptionsPopup __instance)
    {
        foreach (var i in toggles)
        {
            i.toggle.OnValueChangedAsObservable().Subscribe(i.setValue).AddTo(__instance);
        }
    }
}