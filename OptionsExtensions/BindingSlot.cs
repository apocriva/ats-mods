using System;
using BepInEx.Configuration;
using Eremite;
using TMPro;
using UnityEngine.UI;

namespace OptionsExtensions;

internal class BindingSlot : MB
{
    private readonly TMP_Text _label;
    private readonly TMP_Text _current;
    private readonly Button _button;

    private object _context;
    private Action<object> _onClick;

    public BindingSlot()
    {
        _label = FindChild("KeyLabel").GetComponent<TMP_Text>();
        _current = FindChild("Button/Text").GetComponent<TMP_Text>();
        _button = FindChild("Button").GetComponent<Button>();
    }

    public void SetUp(string label)
    {
        _label.text = label;
        _button.onClick.AddListener(OnClick);
    }

    public void SetData(KeyboardShortcut shortcut, object context, Action<object> onClick)
    {
        _current.text = shortcut.ToString();
    }

    private void OnClick()
    {
        _onClick?.Invoke(_context);
    }
}
