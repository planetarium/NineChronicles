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
        private Image rankImage;

        [SerializeField]
        private TextMeshProUGUI rankText;

        [SerializeField]
        private TextMeshProUGUI avatarName;

        [SerializeField]
        private TextMeshProUGUI address;

        [SerializeField]
        private TextMeshProUGUI level;

        [SerializeField]
        private TextMeshProUGUI cp;

        [SerializeField]
        private TextMeshProUGUI highScore;

        [SerializeField]
        private TextMeshProUGUI totalScore;

        [SerializeField]
        private Transform gradeContainer;

        [SerializeField]
        private GameObject rankImageContainer;

        [SerializeField]
        private GameObject rankTextContainer;

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

            var nameWithHash = model.AvatarName
                .Replace(" <size=80%><color=#A68F7E>", string.Empty)
                .Replace("</color></size>", string.Empty);
            avatarName.text = nameWithHash.Substring(0, nameWithHash.Length - 5);
            address.text = nameWithHash.Substring(nameWithHash.Length - 5, 5);
            level.text = $"{model.Level}";
            cp.text = $"{model.Cp:#,0}";
            highScore.text = $"{model.HighScore:#,0}";
            totalScore.text = $"{model.TotalScore:#,0}";

            rankImageContainer.SetActive(false);
            rankTextContainer.SetActive(false);
            if (model.Ranking > 3)
            {
                rankTextContainer.SetActive(true);
                rankText.text = $"{model.Ranking}";
            }
            else
            {
                rankImageContainer.SetActive(true);
                rankImage.sprite = WorldBossFrontHelper.GetRankIcon(model.Ranking);
            }
        }
    }
}
