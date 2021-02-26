using Nekoyume.UI.Model;

namespace Nekoyume.UI.Module
{
    public class SimpleCountableItemView : CountableItemView<CountableItem>
    {
        protected override ImageSizeType imageSizeType => ImageSizeType.Middle;
    }
}
