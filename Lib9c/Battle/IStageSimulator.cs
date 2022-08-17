using Nekoyume.Model;
using Nekoyume.TableData;

namespace Nekoyume.Battle
{
    public interface IStageSimulator : ISimulator
    {
        int StageId { get; }
        EnemySkillSheet EnemySkillSheet { get; }
        CollectionMap ItemMap { get; }
    }
}
