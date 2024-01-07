using BepInEx;
using HarmonyLib;
using Eremite;
using Eremite.Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using Eremite.View.HUD;
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

    private readonly List<string> races = [];
    
    private readonly List<ConfigEntry<KeyboardShortcut>> selectSlotShortcuts = [];
    private readonly Dictionary<string, ConfigEntry<KeyboardShortcut>> selectRaceShortcuts = [];
    private RacesHUD racesHud;

    private void Awake()
    {

        Instance = this;
        harmony = Harmony.CreateAndPatchAll(typeof(Plugin));
        gameObject.hideFlags = HideFlags.HideAndDontSave;

        for (var i = 0; i < NUM_WORKER_SLOTS; ++i)
        {
            selectSlotShortcuts.Add(Config.Bind
            (
                new ConfigDefinition("Slots", $"SelectSlot{i + 1}"),
                new KeyboardShortcut(KeyCode.Keypad1 + i),
                new ConfigDescription($"Hotkey to select race in slot {i + 1}")
            ));
        }

        LogDebug($"Initialized!");
    }

    private void Update()
    {
        if (!GameController.IsGameActive || MB.InputService.IsLocked() || racesHud == null)
            return;

        var slot = GetKeyDownSlot();
        if (slot == null)
            return;

        if (GameMB.ModeService.RaceMode.Value && slot.race == GameMB.GameBlackboardService.PickedRace.Value)
            GameMB.ModeService.Back();
        else
            racesHud.OnSlotClicked(slot);
    }

    private RacesHUDSlot GetKeyDownSlot()
    {
        var selectedIndex = selectSlotShortcuts.FindIndex(item => item.Value.IsDown());
        if (selectedIndex >= 0)
        {
            return GetHudSlotFromShortcutIndex(selectedIndex);
        }

        var selectedRace = selectRaceShortcuts.FirstOrDefault(item => item.Value.Value.IsDown()).Key;
        if (!string.IsNullOrEmpty(selectedRace))
        {
            return racesHud.slots.FirstOrDefault
            (
                slot => slot.race.Name == selectedRace
                    && slot.IsRevealed()
            );
        }

        return null;
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

    private void OnDestroy()
    {
        harmony?.UnpatchSelf();
        LogDebug($"Destroyed!");
    }

    [HarmonyPatch(typeof(MainController), nameof(MainController.OnServicesReady))]
    [HarmonyPostfix]
    private static void OnServicesReady()
    {
        foreach (var race in MB.Settings.Races)
        {
            Instance.races.Add(race.Name);

            Instance.selectRaceShortcuts.Add(race.Name, Instance.Config.Bind
            (
                new ConfigDefinition("Races", $"Select{race.Name}"),
                default(KeyboardShortcut),
                new ConfigDescription($"Hotkey to select {race.Name}")
            ));
        }
    }

    [HarmonyPatch(typeof(RacesHUD), nameof(RacesHUD.SetUpSlots))]
    [HarmonyPostfix]
    private static void RacesHUD_AfterSetUpSlots(RacesHUD __instance)
    {
        Instance.racesHud = __instance;
    }
}
