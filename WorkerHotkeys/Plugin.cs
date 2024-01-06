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

    private enum Races
    {
        Human,
        Beaver,
        Lizard,
        Foxes,
        Harpy
    }

    public static void LogInfo(object obj) => Instance.Logger.LogInfo(obj);
    public static void LogDebug(object obj) => Instance.Logger.LogDebug(obj);
    public static void LogError(object obj) => Instance.Logger.LogError(obj);
    
    private readonly List<ConfigEntry<KeyboardShortcut>> selectSlotShortcuts = [];
    private readonly List<ConfigEntry<KeyboardShortcut>> selectRaceShortcuts = [];
    private RacesHUD racesHud;

    private void Awake()
    {
        for (var i = 0; i < NUM_WORKER_SLOTS; ++i)
        {
            selectSlotShortcuts.Add(Config.Bind
            (
                new ConfigDefinition("Slots", $"SelectSlot{i + 1}"),
                new KeyboardShortcut(KeyCode.Keypad1 + i),
                new ConfigDescription($"Hotkey to select race in slot {i + 1}")
            ));
        }

        foreach (var race in Enum.GetValues(typeof(Races)))
        {
            selectRaceShortcuts.Add(Config.Bind
            (
                new ConfigDefinition("Races", $"Select{race}"),
                new KeyboardShortcut(KeyCode.Keypad1 + NUM_WORKER_SLOTS + (int)race),
                new ConfigDescription($"Hotkey to select {race}")
            ));
        }

        Instance = this;
        harmony = Harmony.CreateAndPatchAll(typeof(Plugin));
        LogInfo($"Initialized!");
        gameObject.hideFlags = HideFlags.HideAndDontSave;
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

        selectedIndex = selectRaceShortcuts.FindIndex(item => item.Value.IsDown());
        if (selectedIndex >= 0)
        {
            var race = (Races)selectedIndex;
            return racesHud.slots.FirstOrDefault
            (
                slot => slot.race.Name == race.ToString()
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

    [HarmonyPatch(typeof(RacesHUD), nameof(RacesHUD.SetUpSlots))]
    [HarmonyPostfix]
    private static void RacesHUD_AfterSetUpSlots(RacesHUD __instance)
    {
        Instance.racesHud = __instance;
    }

    private void OnDestroy()
    {
        harmony.UnpatchSelf();
        LogInfo($"Destroyed!");
    }
}
