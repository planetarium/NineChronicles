using System;
using Libplanet;
using Nekoyume;
using Nekoyume.Model.State;
using Nekoyume.UI;
using UnityEngine;

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

public class GameActionResultNullException : Exception
{
}

public class GameActionResultUnexpectedException : Exception
{
}

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

    public FailedToInstantiateGameObjectException(string name, Exception inner) : base(
        string.Format(MessageFormat, name), inner)
    {
    }
}

public class FailedToLoadResourceException<T> : Exception
{
    private const string MessageFormat = "Failed to load resource. type: `{0}`.";
    private const string MessageFormatWithPath = "Failed to load resource. type: `{0}`, path: `{1}`.";

    public FailedToLoadResourceException() : base(string.Format(MessageFormat, typeof(T).Name))
    {
    }

    public FailedToLoadResourceException(string path) : base(string.Format(MessageFormatWithPath, typeof(T).Name, path))
    {
    }

    public FailedToLoadResourceException(string path, Exception inner) : base(
        string.Format(MessageFormatWithPath, typeof(T).Name, path), inner)
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
    private const string MessageFormatWithGameObject = "Not found `{0}` component in {1}.";

    public NotFoundComponentException() : this(string.Format(MessageFormat, typeof(T).Name))
    {
    }

    public NotFoundComponentException(GameObject gameObject) : this(string.Format(MessageFormatWithGameObject,
        typeof(T).Name,
        gameObject.name))
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

    public SpineBoneNotFoundException(string slotName, Exception inner) : base(string.Format(MessageFormat, slotName),
        inner)
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

    public SpineSlotNotFoundException(string slotName, Exception inner) : base(string.Format(MessageFormat, slotName),
        inner)
    {
    }
}

public class SpineAttachmentNotFoundException : Exception
{
    private const string MessageFormat = "Not found `{0}` spine attachment.";

    public SpineAttachmentNotFoundException()
    {
    }

    public SpineAttachmentNotFoundException(string slotName) : base(string.Format(MessageFormat, slotName))
    {
    }

    public SpineAttachmentNotFoundException(string slotName, Exception inner) : base(
        string.Format(MessageFormat, slotName), inner)
    {
    }
}

public class SerializeFieldException : Exception
{
    public SerializeFieldException(string message) : base(message)
    {
    }
}

public class SerializeFieldNullException : Exception
{
    public SerializeFieldNullException()
    {
    }

    public SerializeFieldNullException(string message) : base(message)
    {
    }
}

public class AddOutOfSpecificRangeException<T> : Exception
{
    private const string MessageFormat = "Add out of specific range. type: `{0}`.";
    private const string MessageFormatWithRange = "Add out of specific range. type: `{0}`, specific range: `{1}`.";

    public AddOutOfSpecificRangeException() : base(string.Format(MessageFormat, typeof(T).Name))
    {
    }

    public AddOutOfSpecificRangeException(int specificRange) : base(string.Format(MessageFormatWithRange, typeof(T).Name,
        specificRange))
    {
    }

    public AddOutOfSpecificRangeException(string message, Exception inner) : base(message, inner)
    {
    }
}

public class AssetNotFoundException : Exception
{
    public AssetNotFoundException(string message) : base(message)
    {
    }
}

public class WidgetNotFoundException : Exception
{
    public WidgetNotFoundException(string widgetName) : base(widgetName)
    {
    }
}

public class WidgetNotFoundException<T> : Exception where T : Widget
{
    private const string MessageFormat = "Widget not found. type: `{0}`.";

    public WidgetNotFoundException() : base(string.Format(MessageFormat, typeof(T).Name))
    {
    }
}


public class FailedToSaveAsPrefabAssetException : Exception
{
    private const string MessageDefault = "Failed to save as prefab.";
    private const string MessageFormat = "Failed to save as prefab to `{0}`.";

    public FailedToSaveAsPrefabAssetException() : base(MessageDefault)
    {
    }

    public FailedToSaveAsPrefabAssetException(string path) : base(string.Format(MessageFormat, path))
    {
    }

    public FailedToSaveAsPrefabAssetException(string path, Exception inner) : base(string.Format(MessageFormat, path),
        inner)
    {
    }
}

public class FailedToInstantiateStateException<T> : Exception where T : State
{
    private const string MessageFormat = "Failed to instantiate state. type: `{0}`.";
    private const string MessageFormatWithAddress = "Failed to instantiate state. type: `{0}` / address: `{1}`.";

    public FailedToInstantiateStateException() : base(string.Format(MessageFormat, typeof(T).Name))
    {
    }

    public FailedToInstantiateStateException(Address address) : base(string.Format(MessageFormatWithAddress, typeof(T).Name, address))
    {
    }
}

public class InvalidSellingPriceException : Exception
{
    private const string MessageFormat = "Selling price of `{0}` is invaild. `{1}`.";

    public InvalidSellingPriceException(Nekoyume.UI.Model.ItemCountAndPricePopup popup) :
        base(string.Format(MessageFormat,
            popup.Item.Value.ItemBase.Value.GetLocalizedName(),
            popup.Price.Value))
    {

    }

    public InvalidSellingPriceException(Nekoyume.UI.Model.ItemCountableAndPricePopup popup) :
        base(string.Format(MessageFormat,
            popup.Item.Value.ItemBase.Value.GetLocalizedName(),
            popup.Price.Value))
    {

    }
}
