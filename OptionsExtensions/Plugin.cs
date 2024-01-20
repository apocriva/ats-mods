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
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using ConfigurationManager;
using Cysharp.Threading.Tasks;
using TMPro;
using UniRx;
using Unity.Profiling;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OptionsExtensions;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency("com.bepis.bepinex.configurationmanager", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin
{
    public enum OptionsTabs
    {
        General,
        Gameplay,
        Alerts,
        Keybinds
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

    public static GameObject ModKeyBindingsSection
    {
        get
        {
            if (_modKeyBindingsSection == null)
                _modKeyBindingsSection = CreateSection(OptionsTabs.Keybinds, 0, "Mod Key Bindings");
            return _modKeyBindingsSection;
        }
    }

    internal static Plugin Instance;
    
    internal static void LogInfo(object message) => Instance.Logger.LogInfo(message);
    internal static void LogDebug(object message) => Instance.Logger.LogDebug(message);
    internal static void LogWarning(object message) => Instance.Logger.LogWarning(message);
    internal static void LogError(object message) => Instance.Logger.LogError(message);

    private Harmony _harmony;

    private static GameObject _modOptionsSection;
    private static GameObject _modKeyBindingsSection;

    private const string HEADER_PATH = "Header";

    private static Dictionary<OptionsTabs, string> _optionsPanelPaths = new()
    {
        { OptionsTabs.General, "Content/GeneralContent/Scroll View/Viewport/Content" },
        { OptionsTabs.Gameplay, "Content/GameplayContent/Scroll View/Viewport/Content" },
        { OptionsTabs.Alerts, "Content/AlertsContent/Scroll View/Viewport/Content" },
        { OptionsTabs.Keybinds, "Content/KeyBindingsContent/Keyboard/Scroll View/Viewport/Content" }
    };

    private static GameObject _optionsSectionPrefab; // child 0: SectionBG, child 1: Header
    private static GameObject _buttonPrefab;
    private static GameObject _togglePrefab;
    private static GameObject _keyBindingSlotPrefab;

    private static GameObject _inputBlocker;
    private static OptionsPopup _optionsPopup;
    private static KeyBindingsPanel _keyBindingsPanel;
    private static Action<InputAction> _keyBindingsPanelOnChangeRequested;

    private class ControlInfo<TControl, TValue>(TControl control, Func<TValue> getValue, Action<TValue> setValue)
    {
        public readonly TControl Control = control;
        public readonly Func<TValue> GetValue = getValue;
        public readonly Action<TValue> SetValue = setValue;
        public bool IsInitialized;
    }

    private static readonly List<ControlInfo<BindingSlot, KeyboardShortcut>> KeyBindingSlots = [];
    private static readonly List<ControlInfo<Toggle, bool>> Toggles = [];

    public static GameObject CreateButton(string label, Action onClick)
    {
        return CreateButton(ModOptionsSection, label, onClick);
    }

    public static GameObject CreateButton(GameObject parentSection, string label, Action onClick, int siblingIndex = -1)
    {
        var go = Instantiate(_buttonPrefab);
        go.name = "OptExButton";

        var rect = go.transform as RectTransform;
        if (rect == null)
            throw new ArgumentException("Specified KeyBindingSlot slot prefab does not have RectTransform");

        rect.SetParent(parentSection.transform);
        rect.localScale = Vector3.one;
        rect.localPosition = new(0, rect.localPosition.y, 0);
        rect.localRotation = Quaternion.identity;
        if (siblingIndex > 0)
            rect.SetSiblingIndex(siblingIndex);

        var button = go.GetComponent<Button>();
        button.GetComponentInChildren<TMP_Text>().text = label;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(new UnityAction(onClick));

        return go;
    }

    public static GameObject CreateKeyBindingSlot(string label, Func<KeyboardShortcut> getValue, Action<KeyboardShortcut> setValue)
    {
        return CreateKeyBindingSlot(ModKeyBindingsSection, label, getValue, setValue);
    }

    public static GameObject CreateKeyBindingSlot(GameObject parentSection, string label, Func<KeyboardShortcut> getValue, Action<KeyboardShortcut> setValue, int siblingIndex = -1)
    {
        var go = Instantiate(_keyBindingSlotPrefab);
        go.name = "OptExBindingSlot";
        Destroy(go.GetComponent<KeyBindingSlot>());

        var rect = go.transform as RectTransform;
        if (rect == null)
            throw new ArgumentException("Specified KeyBindingSlot slot prefab does not have RectTransform");

        rect.SetParent(parentSection.transform);
        rect.localScale = Vector3.one;
        rect.localPosition = new(0, rect.localPosition.y, 0);
        rect.localRotation = Quaternion.identity;
        if (siblingIndex > 0)
            rect.SetSiblingIndex(siblingIndex);

        var control = go.AddComponent<BindingSlot>();
        control.SetUp(label);
        KeyBindingSlots.Add(new ControlInfo<BindingSlot, KeyboardShortcut>(control, getValue, setValue));

        return go;
    }

    public static GameObject CreateToggle(string label, Func<bool> getValue, Action<bool> setValue)
    {
        return CreateToggle(ModOptionsSection, label, getValue, setValue);
    }

    public static GameObject CreateToggle(GameObject parentSection, string label, Func<bool> getValue, Action<bool> setValue, int siblingIndex = -1)
    {
        var go = Instantiate(_togglePrefab);
        go.name = "OptExToggle";
        var rect = go.transform as RectTransform;
        if (rect == null)
            throw new ArgumentException("Specified Toggle prefab does not have RectTransform");

        rect.SetParent(parentSection.transform);
        rect.localScale = Vector3.one;
        rect.localPosition = new(0, rect.localPosition.y, 0);
        rect.localRotation = Quaternion.identity;
        if (siblingIndex > 0)
            rect.SetSiblingIndex(siblingIndex);

        var toggle = go.GetComponentInChildren<Toggle>();
        Toggles.Add(new ControlInfo<Toggle, bool>(toggle, getValue, setValue));

        go.GetComponentInChildren<TextMeshProUGUI>().text = label;

        return go;
    }

    public static GameObject CreateSection(OptionsTabs tab, int index, string title)
    {
        var go = Instantiate(_optionsSectionPrefab);
        go.name = "OptExSection";
        var rect = go.transform as RectTransform;
        if (rect == null)
            throw new ArgumentException("Specified section prefab does not have RectTransform");

        var parent = _optionsPopup.FindChild(_optionsPanelPaths[tab]).transform;
        rect.SetParent(parent, true);
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

        if (Chainloader.PluginInfos.TryGetValue("com.bepis.bepinex.configurationmanager", out var info))
        {
            var confman = info.Instance as ConfigurationManager.ConfigurationManager;
            confman.DisplayingWindowChanged += OnConfigurationManagerWindowChanged;

            CreateButton("Open Configuration Manager", () =>
            {
                var confman = info.Instance as ConfigurationManager.ConfigurationManager;
                confman.DisplayingWindow = !confman.DisplayingWindow;
            });
        }

        InitializeOtherPlugins();
    }

    private static void InitializeOtherPlugins()
    {
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

    private static void PrepareInputBlocker()
    {
        //_inputBlocker = new GameObject("OptExInputBlocker", typeof(RectTransform), typeof(Button));
        //var rect = _inputBlocker.GetRectTransform();
        //rect.SetParent(_optionsPopup.transform, true);
        //rect.SetSiblingIndex(0);
        //rect.SetAnchor(AnchorPresets.FullStrech);
        //_inputBlocker.SetActive(true);
        //_inputBlocker.GetComponent<Button>().interactable = true;
    }

    private static void ClearControlRegistry()
    {
        Toggles.Clear();
    }

    private static void CollectPrefabs(OptionsPopup __instance)
    {
        _optionsPopup = __instance;

        _buttonPrefab = __instance.resetButton.gameObject;
        _togglePrefab = __instance.autoTrackOrdersToggle.transform.parent.gameObject;

        _keyBindingsPanel = __instance.content.Find("KeyBindingsContent/Keyboard").GetComponent<KeyBindingsPanel>();
        if (_keyBindingsPanel != null)
        {
            _keyBindingsPanelOnChangeRequested = _keyBindingsPanel.OnChangeRequested;
            _keyBindingSlotPrefab = _keyBindingsPanel.slots[0].gameObject;
        }
        else
        {
            LogError("Unable to locate KeyBindingsPanel");
        }
    }

    [HarmonyPatch(typeof(OptionsPopup), nameof(OptionsPopup.SetValues))]
    [HarmonyPostfix]
    private static void OptionsPopup_SetValues()
    {
        foreach (var i in Toggles)
        {
            i.Control.isOn = i.GetValue();
        }
    }

    [HarmonyPatch(typeof(OptionsPopup), nameof(OptionsPopup.SetUpInputs))]
    [HarmonyPostfix]
    private static void OptionsPopup_SetUpInputs()
    {
        SetUpControls();
    }

    [HarmonyPatch(typeof(KeyBindingsPanel), nameof(KeyBindingsPanel.SetUpSlots))]
    [HarmonyPostfix]
    private static void KeyBindingsPanel_SetUpSlots()
    {
        SetUpControls();
    }

    private static void SetUpControls()
    {
        foreach (var i in Toggles.Where(i => !i.IsInitialized))
        {
            i.Control.OnValueChangedAsObservable().Subscribe(i.SetValue).AddTo(_optionsPopup);
            i.IsInitialized = true;
        }

        foreach (var i in KeyBindingSlots.Where(i => !i.IsInitialized))
        {
            i.Control.SetData(i.GetValue(), i, ShowBindingCallback);
            i.IsInitialized = true;
        }
    }

    private static void ShowBindingCallback(object o)
    {
        ShowKeyBindingPrompt((ControlInfo<BindingSlot, KeyboardShortcut>)o).Forget();
    }

    private static async UniTaskVoid ShowKeyBindingPrompt(ControlInfo<BindingSlot, KeyboardShortcut> control)
    {
        //_keyBindingsPanel.DisableInput();
        //var inputAction = control.GetValue();
        //if (await _keyBindingsPanel.prompt.Show(inputAction))
        //    control.SetValue(inputAction);
        //_keyBindingsPanel.EnableInput();
    }

    private static void OnConfigurationManagerWindowChanged(object context, ValueChangedEventArgs<bool> isVisible)
    {
        if (isVisible.NewValue)
            _optionsPopup.blend.SetActive(true);
        else
            _optionsPopup.blend.SetActive(false);
    }
}
