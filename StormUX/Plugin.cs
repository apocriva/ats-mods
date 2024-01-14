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

namespace StormUX;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency("OptionsExtensions", "0.0.1")]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance;
    public static GameObject GameObject => Instance.gameObject;
    private Harmony harmony;
    
    public static void LogInfo(object message) => Instance.Logger.LogInfo(message);
    public static void LogDebug(object message) => Instance.Logger.LogDebug(message);
    public static void LogError(object message) => Instance.Logger.LogError(message);

    private void Awake()
    {
        Instance = this;

        harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        //harmony.PatchAll(typeof(OptionsExtensions));
        harmony.PatchAll(typeof(WikiHotkeys));
        harmony.PatchAll(typeof(OverlayToggles));
        gameObject.hideFlags = HideFlags.HideAndDontSave;

        LogDebug($"Initialized!");
    }

    private void OnDestroy()
    {
        harmony?.UnpatchSelf();
        LogDebug($"Destroyed!");
    }
}
