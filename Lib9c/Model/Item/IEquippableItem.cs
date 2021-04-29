namespace Nekoyume.Model.Item
{
    public interface IEquippableItem: IItem
    {
        bool Equipped { get; }
        void Equip();
        void Unequip();
    }
}
