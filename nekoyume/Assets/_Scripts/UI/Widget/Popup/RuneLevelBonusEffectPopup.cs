using System.Linq;
using Nekoyume.Game.Controller;
using Nekoyume.UI.Scroller;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class RuneLevelBonusEffectPopup : PopupWidget
    {
        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private RuneLevelBonusEffectScroll scroll;

        protected override void Awake()
        {
            base.Awake();

            closeButton.onClick.AddListener(() =>
            {
                CloseWidget.Invoke();
                AudioController.PlayClick();
            });
            CloseWidget = () => Close();
        }

        public void Show(decimal runeLevelBonus)
        {
            var orderedSheet = Game.Game.instance.TableSheets.RuneLevelBonusSheet.OrderedList;
            var models = orderedSheet.Select(row =>
            {
                var currentLevelBonus = row.RuneLevel;
                var nextLevelBonus = orderedSheet
                    .FirstOrDefault(nextRow => nextRow.Id == row.Id + 1)?
                    .RuneLevel - 1;

                return new RuneLevelBonusEffectCell.Model
                {
                    LevelBonusMin = currentLevelBonus,
                    LevelBonusMax = nextLevelBonus,
                    RewardMin = currentLevelBonus * row.Bonus,
                    RewardMax = nextLevelBonus * row.Bonus
                };
            }).ToList();

            var currentModel = models.FirstOrDefault(model =>
                runeLevelBonus >= model.LevelBonusMin &&
                runeLevelBonus < model.LevelBonusMax);

            scroll.CurrentModel = currentModel;
            scroll.UpdateData(models, true);
            scroll.JumpTo(currentModel);

            base.Show();
        }
    }
}
