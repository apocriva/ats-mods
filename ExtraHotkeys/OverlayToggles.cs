using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Configuration;
using Eremite;
using Eremite.Buildings.UI;
using Eremite.Controller;
using Eremite.MapObjects.UI;
using HarmonyLib;
using UnityEngine.InputSystem;

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

    private void Awake()
    {
        InputConfig.BlightOverlay.performed += OnBlightPressed;
        InputConfig.BlightOverlay.canceled += OnBlightReleased;
        InputConfig.HighlightResources.performed += OnDepositPressed;
        InputConfig.HighlightResources.canceled += OnDepositReleased;
    }

    private void OnDestroy()
    {
        InputConfig.BlightOverlay.performed -= OnBlightPressed;
        InputConfig.BlightOverlay.canceled -= OnBlightReleased;
        InputConfig.HighlightResources.performed -= OnDepositPressed;
        InputConfig.HighlightResources.canceled -= OnDepositReleased;
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
