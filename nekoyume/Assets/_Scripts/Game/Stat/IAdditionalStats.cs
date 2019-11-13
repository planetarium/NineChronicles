namespace Nekoyume.Game
{
    public interface IAdditionalStats
    {
        int AdditionalHP { get; }
        int AdditionalATK { get; }
        int AdditionalDEF { get; }
        int AdditionalCRI { get; }
        int AdditionalDOG { get; }
        int AdditionalSPD { get; }
        
        bool HasAdditionalHP { get; }
        bool HasAdditionalATK { get; }
        bool HasAdditionalDEF { get; }
        bool HasAdditionalCRI { get; }
        bool HasAdditionalDOG { get; }
        bool HasAdditionalSPD { get; }
        bool HasAdditionalStats { get; }
    }
}
