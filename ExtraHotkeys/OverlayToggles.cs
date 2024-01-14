using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Configuration;
using Eremite;
using Eremite.Buildings.UI;
using Eremite.Controller;
using Eremite.MapObjects.UI;
using Eremite.View.Popups.GameMenu;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ExtraHotkeys;

public class OverlayToggles : GameMB
{
    private static ConfigFile Config => Plugin.Instance.Config;

    private enum Overlays
    {
        Blight,
        Deposit,
        Rainpunk,
        Recipe,
    }

    private static ConfigEntry<bool> isToggleEnabled;
    private static Dictionary<Overlays, List<Action<InputAction.CallbackContext>>> showCallbacks = new();
    private static Dictionary<Overlays, List<Action<InputAction.CallbackContext>>> hideCallbacks = new();
    private static Dictionary<Overlays, bool> isShowing = new();

    [HarmonyPatch(typeof(MainController), nameof(MainController.OnServicesReady))]
    [HarmonyPostfix]
    private static void OnServicesReady()
    {
        isToggleEnabled = Config.Bind
        (
            new ConfigDefinition("Misc", "OverlayToggleEnabled"),
            false,
            new ConfigDescription("Set to true to enable overlay toggle functionality. False for base game behavior.")
        );

        foreach (Overlays overlay in Enum.GetValues(typeof(Overlays)))
        {
            showCallbacks[overlay] = [];
            hideCallbacks[overlay] = [];
            isShowing[overlay] = false;
        }
    }

    [HarmonyPatch(typeof(OptionsExtensions), nameof(OptionsExtensions.Initialize))]
    [HarmonyPostfix]
    private static void InitializeOptions()
    {
        OptionsExtensions.CreateToggle
        (
            "Overlay Toggling Enabled",
            () => isToggleEnabled.Value,
            newValue => isToggleEnabled.Value = newValue
        );
    }

    [HarmonyPatch(typeof(GameController), nameof(Eremite.Controller.GameController.StartGame))]
    [HarmonyPostfix]
    private static void OnGameStarted()
    {
        if (Plugin.GameObject.GetComponent<OverlayToggles>() == null)
            Plugin.GameObject.AddComponent<OverlayToggles>();
    }

    [HarmonyPatch(typeof(BlightOverlaysManager), nameof(BlightOverlaysManager.OnEnable))]
    [HarmonyPrefix]
    private static bool Blight_OnEnable(BlightOverlaysManager __instance)
    {
        showCallbacks[Overlays.Blight].Add(__instance.OnHighlightStarted);
        hideCallbacks[Overlays.Blight].Add(__instance.OnHighlightFinished);
        return false;
    }

    [HarmonyPatch(typeof(BlightOverlaysManager), nameof(BlightOverlaysManager.OnDisable))]
    [HarmonyPrefix]
    private static bool Blight_OnDisable(BlightOverlaysManager __instance)
    {
        showCallbacks[Overlays.Blight].Remove(__instance.OnHighlightStarted);
        hideCallbacks[Overlays.Blight].Remove(__instance.OnHighlightFinished);
        return false;
    }

    [HarmonyPatch(typeof(DepositsIndicatorsController), nameof(DepositsIndicatorsController.OnEnable))]
    [HarmonyPrefix]
    private static bool Deposit_OnEnable(DepositsIndicatorsController __instance)
    {
        showCallbacks[Overlays.Deposit].Add(__instance.HighlightAllInput);
        hideCallbacks[Overlays.Deposit].Add(__instance.UnhighlightAllInput);
        return false;
    }

    [HarmonyPatch(typeof(DepositsIndicatorsController), nameof(DepositsIndicatorsController.OnDisable))]
    [HarmonyPrefix]
    private static bool Deposit_OnDisable(DepositsIndicatorsController __instance)
    {
        showCallbacks[Overlays.Deposit].Remove(__instance.HighlightAllInput);
        hideCallbacks[Overlays.Deposit].Remove(__instance.UnhighlightAllInput);
        return false;
    }

    [HarmonyPatch(typeof(OreIndicatorsController), nameof(OreIndicatorsController.OnEnable))]
    [HarmonyPrefix]
    private static bool Ore_OnEnable(OreIndicatorsController __instance)
    {
        showCallbacks[Overlays.Deposit].Add(__instance.HighlightAll);
        hideCallbacks[Overlays.Deposit].Add(__instance.UnhighlightAll);
        return false;
    }

    [HarmonyPatch(typeof(OreIndicatorsController), nameof(OreIndicatorsController.OnDisable))]
    [HarmonyPrefix]
    private static bool Ore_OnDisable(OreIndicatorsController __instance)
    {
        showCallbacks[Overlays.Deposit].Remove(__instance.HighlightAll);
        hideCallbacks[Overlays.Deposit].Remove(__instance.UnhighlightAll);
        return false;
    }

