using System;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class TextButton : IDisposable
    {
        public enum ButtonType
        {
            Cancel,
            Default,
            DefaultPressed,
            Black,
            BlackPressed,
            Violet,
            Yellow
        }

        public readonly ReactiveProperty<ButtonType> buttonType = new ReactiveProperty<ButtonType>();
        public readonly ReactiveProperty<string> text = new ReactiveProperty<string>();

        public readonly Subject<TextButton> onClick = new Subject<TextButton>();

        public void Dispose()
        {
            buttonType.Dispose();
            text.Dispose();

            onClick.Dispose();
        }
    }
}
