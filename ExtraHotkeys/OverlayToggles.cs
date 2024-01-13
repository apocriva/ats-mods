using System;
using System.Collections.Generic;
using System.Text;
using Eremite;
using Eremite.Controller;
using Eremite.MapObjects.UI;
using HarmonyLib;
using UnityEngine.InputSystem;

namespace ExtraHotkeys;

public class OverlayToggles : GameMB
{
    private enum Overlays
    {
        Blight,
        Deposit,
        Rainpunk,
        Recipe,
    }

    private static Dictionary<Overlays, List<Action<InputAction.CallbackContext>>> showCallbacks = new();
    private static Dictionary<Overlays, List<Action<InputAction.CallbackContext>>> hideCallbacks = new();
    private static Dictionary<Overlays, bool> isShowing = new();

    [HarmonyPatch(typeof(GameController), nameof(Eremite.Controller.GameController.StartGame))]
    [HarmonyPostfix]
    private static void OnGameStarted()
    {
        if (Plugin.GameObject.GetComponent<OverlayToggles>() == null)
            Plugin.GameObject.AddComponent<OverlayToggles>();

        foreach (Overlays overlay in Enum.GetValues(typeof(Overlays)))
        {
            showCallbacks[overlay] = [];
            hideCallbacks[overlay] = [];
            isShowing[overlay] = false;
        }
    }

    [HarmonyPatch(typeof(DepositsIndicatorsController), nameof(DepositsIndicatorsController.OnEnable))]
    [HarmonyPrefix]
    private static bool DepositsIndicatorsController_OnEnable(DepositsIndicatorsController __instance)
    {
        showCallbacks[Overlays.Deposit].Add(__instance.HighlightAllInput);
        hideCallbacks[Overlays.Deposit].Add(__instance.UnhighlightAllInput);
        return false;
    }

    [HarmonyPatch(typeof(DepositsIndicatorsController), nameof(DepositsIndicatorsController.OnDisable))]
    [HarmonyPrefix]
    private static bool DepositsIndicatorsController_OnDisable(DepositsIndicatorsController __instance)
    {
        showCallbacks[Overlays.Deposit].Remove(__instance.HighlightAllInput);
        hideCallbacks[Overlays.Deposit].Remove(__instance.UnhighlightAllInput);
        return false;
    }

    private void Awake()
    {
        InputConfig.HighlightResources.performed += OnHighlightResourcesPressed;
        InputConfig.HighlightResources.canceled += OnHighlightResourcesReleased;
    }

    private void OnDestroy()
    {
        InputConfig.HighlightResources.performed -= OnHighlightResourcesPressed;
        InputConfig.HighlightResources.canceled -= OnHighlightResourcesReleased;
    }

    private void OnHighlightResourcesPressed(InputAction.CallbackContext context)
    {
        if (isShowing[Overlays.Deposit])
        {
            isShowing[Overlays.Deposit] = false;
            foreach (var action in hideCallbacks[Overlays.Deposit])
                action.Invoke(context);
        }
        else
        {
            isShowing[Overlays.Deposit] = true;
            foreach (var action in showCallbacks[Overlays.Deposit])
                action.Invoke(context);
        }
    }

    private void OnHighlightResourcesReleased(InputAction.CallbackContext context)
    {
    }
}
