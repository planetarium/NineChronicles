using System;
using System.Collections.Generic;

namespace Nekoyume.UI
{
    [Serializable]
    public enum DialogEmojiType
    {
        None = 0,
        Idle,
        Reaction,
        Question,
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
