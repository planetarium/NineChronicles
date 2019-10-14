namespace Nekoyume.Game
{
    public interface IStats
    {
        int HP { get; }
        int ATK { get; }
        int DEF { get; }
        int CRI { get; }
        int DOG { get; }
        int SPD { get; }
        
        bool HasHP { get; }
        bool HasATK { get; }
        bool HasDEF { get; }
        bool HasCRI { get; }
        bool HasDOG { get; }
        bool HasSPD { get; }
    }
}
