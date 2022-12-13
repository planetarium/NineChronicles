using System;
using System.Runtime.Serialization;

namespace Nekoyume.Model.Rune
{
    [Serializable]
    public class RuneNotFoundException : Exception
    {
        public RuneNotFoundException(string message) : base(message)
        {
        }

        protected RuneNotFoundException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class RuneCostNotFoundException : Exception
    {
        public RuneCostNotFoundException(string message) : base(message)
        {
        }

        protected RuneCostNotFoundException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class RuneCostDataNotFoundException : Exception
    {
        public RuneCostDataNotFoundException(string message) : base(message)
        {
        }

        protected RuneCostDataNotFoundException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class TryCountIsZeroException : Exception
    {
        public TryCountIsZeroException(string msg) : base(msg)
        {
        }

        public TryCountIsZeroException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class RuneListNotFoundException : Exception
    {
        public RuneListNotFoundException(string message) : base(message)
        {
        }

        protected RuneListNotFoundException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class RuneStateNotFoundException : Exception
    {
        public RuneStateNotFoundException(string message) : base(message)
        {
        }

        protected RuneStateNotFoundException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class SlotNotFoundException : Exception
    {
        public SlotNotFoundException(string message) : base(message)
        {
        }

        protected SlotNotFoundException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class SlotIsLockedException : Exception
    {
        public SlotIsLockedException(string message) : base(message)
        {
        }

        protected SlotIsLockedException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class SlotIsAlreadyUnlockedException : Exception
    {
        public SlotIsAlreadyUnlockedException(string message) : base(message)
        {
        }

        protected SlotIsAlreadyUnlockedException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class SlotRuneTypeException : Exception
    {
        public SlotRuneTypeException(string message) : base(message)
        {
        }

        protected SlotRuneTypeException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class IsEquippableRuneException : Exception
    {
        public IsEquippableRuneException(string message) : base(message)
        {
        }

        protected IsEquippableRuneException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class RuneInfosIsEmptyException : Exception
    {
        public RuneInfosIsEmptyException(string message) : base(message)
        {
        }

        protected RuneInfosIsEmptyException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class DuplicatedRuneSlotIndexException : Exception
    {

        public DuplicatedRuneSlotIndexException(string message) : base(message)
        {
        }

        protected DuplicatedRuneSlotIndexException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class DuplicatedRuneIdException : Exception
    {

        public DuplicatedRuneIdException(string message) : base(message)
        {
        }

        protected DuplicatedRuneIdException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class IsUsableSlotException : Exception
    {

        public IsUsableSlotException(string message) : base(message)
        {
        }

        protected IsUsableSlotException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class MismatchRuneSlotTypeException : Exception
    {
        public MismatchRuneSlotTypeException(string message) : base(message)
        {
        }

        protected MismatchRuneSlotTypeException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }
}
