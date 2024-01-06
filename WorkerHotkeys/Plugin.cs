using BepInEx;
using HarmonyLib;
using Eremite;
using Eremite.Controller;
using Eremite.Services;
using Eremite.View.Popups.GameMenu;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Configuration;
using Cysharp.Threading.Tasks;
using Eremite.Controller.Generator;
using Eremite.Model;
using Eremite.View.HUD;
using Eremite.View.HUD.TradeRoutes;
using UniRx;
using UnityEngine;

namespace WorkerHotkeys;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance;
    private Harmony harmony;

    private const int NUM_WORKER_SLOTS = 3;

    public static void LogInfo(object obj) => Instance.Logger.LogInfo(obj);
    public static void LogDebug(object obj) => Instance.Logger.LogDebug(obj);
    public static void LogError(object obj) => Instance.Logger.LogError(obj);
    
    private static readonly List<ConfigEntry<KeyboardShortcut>> selectSlotShortcuts = [];
    private static RacesHUD racesHud;

    private void Awake()
    {
        for (var i = 0; i < NUM_WORKER_SLOTS; ++i)
        {
            selectSlotShortcuts.Add(Config.Bind
            (
                new ConfigDefinition("Hotkeys", $"SelectSlot{i + 1}"),
                new KeyboardShortcut(KeyCode.Keypad1 + i),
                new ConfigDescription($"Hotkey to select race in slot {i + 1}")
            ));
        }

        Instance = this;
        harmony = Harmony.CreateAndPatchAll(typeof(Plugin));
        LogInfo($"Initialized!");
        gameObject.hideFlags = HideFlags.HideAndDontSave;
    }

    private void Update()
    {
        if (!GameController.IsGameActive || MB.InputService.IsLocked())
            return;

        var selectedIndex = selectSlotShortcuts.FindIndex(item => item.Value.IsDown());
        if (selectedIndex < 0)
            return;

        var slot = GetHudSlotFromShortcutIndex(selectedIndex);
        if (GameMB.ModeService.RaceMode.Value &&
            (slot == null || slot.race == GameMB.GameBlackboardService.PickedRace.Value))
        {
            GameMB.ModeService.Back();
        }
        else if (slot != null)
            racesHud.OnSlotClicked(slot);
    }

    private RacesHUDSlot GetHudSlotFromShortcutIndex(int shortcutIndex)
    {
        var shortcutCheckIndex = 0;
        foreach (var slot in racesHud.slots)
        {
            if (slot.IsRevealed())
            {
                if (shortcutCheckIndex == shortcutIndex)
                    return slot;

                shortcutCheckIndex++;
            }
        }

        return null;
    }

    [HarmonyPatch(typeof(RacesHUD), nameof(RacesHUD.SetUpSlots))]
    [HarmonyPostfix]
    private static void RacesHUD_AfterSetUpSlots(RacesHUD __instance)
    {
        racesHud = __instance;
    }

    private void OnDestroy()
    {
        harmony.UnpatchSelf();
        LogInfo($"Destroyed!");
    }
}
