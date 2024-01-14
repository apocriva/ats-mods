using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Configuration;
using Eremite;
using Eremite.Controller;
using Eremite.View.HUD;
using HarmonyLib;
using UnityEngine;

namespace StormUX;

internal class WorkerHotkeys : GameMB
{
    private static ConfigFile Config => Plugin.Instance.Config;

    private const int NUM_WORKER_SLOTS = 3;

    private static readonly List<string> races = [];
    
    private static readonly List<ConfigEntry<KeyboardShortcut>> selectSlotShortcuts = [];
    private static readonly Dictionary<string, ConfigEntry<KeyboardShortcut>> selectRaceShortcuts = [];
    private static RacesHUD racesHud;

    [HarmonyPatch(typeof(MainController), nameof(MainController.OnServicesReady))]
    [HarmonyPostfix]
    private static void OnServicesReady()
    {
        for (var i = 0; i < NUM_WORKER_SLOTS; ++i)
        {
            selectSlotShortcuts.Add(Config.Bind
            (
                new ConfigDefinition("WorkerSlotHotkeys", $"SelectSlot{i + 1}"),
                new KeyboardShortcut(KeyCode.Keypad1 + i),
                new ConfigDescription($"Hotkey to select race in slot {i + 1}")
            ));
        }

        foreach (var race in Settings.Races)
        {
            races.Add(race.Name);

            selectRaceShortcuts.Add(race.Name, Config.Bind
            (
                new ConfigDefinition("WorkerRaceHotkeys", $"Select{race.Name}"),
                default(KeyboardShortcut),
                new ConfigDescription($"Hotkey to select {race.Name}")
            ));
        }
    }

    [HarmonyPatch(typeof(RacesHUD), nameof(RacesHUD.SetUpSlots))]
    [HarmonyPostfix]
    private static void RacesHUD_AfterSetUpSlots(RacesHUD __instance)
    {
        racesHud = __instance;
    }

    private void Update()
    {
        if (!IsGameActive || InputService.IsLocked() || racesHud == null)
            return;

        var slot = GetKeyDownSlot();
        if (slot == null)
            return;

        if (ModeService.RaceMode.Value && slot.race == GameBlackboardService.PickedRace.Value)
            ModeService.Back();
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
}
