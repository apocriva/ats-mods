using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using BepInEx.Configuration;
using Eremite;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WorkerHotkeys
{
    public class PluginState
    {
        public InputAction SelectWorker1Hotkey;

        public PluginState()
        {
            SelectWorker1Hotkey = new InputAction
            (
                name: "SelectWorker1",
                type: InputActionType.Button,
                expectedControlType: "Button"
            );
        }
    }
}
