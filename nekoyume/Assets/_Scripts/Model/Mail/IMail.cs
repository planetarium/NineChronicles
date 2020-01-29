namespace Nekoyume.Model.Mail
{
    public interface IMail
    {
        void Read(CombinationMail mail);
        void Read(SellCancelMail mail);
        void Read(BuyerMail buyerMail);
        void Read(SellerMail sellerMail);
        void Read(ItemEnhanceMail itemEnhanceMail);
    }
}
