using System;
using System.Collections.Generic;
using System.Text;

namespace OptionsExtensions;

/// <summary>
/// OptionsExtensions will call this method after collecting all the necessary
/// prefabs and whatnot when the options popup is opened for the first time, in
/// order from highest to lowest priority.
/// </summary>
public class OnInitializeAttribute(int priority = 0) : Attribute
{
    public int Priority { get; } = priority;
}
