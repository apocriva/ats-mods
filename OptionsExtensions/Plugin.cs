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
using ConfigurationManager;
using Eremite.Services;
using TMPro;
using UniRx;
using UnityEngine.Events;
using UnityEngine.UI;

namespace OptionsExtensions;

/// <summary>
/// Provides some utilities for modifying/extending the Against the Storm options menu
/// </summary>
[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency("com.bepis.bepinex.configurationmanager", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin
{
    /// <summary>
    /// There's also a Twitch tab, but does it need new controls?
    /// </summary>
    public enum OptionsTabs
    {
        General,
        Gameplay,
        Alerts,
        Keybinds
    }

    /// <summary>
    /// An options section which is at the top of the General tab. Newly-created
    /// controls will go here by default unless otherwise specified.
    /// </summary>
    public static GameObject ModOptionsSection
    {
        get
        {
            if (_modOptionsSection == null)
                _modOptionsSection = CreateSection(OptionsTabs.General, 0, "Mod Options");
            return _modOptionsSection;
        }
    }

    /// <summary>
    /// A general section in the Key Bindings tab. Key bindings not currently supported!
    /// </summary>
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
    private static GameObject _sliderPrefab;
    private static GameObject _togglePrefab;

    private static ConfigurationManager.ConfigurationManager _configurationManager;

    private static GameObject _inputBlocker;
    private static OptionsPopup _optionsPopup;
    private static KeyBindingsPanel _keyBindingsPanel;

    private class ControlInfo<TControl, TValue>(TControl control, Func<TValue> getValue, Action<TValue> setValue)
    {
        public readonly TControl Control = control;
        public readonly Func<TValue> GetValue = getValue;
        public readonly Action<TValue> SetValue = setValue;
        public bool IsInitialized;
    }
    
    private static readonly List<ControlInfo<Slider, float>> Sliders = [];
    private static readonly List<ControlInfo<Toggle, bool>> Toggles = [];

    /// <summary>
    /// <inheritdoc cref="CreateButton(GameObject,string,Action,int)"/>
    /// Control is placed under <see cref="ModOptionsSection"/>.
    /// </summary>
    public static GameObject CreateButton(string label, Action onClick)
    {
        return CreateButton(ModOptionsSection, label, onClick);
    }

    /// <summary>
    /// Creates a button with the provided settings.
    /// Specifying a sibling index of less than 2 will disturb the section layout.
    /// </summary>
    public static GameObject CreateButton(GameObject parentSection, string label, Action onClick, int siblingIndex = -1)
    {
        // TODO: Fix button stretching.
        var go = Instantiate(_buttonPrefab);
        go.name = "Button";
        var rect = go.GetRectTransform();
        rect.SetParent(parentSection.transform);
        rect.localScale = Vector3.one;
        rect.localPosition = Vector3.zero;
        rect.localRotation = Quaternion.identity;
        if (siblingIndex > 0)
            rect.SetSiblingIndex(siblingIndex);

        var button = go.GetComponent<Button>();
        button.GetComponentInChildren<TMP_Text>().text = label;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(new UnityAction(onClick));

        return go;
    }
    
    /// <summary>
    /// <inheritdoc cref="CreateSlider(GameObject,string,Func{float},Action{float},int)"/>
    /// Control is placed under <see cref="ModOptionsSection"/>.
    /// </summary>
    public static GameObject CreateSlider(string label, Func<float> getValue, Action<float> setValue)
    {
        return CreateSlider(ModOptionsSection, label, getValue, setValue);
    }
    
    /// <summary>
    /// Creates a slider with the provided settings.
    /// Specifying a sibling index of less than 2 will disturb the section layout.
    /// </summary>
    public static GameObject CreateSlider(GameObject parentSection, string label, Func<float> getValue, Action<float> setValue, int siblingIndex = -1)
    {
        var go = Instantiate(_sliderPrefab);
        go.name = "OptExSlider";
        var rect = go.transform as RectTransform;
        if (rect == null)
            throw new ArgumentException("Specified Slider prefab does not have RectTransform");

        rect.SetParent(parentSection.transform);
        rect.localScale = Vector3.one;
        rect.localPosition = new(0, rect.localPosition.y, 0);
        rect.localRotation = Quaternion.identity;
        if (siblingIndex > 0)
            rect.SetSiblingIndex(siblingIndex);

        var control = go.GetComponentInChildren<Slider>();
        Sliders.Add(new ControlInfo<Slider, float>(control, getValue, setValue));

        go.GetComponentInChildren<TextMeshProUGUI>().text = label;

        return go;
    }

    /// <summary>
    /// <inheritdoc cref="CreateToggle(GameObject,string,Func{bool},Action{bool},int)"/>
    /// Control is placed under <see cref="ModOptionsSection"/>.
    /// </summary>
    public static GameObject CreateToggle(string label, Func<bool> getValue, Action<bool> setValue)
    {
        return CreateToggle(ModOptionsSection, label, getValue, setValue);
    }

    /// <summary>
    /// Creates a toggle with the provided settings.
    /// Specifying a sibling index of less than 2 will disturb the section layout.
    /// </summary>
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

    /// <summary>
    /// Creates a section in the specified options tab. Controls can be added to
    /// this new section by passing it into the various Create*() methods.
    /// </summary>
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

        _modOptionsSection?.transform.SetAsFirstSibling();

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

        if (_configurationManager == null && Chainloader.PluginInfos.TryGetValue("com.bepis.bepinex.configurationmanager", out var info))
        {
            _configurationManager = info.Instance as ConfigurationManager.ConfigurationManager;
            _configurationManager!.DisplayingWindowChanged += OnConfigurationManagerWindowChanged;
        }

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
        _modOptionsSection = null;
        _modKeyBindingsSection = null;

        ClearControlRegistry();
        CollectPrefabs(__instance);
        _optionsSectionPrefab = __instance.transform.Find(__instance.VolumePath).gameObject;

        if (_configurationManager != null)
        {
            CreateButton("Open Configuration Manager", () =>
            {
                _configurationManager.DisplayingWindow = true;
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

    private static void ClearControlRegistry()
    {
        Toggles.Clear();
    }

    private static void CollectPrefabs(OptionsPopup __instance)
    {
        _optionsPopup = __instance;

        _buttonPrefab = __instance.resetButton.gameObject;
        _sliderPrefab = __instance.cameraMouseSensitivitySlider.transform.parent.gameObject;
        _togglePrefab = __instance.autoTrackOrdersToggle.transform.parent.gameObject;

        _keyBindingsPanel = __instance.content.Find("KeyBindingsContent/Keyboard").GetComponent<KeyBindingsPanel>();
        if (_keyBindingsPanel != null)
        {
            // This button has a material with _Stencil and works in the scroll view
            _buttonPrefab = _keyBindingsPanel.slots[0].gameObject.transform.Find("Button").gameObject;
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
        foreach (var i in Sliders)
            i.Control.value = i.GetValue();

        foreach (var i in Toggles)
            i.Control.isOn = i.GetValue();
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
        foreach (var i in Sliders.Where(i => !i.IsInitialized))
        {
            i.Control.OnValueChangedAsObservable().Subscribe(i.SetValue).AddTo(_optionsPopup);
            i.IsInitialized = true;
        }

        foreach (var i in Toggles.Where(i => !i.IsInitialized))
        {
            i.Control.OnValueChangedAsObservable().Subscribe(i.SetValue).AddTo(_optionsPopup);
            i.IsInitialized = true;
        }
    }

    private static void OnConfigurationManagerWindowChanged(object context, ValueChangedEventArgs<bool> isVisible)
    {
        if (isVisible.NewValue)
            DisableGameInput();
        else
            EnableGameInput();
    }

    private static void EnableGameInput()
    {
        foreach (var i in FindObjectsOfType<GraphicRaycaster>())
            i.enabled = true;

        Serviceable.InputService.ReleaseInput(Instance);
        if (GameMB.IsGameActive)
        {
            GameMB.GameController.CameraController.MovementLock.Release(Instance);
            GameMB.GameController.CameraController.ZoomLock.Release(Instance);
        }
    }

    private static void DisableGameInput()
    {
        foreach (var i in FindObjectsOfType<GraphicRaycaster>())
            i.enabled = false;

        Serviceable.InputService.LockInput(Instance);
        if (GameMB.IsGameActive)
        {
            GameMB.GameController.CameraController.MovementLock.Lock(Instance);
            GameMB.GameController.CameraController.ZoomLock.Lock(Instance);
        }
    }
}