    [HarmonyPatch(typeof(RainpunkOverlaysManager), nameof(RainpunkOverlaysManager.OnEnable))]
    [HarmonyPrefix]
    private static bool Rainpunk_OnEnable(RainpunkOverlaysManager __instance)
    {
        showCallbacks[Overlays.Rainpunk].Add(__instance.OnHighlightStarted);
        hideCallbacks[Overlays.Rainpunk].Add(__instance.OnHighlightFinished);
        return false;
    }

    [HarmonyPatch(typeof(RainpunkOverlaysManager), nameof(RainpunkOverlaysManager.OnDisable))]
    [HarmonyPrefix]
    private static bool Rainpunk_OnDisable(RainpunkOverlaysManager __instance)
    {
        showCallbacks[Overlays.Rainpunk].Remove(__instance.OnHighlightStarted);
        hideCallbacks[Overlays.Rainpunk].Remove(__instance.OnHighlightFinished);
        return false;
    }

    [HarmonyPatch(typeof(RecipesOverlaysManager), nameof(RecipesOverlaysManager.OnEnable))]
    [HarmonyPrefix]
    private static bool Recipe_OnEnable(RecipesOverlaysManager __instance)
    {
        showCallbacks[Overlays.Recipe].Add(__instance.OnHighlightStarted);
        hideCallbacks[Overlays.Recipe].Add(__instance.OnHighlightFinished);
        return false;
    }

    [HarmonyPatch(typeof(RecipesOverlaysManager), nameof(RecipesOverlaysManager.OnDisable))]
    [HarmonyPrefix]
    private static bool Recipe_OnDisable(RecipesOverlaysManager __instance)
    {
        showCallbacks[Overlays.Recipe].Remove(__instance.OnHighlightStarted);
        hideCallbacks[Overlays.Recipe].Remove(__instance.OnHighlightFinished);
        return false;
    }

    private void Awake()
    {
        InputConfig.BlightOverlay.performed += OnBlightPressed;
        InputConfig.BlightOverlay.canceled += OnBlightReleased;
        InputConfig.HighlightResources.performed += OnDepositPressed;
        InputConfig.HighlightResources.canceled += OnDepositReleased;
        InputConfig.RainpunkOverlay.performed += OnRainpunkPressed;
        InputConfig.RainpunkOverlay.canceled += OnRainpunkReleased;
        InputConfig.HighlightRecipes.performed += OnRecipePressed;
        InputConfig.HighlightRecipes.canceled += OnRecipeReleased;
    }

    private void OnDestroy()
    {
        InputConfig.BlightOverlay.performed -= OnBlightPressed;
        InputConfig.BlightOverlay.canceled -= OnBlightReleased;
        InputConfig.HighlightResources.performed -= OnDepositPressed;
        InputConfig.HighlightResources.canceled -= OnDepositReleased;
        InputConfig.RainpunkOverlay.performed -= OnRainpunkPressed;
        InputConfig.RainpunkOverlay.canceled -= OnRainpunkReleased;
        InputConfig.HighlightRecipes.performed -= OnRecipePressed;
        InputConfig.HighlightRecipes.canceled -= OnRecipeReleased;
    }

    private static void OnBlightPressed(InputAction.CallbackContext context)
    {
        OnPressed(Overlays.Blight, context);
    }

    private static void OnBlightReleased(InputAction.CallbackContext context)
    {
        OnReleased(Overlays.Blight, context);
    }

    private static void OnDepositPressed(InputAction.CallbackContext context)
    {
        OnPressed(Overlays.Deposit, context);
    }

    private static void OnDepositReleased(InputAction.CallbackContext context)
    {
        OnReleased(Overlays.Deposit, context);
    }

    private static void OnRainpunkPressed(InputAction.CallbackContext context)
    {
        OnPressed(Overlays.Rainpunk, context);
    }

    private static void OnRainpunkReleased(InputAction.CallbackContext context)
    {
        OnReleased(Overlays.Rainpunk, context);
    }

    private static void OnRecipePressed(InputAction.CallbackContext context)
    {
        OnPressed(Overlays.Recipe, context);
    }

    private static void OnRecipeReleased(InputAction.CallbackContext context)
    {
        OnReleased(Overlays.Recipe, context);
    }

    private static void OnPressed(Overlays overlay, InputAction.CallbackContext context)
    {
        if (!isToggleEnabled.Value || !isShowing[overlay])
        {
            isShowing[overlay] = true;
            foreach (var action in showCallbacks[overlay])
                action.Invoke(context);
        }
        else
        {
            isShowing[overlay] = false;
            foreach (var action in hideCallbacks[overlay])
                action.Invoke(context);
        }
    }

    private static void OnReleased(Overlays overlay, InputAction.CallbackContext context)
    {
        if (!isToggleEnabled.Value)
        {
            isShowing[overlay] = false;
            foreach (var action in hideCallbacks[overlay])
                action.Invoke(context);
        }
    }
}
