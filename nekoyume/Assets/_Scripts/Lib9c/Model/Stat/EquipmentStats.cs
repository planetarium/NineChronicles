using Nekoyume.TableData;

namespace Nekoyume.Model.Stat
{
    /// <summary>
    /// 장비의 스탯을 관리한다.
    /// 스탯은 강화에 의한 _enhancementStats를 기본으로 하고
    /// > 조합으로 인한 _combinationStats 
    /// 마지막으로 모든 스탯을 합한 EquipmentStats 순서로 계산한다.
    /// </summary>
    public class EquipmentStats : Stats
    {
        private readonly EquipmentItemSheet.Row _row;

        private readonly Stats _enhancementStats = new Stats();
        private readonly Stats _combinationStats = new Stats();

        // ... 고민.
    }
}
