using Nekoyume.Helper;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module.WorldBoss;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class WorldBossRankItemView : MonoBehaviour
    {
        [SerializeField]
        private Image portrait;

        [SerializeField]
        private TextMeshProUGUI avatarName;

        [SerializeField]
        private TextMeshProUGUI address;

        [SerializeField]
        private TextMeshProUGUI level;

        [SerializeField]
        private TextMeshProUGUI rank;

        [SerializeField]
        private TextMeshProUGUI cp;

        [SerializeField]
        private TextMeshProUGUI highScore;

        [SerializeField]
        private TextMeshProUGUI totalScore;

        [SerializeField]
        private Transform gradeContainer;

        private GameObject _gradeObject;

        public void Set(WorldBossRankItem model, WorldBossRankScroll.ContextModel context)
        {
            if (model is null)
            {
                return;
            }

            if (_gradeObject != null)
            {
                Destroy(_gradeObject);
            }

            var grade = (WorldBossGrade)WorldBossHelper.CalculateRank(model.HighScore);
            if (WorldBossFrontHelper.TryGetGrade(grade, true, out var prefab))
            {
                _gradeObject = Instantiate(prefab, gradeContainer);
            }

            portrait.sprite = SpriteHelper.GetItemIcon(model.Portrait);
            avatarName.text = model.AvatarName;
            level.text = $"{model.Level}";
            rank.text = model.Ranking > 0 ? $"{model.Ranking}" : "-";
            cp.text = $"{model.Cp:#,0}";
            highScore.text = model.HighScore > 0 ? $"{model.HighScore:#,0}" : "-";
            totalScore.text = model.TotalScore > 0 ? $"{model.TotalScore:#,0}" : "-";
        }
    }
}
