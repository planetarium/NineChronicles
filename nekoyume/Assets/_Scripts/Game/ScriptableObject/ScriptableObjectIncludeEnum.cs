using System;
using System.Collections.Generic;
using UnityEngine;

public class ScriptableObjectIncludeEnum<T> : ScriptableObject where T : Enum
{
    public T type;
    public List<string> enums;

    public List<string> Enums
    {
        get => enums;
        set => enums = value;
    }
}
