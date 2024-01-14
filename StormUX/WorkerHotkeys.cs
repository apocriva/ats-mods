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

    private const int NumWorkerSlots = 3;
    
    private static readonly List<ConfigEntry<KeyboardShortcut>> SelectSlotShortcuts = [];
    private static readonly Dictionary<string, ConfigEntry<KeyboardShortcut>> SelectRaceShortcuts = [];
    private static RacesHUD _racesHud;

    [HarmonyPatch(typeof(MainController), nameof(MainController.OnServicesReady))]
    [HarmonyPostfix]
    private static void OnServicesReady()
    {
        for (var i = 0; i < NumWorkerSlots; ++i)
        {
            SelectSlotShortcuts.Add(Config.Bind
            (
                new ConfigDefinition("WorkerSlotHotkeys", $"SelectSlot{i + 1}"),
                new KeyboardShortcut(KeyCode.Keypad1 + i),
                new ConfigDescription($"Hotkey to select race in slot {i + 1}")
            ));
        }

        foreach (var race in Settings.Races)
        {
            SelectRaceShortcuts.Add(race.Name, Config.Bind
            (
                new ConfigDefinition("WorkerRaceHotkeys", $"Select{race.Name}"),
                default(KeyboardShortcut),
                new ConfigDescription($"Hotkey to select {race.Name}")
            ));
        }
    }

    [HarmonyPatch(typeof(GameController), nameof(Eremite.Controller.GameController.StartGame))]
    [HarmonyPostfix]
    private static void OnGameStarted()
    {
        if (Plugin.GameObject.GetComponent<WorkerHotkeys>() == null)
            Plugin.GameObject.AddComponent<WorkerHotkeys>();
    }

    [HarmonyPatch(typeof(RacesHUD), nameof(RacesHUD.SetUpSlots))]
    [HarmonyPostfix]
    private static void RacesHUD_AfterSetUpSlots(RacesHUD __instance)
    {
        _racesHud = __instance;
    }

    private void Update()
    {
        if (!IsGameActive || InputService.IsLocked() || _racesHud == null)
            return;

        var slot = GetKeyDownSlot();
        if (slot == null)
            return;

        if (ModeService.RaceMode.Value && slot.race == GameBlackboardService.PickedRace.Value)
            ModeService.Back();
        else
            _racesHud.OnSlotClicked(slot);
    }

    private static RacesHUDSlot GetKeyDownSlot()
    {
        var selectedIndex = SelectSlotShortcuts.FindIndex(item => item.Value.IsDown());
        if (selectedIndex >= 0)
            return GetHudSlotFromShortcutIndex(selectedIndex);

        var selectedRace = SelectRaceShortcuts.FirstOrDefault(item => item.Value.Value.IsDown()).Key;
        if (!string.IsNullOrEmpty(selectedRace))
        {
            return _racesHud.slots.FirstOrDefault
            (
                slot => slot.race.Name == selectedRace
                        && slot.IsRevealed()
            );
        }

        return null;
    }

    private static RacesHUDSlot GetHudSlotFromShortcutIndex(int shortcutIndex)
    {
        var shortcutCheckIndex = 0;
        foreach (var slot in _racesHud.slots)
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
