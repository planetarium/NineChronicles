using System;
using System.Collections.Generic;

namespace Nekoyume.UI
{
    [Serializable]
    public enum DialogEmojiType
    {
        None = 0,
        Idle = 1,
        Reaction = 2,
        Question = 3,
    }

    public class DialogEmojiTypeEqualityComparer : IEqualityComparer<DialogEmojiType>
    {
        public bool Equals(DialogEmojiType x, DialogEmojiType y)
        {
            return x == y;
        }

        public int GetHashCode(DialogEmojiType obj)
        {
            return (int) obj;
        }
    }
}
