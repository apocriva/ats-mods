using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Eremite;
using Eremite.Controller;
using Eremite.View.UI.Wiki;
using UnityEngine;
using OptEx = OptionsExtensions.Plugin;

namespace StormUX;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency("OptionsExtensions", "0.0.1")]
public class Plugin : BaseUnityPlugin
{
    internal static Plugin Instance;
    internal static GameObject GameObject => Instance.gameObject;
    
    internal static void LogInfo(object message) => Instance.Logger.LogInfo(message);
    internal static void LogDebug(object message) => Instance.Logger.LogDebug(message);
    internal static void LogError(object message) => Instance.Logger.LogError(message);

    internal static GameObject OptionsSection;

    private Harmony _harmony;

    private void Awake()
    {
        Instance = this;

        _harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        _harmony.PatchAll(typeof(WikiHotkeys));
        _harmony.PatchAll(typeof(OverlayToggles));
        _harmony.PatchAll(typeof(WorkerHotkeys));
        gameObject.hideFlags = HideFlags.HideAndDontSave;

        LogDebug($"Initialized!");
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
        LogDebug($"Destroyed!");
    }

    [OptionsExtensions.OnInitialize(1)]
    private static void OptExInitialize()
    {
        OptionsSection = OptEx.CreateSection(OptEx.OptionsTabs.Gameplay, 0, "StormUX");
    }
}
