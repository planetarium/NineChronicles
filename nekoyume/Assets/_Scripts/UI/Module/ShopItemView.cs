using Nekoyume.UI.Model;

namespace Nekoyume.UI.Module
{
    public class ShopItemView : CountableItemView<ShopItem>
    {
        public override void SetData(ShopItem model)
        {
            if (model is null)
            {
                Clear();
                return;
            }
            
            base.SetData(model);

            Model.View = this;
        }
    }
}
