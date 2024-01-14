using System;
using System.Collections.Generic;
using System.Text;

namespace OptionsExtensions;

public class OnInitializeAttribute(int priority = 0) : Attribute
{
    public int Priority { get; } = priority;
}
