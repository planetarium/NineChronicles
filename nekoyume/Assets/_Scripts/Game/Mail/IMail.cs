namespace Nekoyume.Game.Mail
{
    public interface IMail
    {
        void Read(CombinationMail mail);
        void Read(SellCancelMail mail);
    }
}
