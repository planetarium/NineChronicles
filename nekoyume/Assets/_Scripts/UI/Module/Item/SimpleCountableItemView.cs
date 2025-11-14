using Nekoyume.UI.Model;

namespace Nekoyume.UI.Module
{
    public class SimpleCountableItemView : CountableItemView<CountableItem>
    {
        protected override ImageSizeType imageSizeType => ImageSizeType.Middle;

        protected override void SetCount(int count)
        {
            if (ignoreOne && count == 1)
            {
                countText.text = string.Empty;
                return;
            }

            // FungibleAssetValue인 경우 단축 표기 사용
            if (Model?.ItemBase.Value is null)
            {
                countText.text = Model.FungibleAssetValue.Value.MajorUnit.ToCurrencyNotation();
            }
            else
            {
                // 일반 ItemBase인 경우 기존대로 정수 표시
                base.SetCount(count);
            }
        }
    }
}
