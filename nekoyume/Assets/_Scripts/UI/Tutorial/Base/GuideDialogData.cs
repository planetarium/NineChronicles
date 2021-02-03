using System.Diagnostics.Tracing;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class GuideDialogData : ITutorialData
    {
        public TutorialItemType Type { get; } = TutorialItemType.Dialog;
        public DialogEmojiType EmojiType { get; }
        public DialogCommaType CommaType { get; }
        public string ScriptL10nKey { get; }
        public float TargetHeight { get; }
        public Button Button { get; }

        public GuideDialogData(DialogEmojiType emojiType,
            DialogCommaType commaType,
            string scriptL10nKey,
            float targetHeight,
            Button button)
        {
            EmojiType = emojiType;
            CommaType = commaType;
            ScriptL10nKey = scriptL10nKey;
            TargetHeight = targetHeight;
            Button = button;
        }
    }
}
