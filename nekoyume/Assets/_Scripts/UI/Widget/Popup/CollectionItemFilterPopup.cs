namespace Nekoyume.UI
{
    public class CollectionItemFilterPopup : ItemFilterPopupBase
    {
        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);

            ResetViewFromFilterOption();
            var collectionWidget = Find<Collection>();
            collectionWidget.SetItemFilterOption(GetItemFilterOptionType());
        }
    }
}
