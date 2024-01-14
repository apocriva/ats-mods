using BepInEx;
using HarmonyLib;
using Eremite;
using Eremite.Controller;
using UnityEngine;
using Eremite.View.Popups.GameMenu;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection;
using TMPro;
using UniRx;
using UnityEngine.Assertions;
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
            if (_modOptionsSection == null)
                _modOptionsSection = CreateSection(OptionsTabs.General, 0, "Mod Options");
            return _modOptionsSection;
        }
    }

    internal static Plugin Instance;
    
    internal static void LogInfo(object message) => Instance.Logger.LogInfo(message);
    internal static void LogDebug(object message) => Instance.Logger.LogDebug(message);
    internal static void LogWarning(object message) => Instance.Logger.LogWarning(message);
    internal static void LogError(object message) => Instance.Logger.LogError(message);

    private Harmony _harmony;

    private static GameObject _modOptionsSection;

    private const string HEADER_PATH = "Header";

    private static Dictionary<OptionsTabs, string> _optionsPanelPaths = new()
    {
        { OptionsTabs.General, "Content/GeneralContent/Scroll View/Viewport/Content" },
        { OptionsTabs.Gameplay, "Content/GameplayContent/Scroll View/Viewport/Content" },
        { OptionsTabs.Alerts, "Content/AlertsContent/Scroll View/Viewport/Content" },
        //{ OptionsTabs.Keybinds, "Content/GeneralContent/Scroll View/Viewport/Content" }
    };

    private static GameObject _optionsSectionPrefab; // child 0: SectionBG, child 1: Header
    private static GameObject _togglePrefab;

    private struct ToggleInfo(Toggle toggle, Func<bool> getValue, Action<bool> setValue)
    {
        public readonly Toggle Toggle = toggle;
        public readonly Func<bool> GetValue = getValue;
        public readonly Action<bool> SetValue = setValue;
    }

    private static readonly List<ToggleInfo> Toggles = [];

    public static GameObject CreateToggle(string label, Func<bool> getValue, Action<bool> setValue)
    {
        return CreateToggle(ModOptionsSection, label, getValue, setValue);
    }

    public static GameObject CreateToggle(GameObject parentSection, string label, Func<bool> getValue, Action<bool> setValue, int siblingIndex = -1)
    {
        var go = Instantiate(_togglePrefab);
        var rect = go.transform as RectTransform;
        if (rect == null)
            throw new ArgumentException("Specified toggle prefab does not have RectTransform");

        rect.SetParent(parentSection.transform);
        rect.localScale = Vector3.one;
        rect.localPosition = new(0, rect.localPosition.y, 0);
        rect.localRotation = Quaternion.identity;
        if (siblingIndex > 0)
            rect.SetSiblingIndex(siblingIndex);

        var toggle = go.GetComponentInChildren<Toggle>();
        Toggles.Add(new ToggleInfo(toggle, getValue, setValue));

        go.GetComponentInChildren<TextMeshProUGUI>().text = label;

        return go;
    }

    public static GameObject CreateSection(OptionsTabs tab, int index, string title)
    {
        var go = Instantiate(_optionsSectionPrefab);
        var rect = go.transform as RectTransform;
        if (rect == null)
            throw new ArgumentException("Specified section prefab does not have RectTransform");

        rect.SetParent(_optionsSectionPrefab.transform.parent, true);
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

        return go;
    }

    private void Awake()
    {
        Instance = this;
        _harmony = Harmony.CreateAndPatchAll(typeof(Plugin));
        gameObject.hideFlags = HideFlags.HideAndDontSave;

        LogDebug($"Initialized!");
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
        LogDebug($"Destroyed!");
    }

    [HarmonyPatch(typeof(OptionsPopup), nameof(OptionsPopup.Initialize))]
    [HarmonyPrefix]
    private static void Initialize(OptionsPopup __instance)
    {
        ClearControlRegistry();
        CollectPrefabs(__instance);
        _optionsSectionPrefab = __instance.transform.Find(__instance.VolumePath).gameObject;

        var methods = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(x => x.IsClass)
            .SelectMany(x => x.GetMethods(BindingFlags.NonPublic | BindingFlags.Static))
            .Where(x => x.GetCustomAttributes(typeof(OnInitializeAttribute), false).FirstOrDefault() != null)
            .OrderBy(x => -x.GetCustomAttribute<OnInitializeAttribute>().Priority);

        foreach (var method in methods)
        {
            if (!method.IsStatic)
            {
                LogError("OnInitializeAttribute can only be used on static methods.");
                continue;
            }

            method.Invoke(null, null);
        }
    }

    private static void ClearControlRegistry()
    {
        Toggles.Clear();
    }

    private static void CollectPrefabs(OptionsPopup __instance)
    {
        _togglePrefab = __instance.autoTrackOrdersToggle.transform.parent.gameObject;
    }

    [HarmonyPatch(typeof(OptionsPopup), nameof(OptionsPopup.SetValues))]
    [HarmonyPostfix]
    private static void SetValues()
    {
        foreach (var i in Toggles)
        {
            i.Toggle.isOn = i.GetValue();
        }
    }

    [HarmonyPatch(typeof(OptionsPopup), nameof(OptionsPopup.SetUpInputs))]
    [HarmonyPostfix]
    internal static void SetUpInputs(OptionsPopup __instance)
    {
        foreach (var i in Toggles)
        {
            i.Toggle.OnValueChangedAsObservable().Subscribe(i.SetValue).AddTo(__instance);
        }
    }
}
