using Nekoyume.TableData;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module.WorldBoss
{
    public class WorldBossBattleRewardItem : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI rune;

        [SerializeField]
        private TextMeshProUGUI crystal;

        [SerializeField]
        private GameObject selected;

        public void Set(WorldBossKillRewardSheet.Row row, bool isSelected)
        {
            crystal.text = $"{row.Crystal:#,0}";
            rune.text = $"{row.RuneMin:#,0}~{row.RuneMax:#,0}";
            selected.SetActive(isSelected);
        }
    }
}
