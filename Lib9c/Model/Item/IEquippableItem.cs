namespace Nekoyume.Model.Item
{
    public interface IEquippableItem
    {
        bool Equipped { get; }
        void Equip();
        void Unequip();
    }
}
