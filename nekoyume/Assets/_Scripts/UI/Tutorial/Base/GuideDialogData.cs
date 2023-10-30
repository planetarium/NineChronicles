using System;
using UnityEngine;

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

        public DialogPositionType positionType;

        public GuideDialogData(
            DialogEmojiType emojiType,
            DialogCommaType commaType,
            DialogPositionType positionType,
            string script,
            RectTransform target)
        {
            this.emojiType = emojiType;
            this.commaType = commaType;
            this.positionType = positionType;
            this.script = script;
            this.target = target;
        }
    }
}
