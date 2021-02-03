using System.Diagnostics.Tracing;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class GuideDialogData : ITutorialData
    {
        public TutorialItemType Type { get; } = TutorialItemType.Dialog;
        public DialogEmojiType EmojiType { get; }
        public DialogCommaType CommaType { get; }
        public string Script { get; }
        public RectTransform Target { get; }
        public Button Button { get; }

        public GuideDialogData(DialogEmojiType emojiType,
            DialogCommaType commaType,
            string script,
            RectTransform target,
            Button button)
        {
            EmojiType = emojiType;
            CommaType = commaType;
            Script = script;
            Target = target;
            Button = button;
        }
    }
}
