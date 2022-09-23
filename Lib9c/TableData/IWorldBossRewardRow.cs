namespace Nekoyume.TableData
{
    public interface IWorldBossRewardRow
    {
        int BossId { get; }
        int Rank { get; }
        int Rune { get; }
        int Crystal { get; }
    }
}
