using System;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    [Serializable]
    public class GuideDialogData : ITutorialData
    {
        public TutorialItemType type = TutorialItemType.Dialog;

        public DialogEmojiType emojiType;

        public DialogCommaType commaType;

        public string script;

        public RectTransform target;

        public TutorialItemType Type => type;

        public GuideDialogData(
            DialogEmojiType emojiType,
            DialogCommaType commaType,
            string script,
            RectTransform target)
        {
            this.emojiType = emojiType;
            this.commaType = commaType;
            this.script = script;
            this.target = target;
        }
    }
}
