using BepInEx;
using HarmonyLib;
using Eremite;
using Eremite.Controller;
using Eremite.Services;
using Eremite.View.Popups.GameMenu;
using UnityEngine.InputSystem;
using System;
using System.IO;
using BepInEx.Configuration;
using Cysharp.Threading.Tasks;
using Eremite.Controller.Generator;
using Eremite.View.HUD.TradeRoutes;
using UniRx;
using UnityEngine;
using UnityEngine.Yoga;
using ConfigDefinition = BepInEx.Configuration.ConfigDefinition;

namespace WorkerHotkeys
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;
        private Harmony harmony;

        public static void LogInfo(object obj) => Instance.Logger.LogInfo(obj);
        public static void LogDebug(object obj) => Instance.Logger.LogDebug(obj);
        public static void LogError(object obj) => Instance.Logger.LogError(obj);

        public static PluginState State { get; private set; } = new();

        private void Awake()
        {
            Instance = this;
            //harmony = Harmony.CreateAndPatchAll(typeof(Plugin));
        }

        private static string GetSavePath()
        {
            return Path.Combine(Serviceable.ProfilesService.GetFolderPath(), PluginInfo.PLUGIN_GUID + ".save");
        }

        private static void Save()
        {
            LogDebug($"Saving to {GetSavePath()}");
            //LogDebug($"State.SelectWorker1Hotkey binding={State.SelectWorker1Hotkey.bindings[0].path}");
            try
            {
                //var json = JsonUtility.ToJson(State);
                //File.WriteAllText(GetSavePath(), json);
            }
            catch (Exception e)
            {
                LogError(e.Message);
                LogError(e.StackTrace);
                LogError($"Error while trying to save data for mod {PluginInfo.PLUGIN_GUID}");
            }
        }

        private static void Load()
        {
            try
            {
                /*if (File.Exists(GetSavePath()))
                {
                    LogInfo($"Loading from {GetSavePath()}");
                    var json = File.ReadAllText(GetSavePath());
                    State = JsonUtility.FromJson<PluginState>(json);
                }*/
            }
            catch(Exception e)
            {
                LogError(e.Message);
                LogError($"Error while trying to load data for mod {PluginInfo.PLUGIN_GUID}");
            }

            State ??= new();
            SetupActions();
        }

        private static void Reset()
        {
            State = new();
            SetupActions();
        }

        private static void SetupActions()
        {
            LogDebug($"SetupActions");

            var config = MB.InputConfig.Get();
            MB.InputService.Config.Disable();
            var action = config.FindAction("SelectWorker1");
            if (action == null)
            {
                LogDebug("Creating SelectWorker1 action...");
                action = config.AddAction
                    (
                        name: "SelectWorker1",
                        type: InputActionType.Button
                    );
                config.AddBinding("<Keyboard>/numpad1", action);
            }
            LogDebug($"SelectWorker1 action.path={action.bindings[0].path} action.controls.Count={action.controls.Count}");
            foreach (var control in action.controls)
            {
                LogDebug($"control name={control.device.name} path={control.path}");
            }
            MB.InputService.Config.Enable();

            LogDebug($"config has {config.actions.Count} action(s)");
            foreach (var configAction in config.actions)
            {
                LogDebug($"action name={configAction.name} path={configAction.bindings[0].path}");
            }
        }

        [HarmonyPatch(typeof(MainController), nameof(MainController.OnServicesReady))]
        [HarmonyPostfix]
        private static void OnStartup()
        {
            Load();
            LogInfo($"{PluginInfo.PLUGIN_GUID} initialized.");
        }

        [HarmonyPatch(typeof(ClientPrefsService), nameof(ClientPrefsService.Save))]
        [HarmonyPostfix]
        private static void OnSave()
        {
            Save();
        }

        [HarmonyPatch(typeof(ClientPrefsService), nameof(ClientPrefsService.Reset))]
        [HarmonyPostfix]
        private static void OnReset()
        {
            Reset();
        }

        [HarmonyPatch(typeof(KeyBindingsPanel), nameof(KeyBindingsPanel.SetUpKeyboard))]
        [HarmonyPrefix]
        private static void KeyBindingsPanel_AddWorkerHotkeys(KeyBindingsPanel __instance)
        {
            __instance.SetUpSlot(State.SelectWorker1Hotkey);
        }

        [HarmonyPatch(typeof(KeyBindingSlot), nameof(KeyBindingSlot.SetUp))]
        [HarmonyPostfix]
        private static void KeyBindingSlot_SetUp(KeyBindingSlot __instance)
        {
            var action = __instance.action;
            LogDebug($"KeyBindingSlot_SetUp action={action.name} expectedControlType={action.expectedControlType} controls.Count={action.controls.Count}");
            foreach (var control in action.controls)
            {
                LogDebug($"control name={control.device.name} path={control.path}");
            }
        }

        public static void Actions_AddCallbacks()
        {
            LogDebug($"Actions_AddCallbacks");
            State.SelectWorker1Hotkey.started += OnWorker1Selected;
            State.SelectWorker1Hotkey.performed += OnWorker1Selected;
            State.SelectWorker1Hotkey.canceled += OnWorker1Selected;
        }

        public static void Actions_UnregisterCallbacks()
        {
            LogDebug($"Actions_UnregisterCallbacks");
            State.SelectWorker1Hotkey.started -= OnWorker1Selected;
            State.SelectWorker1Hotkey.performed -= OnWorker1Selected;
            State.SelectWorker1Hotkey.canceled -= OnWorker1Selected;
        }

        private static void OnWorker1Selected(InputAction.CallbackContext context)
        {
            LogDebug($"OnWorker1Selected: {context.ToString()}");
        }
    }
}
