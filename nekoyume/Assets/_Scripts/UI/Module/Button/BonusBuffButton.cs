using Nekoyume.Model.State;
using Nekoyume.State;
using System.Collections.Generic;
using System.Linq;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class BonusBuffButton : MonoBehaviour
    {
        [SerializeField]
        private BonusBuffViewDataScriptableObject buffViewData = null;

        [SerializeField]
        private Button button = null;

        [SerializeField]
        private Image iconImage = null;

        [SerializeField]
        private Image bgImage = null;

        [SerializeField]
        private TextMeshProUGUI starCountText = null;

        [SerializeField]
        private GameObject normalObject = null;

        [SerializeField]
        private GameObject availableObject = null;

        [SerializeField]
        private GameObject buffSelectedObject = null;

        private IDisposable _disposableForOnDisabled = null;

        private int _stageId;

        private bool _hasEnoughStars;

        private void Awake()
        {
            button.onClick.AddListener(OnClickButton);
        }

        private void OnDisable()
        {
            _disposableForOnDisabled?.Dispose();
            _disposableForOnDisabled = null;
        }

        public void SetData(CrystalRandomSkillState skillState, int currentStageId)
        {
            _disposableForOnDisabled?.Dispose();
            _disposableForOnDisabled = null;
            _stageId = currentStageId;

            var tableSheets = Game.Game.instance.TableSheets;
            if (States.Instance.CurrentAvatarState.worldInformation.IsStageCleared(currentStageId) ||
                !tableSheets.CrystalStageBuffGachaSheet.TryGetValue(currentStageId, out var row))
            {
                gameObject.SetActive(false);
                return;
            }

            var starCount = skillState != null ? skillState.StarCount : 0;
            _hasEnoughStars = starCount >= row.MaxStar;
            starCountText.text = $"{starCount}/{row.MaxStar}";
            SetIcon(skillState);
            gameObject.SetActive(true);
        }

        private void SetIcon(CrystalRandomSkillState skillState)
        {
            var isBuffAvailable = skillState != null && _hasEnoughStars;
            var selectedId = Widget.Find<BuffBonusResultPopup>().SelectedSkillId;

            var tableSheets = Game.Game.instance.TableSheets;
            if (selectedId.HasValue ||
                (skillState != null && skillState.SkillIds.Any()))
            {
                var buffId = selectedId ?? skillState.SkillIds.Select(buffId =>
                {
                    var randomBuffSheet = tableSheets.CrystalRandomBuffSheet;
                    if (!randomBuffSheet.TryGetValue(buffId, out var bonusBuffRow))
                    {
                        return null;
                    }
                    return bonusBuffRow;
                })
                .OrderBy(x => x.Rank)
                .ThenBy(x => x.Id)
                .First()
                .Id;

                if (!tableSheets.CrystalRandomBuffSheet.TryGetValue(buffId, out var buffRow) ||
                    !tableSheets.SkillSheet.TryGetValue(buffRow.SkillId, out var skillRow))
                {
                    isBuffAvailable = false;
                }
                else
                {
                    var iconSprite = buffViewData.GetBonusBuffIcon(skillRow.SkillCategory);
                    var gradeData = buffViewData.GetBonusBuffGradeData(buffRow.Rank);
                    iconImage.sprite = iconSprite;
                    bgImage.sprite = gradeData.SmallBgSprite;
                }
            }

            normalObject.SetActive(!isBuffAvailable);
            availableObject.SetActive(isBuffAvailable && !selectedId.HasValue);
            buffSelectedObject.SetActive(selectedId.HasValue);
        }

        private void OnClickButton()
        {
            var skillState = States.Instance.CrystalRandomSkillState;

            if (_hasEnoughStars)
            {
                _disposableForOnDisabled?.Dispose(); 
                _disposableForOnDisabled = Widget.Find<BuffBonusResultPopup>().OnBuffSelectedSubject
                    .Subscribe(_ => SetIcon(skillState))
                    .AddTo(gameObject);
            }

            if (skillState is null ||
                !skillState.SkillIds.Any())
            {
                Widget.Find<BuffBonusPopup>().Show(_stageId, _hasEnoughStars);
            }
            else
            {
                Widget.Find<BuffBonusResultPopup>().Show(_stageId, skillState);
            }
        }
    }
}
