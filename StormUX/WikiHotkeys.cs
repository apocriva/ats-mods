using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Configuration;
using Eremite;
using Eremite.Controller;
using Eremite.View.UI.Wiki;
using HarmonyLib;
using UnityEngine;

namespace StormUX;

public class WikiHotkeys : GameMB
{
    private static WikiHotkeys Instance;
    private static ConfigFile Config => Plugin.Instance.Config;

    private enum WikiPanels
    {
        None,
        Basic,
        Buildings,
        Effects,
        Traders,
        Races
    }
    private Dictionary<WikiPanels, ConfigEntry<KeyboardShortcut>> wikiShortcuts = new();

    private WikiPopup wikiPopup;
    private WikiBasicPanel wikiBasicPanel;
    private WikiBuildingsPanel wikiBuildingsPanel;
    private WikiEffectsPanel wikiEffectsPanel;
    private WikiTradersPanel wikiTradersPanel;
    private WikiRacesPanel wikiRacesPanel;
    
    [HarmonyPatch(typeof(WikiPopup), nameof(WikiPopup.SetUp))]
    [HarmonyPostfix]
    private static void OnWikiPopup_SetUp(WikiPopup __instance)
    {
        Instance = Plugin.GameObject.GetComponent<WikiHotkeys>();
        if (Instance == null)
            Instance = Plugin.GameObject.AddComponent<WikiHotkeys>();

        Instance.wikiPopup = __instance;
        foreach (var item in __instance.panels)
        {
            switch (item)
            {
                case WikiBasicPanel panel:
                    Instance.wikiBasicPanel = panel;
                    break;
                case WikiBuildingsPanel panel:
                    Instance.wikiBuildingsPanel = panel;
                    break;
                case WikiEffectsPanel panel:
                    Instance.wikiEffectsPanel = panel;
                    break;
                case WikiTradersPanel panel:
                    Instance.wikiTradersPanel = panel;
                    break;
                case WikiRacesPanel panel:
                    Instance.wikiRacesPanel = panel;
                    break;
            }
        }
    }

    private void Awake()
    {
        SetupShortcuts();
    }

    private void SetupShortcuts()
    {
        wikiShortcuts[WikiPanels.Basic] = Config.Bind
        (
            new ConfigDefinition("Wiki", "Basic"),
            new KeyboardShortcut(KeyCode.F1),
            new ConfigDescription("Hotkey to open Encyclopedia -> Basic")
        );
        wikiShortcuts[WikiPanels.Buildings] = Config.Bind
        (
            new ConfigDefinition("Wiki", "Buildings"),
            new KeyboardShortcut(KeyCode.F2),
            new ConfigDescription("Hotkey to open Encyclopedia -> Buildings")
        );
        wikiShortcuts[WikiPanels.Effects] = Config.Bind
        (
            new ConfigDefinition("Wiki", "Effects"),
            new KeyboardShortcut(KeyCode.F3),
            new ConfigDescription("Hotkey to open Encyclopedia -> Effects")
        );
        wikiShortcuts[WikiPanels.Traders] = Config.Bind
        (
            new ConfigDefinition("Wiki", "Traders"),
            new KeyboardShortcut(KeyCode.F4),
            new ConfigDescription("Hotkey to open Encyclopedia -> Traders")
        );
        wikiShortcuts[WikiPanels.Races] = Config.Bind
        (
            new ConfigDefinition("Wiki", "Races"),
            new KeyboardShortcut(KeyCode.F5),
            new ConfigDescription("Hotkey to open Encyclopedia -> Races")
        );
    }

    private void Update()
    {
        if (!IsGameActive || InputService.IsLocked())
            return;

        // TODO: This is an awful way to listen for a keypress.
        var selectedPanel = wikiShortcuts.FirstOrDefault(item => item.Value.Value.IsDown()).Key;
        if (selectedPanel == WikiPanels.None)
            return;

#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
        WikiCategoryPanel panelToShow = selectedPanel switch
#pragma warning restore CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
        {
            WikiPanels.Basic => wikiBasicPanel,
            WikiPanels.Buildings => wikiBuildingsPanel,
            WikiPanels.Effects => wikiEffectsPanel,
            WikiPanels.Traders => wikiTradersPanel,
            WikiPanels.Races => wikiRacesPanel,
        };

        if (wikiPopup.IsShown() && wikiPopup.current == panelToShow)
            wikiPopup.Hide();
        else
            wikiPopup.OnCategoryRequested(panelToShow);
    }
}
