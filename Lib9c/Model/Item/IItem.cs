namespace Nekoyume.Model.Item
{
    public interface IItem
    {
        ItemType ItemType { get; }
        
        ItemSubType ItemSubType { get; }
    }
}
