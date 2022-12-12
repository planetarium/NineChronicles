namespace Nekoyume.Model.Mail
{
    public interface IMail
    {
        void Read(CombinationMail mail);
        void Read(SellCancelMail mail);
        void Read(BuyerMail buyerMail);
        void Read(SellerMail sellerMail);
        void Read(ItemEnhanceMail itemEnhanceMail);
        void Read(DailyRewardMail dailyRewardMail);
        void Read(MonsterCollectionMail monsterCollectionMail);
        void Read(OrderExpirationMail orderExpirationMail);
        void Read(CancelOrderMail cancelOrderMail);
        void Read(OrderBuyerMail orderBuyerMail);
        void Read(OrderSellerMail orderSellerMail);
        void Read(GrindingMail grindingMail);
        void Read(MaterialCraftMail materialCraftMail);
    }
}
