using UniRx;

namespace Nekoyume.UI.Model
{
    public class ImageButton
    {
        public enum ButtonType
        {
            BlackClose,
            BluePlus,
            BlueMinus,
            GrayPlus,
            GrayMinus
        }

        public readonly ReactiveProperty<ButtonType> buttonType = new ReactiveProperty<ButtonType>();
        public readonly ReactiveProperty<string> text = new ReactiveProperty<string>();

        public readonly Subject<ImageButton> onClick = new Subject<ImageButton>();

        public void Dispose()
        {
            buttonType.Dispose();
            text.Dispose();

            onClick.Dispose();
        }
    }
}
