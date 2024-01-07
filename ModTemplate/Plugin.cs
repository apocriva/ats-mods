using BepInEx;
using HarmonyLib;
using Eremite;
using Eremite.Controller;
using UnityEngine;

namespace ModTemplate
{
    /// <summary>
    /// Originally based on https://github.com/ats-mods/ModTemplate.
    /// </summary>
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;
        private Harmony harmony;
        
        public static void LogInfo(object message) => Instance.Logger.LogInfo(message);
        public static void LogDebug(object message) => Instance.Logger.LogDebug(message);
        public static void LogError(object message) => Instance.Logger.LogError(message);

        private void Awake()
        {
            Instance = this;
            harmony = Harmony.CreateAndPatchAll(typeof(Plugin));
            gameObject.hideFlags = HideFlags.HideAndDontSave;

            LogInfo($"Initialized!");
        }

        private void OnDestroy()
        {
            harmony?.UnpatchSelf();
            LogInfo($"Destroyed!");
        }
    }
}
