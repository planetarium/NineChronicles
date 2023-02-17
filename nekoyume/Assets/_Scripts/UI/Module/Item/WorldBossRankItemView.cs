using System;
using System.Collections.Generic;
using Nekoyume.Game.Character;
using Nekoyume.Helper;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Nekoyume.UI.Module.WorldBoss;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;

    public class WorldBossRankItemView : MonoBehaviour
    {
        [SerializeField]
        private DetailedCharacterView characterView;

        [SerializeField]
        private Image rankImage;

        [SerializeField]
        private TextMeshProUGUI rankText;

        [SerializeField]
        private TextMeshProUGUI avatarName;

        [SerializeField]
        private TextMeshProUGUI address;

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

        [SerializeField]
        public TouchHandler touchHandler;

        private GameObject _gradeObject;
        private GameObject _rankObject;
        private readonly List<IDisposable> _disposables = new();

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

            if (_rankObject != null)
            {
                Destroy(_rankObject);
            }

            var grade = (WorldBossGrade)WorldBossHelper.CalculateRank(model.BossRow, model.HighScore);
            if (WorldBossFrontHelper.TryGetGrade(grade, true, out var prefab))
            {
                _gradeObject = Instantiate(prefab, gradeContainer);
            }

            characterView.SetByFullCostumeOrArmorId(model.Portrait, model.Level);
            avatarName.text = model.AvatarName;
            address.text = $"#{model.Address[..4]}";
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
                var rankPrefab = WorldBossFrontHelper.GetRankPrefab(model.Ranking);
                _rankObject = Instantiate(rankPrefab, rankImageContainer.transform);
            }

            if (touchHandler != null)
            {
                _disposables.DisposeAllAndClear();
                touchHandler.OnClick.Select(_ => model)
                    .Subscribe(context.OnClick.OnNext).AddTo(_disposables);
            }
        }
    }
}
