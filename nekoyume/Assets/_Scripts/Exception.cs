using System;
using Spine;
using UnityEngine;

public class UnexpectedOperationException : Exception
{
}

public class InvalidActionException : Exception
{
    public InvalidActionException()
    {
    }

    public InvalidActionException(string message) : base(message)
    {
    }

    public InvalidActionException(string message, Exception inner) : base(message, inner)
    {
    }
}

public class GameActionResultNullException : Exception {}
public class GameActionResultUnexpectedException : Exception {}

public class FailedToInstantiateGameObjectException : Exception
{
    private const string MessageDefault = "Failed to instantiate a `GameObject`.";
    private const string MessageFormat = "Failed to instantiate a `GameObject` by prefab named `{0}`.";
    
    public FailedToInstantiateGameObjectException() : base(MessageDefault)
    {
        
    }
    
    public FailedToInstantiateGameObjectException(string name) : base(string.Format(MessageFormat, name))
    {
    }

    public FailedToInstantiateGameObjectException(string name, Exception inner) : base(string.Format(MessageFormat, name), inner)
    {
    }
}

public class FailedToLoadResourceException<T> : Exception
{
    private const string MessageFormat0 = "Failed to load resource. type : `{0}`.";
    private const string MessageFormat1 = "Failed to load resource. type : `{0}`, path : `{1}`.";

    private static readonly string TypeName = typeof(T).Name;
    
    public FailedToLoadResourceException() : base(string.Format(MessageFormat0, TypeName))
    {
    }

    public FailedToLoadResourceException(string path) : base(string.Format(MessageFormat1, TypeName, path))
    {
    }

    public FailedToLoadResourceException(string path, Exception inner) : base(string.Format(MessageFormat1, TypeName, path), inner)
    {
    }
}

public class NotFoundGameObjectException : Exception
{
    private const string MessageDefault = "Not found `GameObject`.";
    private const string MessageFormat = "Not found `GameObject` named `{0}`.";

    public NotFoundGameObjectException() : base(MessageDefault)
    {
    }

    public NotFoundGameObjectException(string name) : base(string.Format(MessageFormat, name))
    {
    }

    public NotFoundGameObjectException(string name, Exception inner) : base(string.Format(MessageFormat, name), inner)
    {
    }
}

public class NotFoundComponentException<T> : Exception where T : Component
{
    private const string MessageFormat = "Not found `{0}` component.";

    private static readonly string TypeName = typeof(T).Name;
    
    public NotFoundComponentException() : this(string.Format(MessageFormat, TypeName))
    {
    }

    public NotFoundComponentException(string message) : base(message)
    {
    }

    public NotFoundComponentException(string message, Exception inner) : base(message, inner)
    {
    }
}

public class SpineBoneNotFoundException : Exception
{
    private const string MessageFormat = "Not found `{0}` spine bone.";
    
    public SpineBoneNotFoundException()
    {
    }

    public SpineBoneNotFoundException(string slotName) : base(string.Format(MessageFormat, slotName))
    {
    }

    public SpineBoneNotFoundException(string slotName, Exception inner) : base(string.Format(MessageFormat, slotName), inner)
    {
    }
}

public class SpineSlotNotFoundException : Exception
{
    private const string MessageFormat = "Not found `{0}` spine slot.";
    
    public SpineSlotNotFoundException()
    {
    }

    public SpineSlotNotFoundException(string slotName) : base(string.Format(MessageFormat, slotName))
    {
    }

    public SpineSlotNotFoundException(string slotName, Exception inner) : base(string.Format(MessageFormat, slotName), inner)
    {
    }
}

public class SerializeFieldNullException : Exception
{
    
}

public class AddOutOfSpecificRangeException<T> : Exception
{
    private const string MessageFormat0 = "Add out of specific range. type : `{0}`.";
    private const string MessageFormat1 = "Add out of specific range. type : `{0}`, specific range : `{1}`";

    private static readonly string TypeName = typeof(T).Name;

    public AddOutOfSpecificRangeException() : base(string.Format(MessageFormat0, TypeName))
    {
    }

    public AddOutOfSpecificRangeException(int specificRange) : base(string.Format(MessageFormat1, TypeName,
        specificRange))
    {
    }

    public AddOutOfSpecificRangeException(string message, Exception inner) : base(message, inner)
    {
    }
}

public class AssetNotFoundException : Exception
{
    public AssetNotFoundException(string assetPath) : base(assetPath)
    {
    }
}
