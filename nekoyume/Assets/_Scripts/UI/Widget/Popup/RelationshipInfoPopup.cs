using System.Collections.Generic;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.UI.Scroller;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class RelationshipInfoPopup : PopupWidget
    {
        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private RelationshipInfoScroll scroll;

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

        public override void Initialize()
        {
            var scrollModels = new List<RelationshipInfoCell.Model>();
            var prevRelationship = 0;
            foreach (var row in TableSheets.Instance.CustomEquipmentCraftRelationshipSheet.Values)
            {
                scrollModels.Add(new RelationshipInfoCell.Model()
                {
                    MinRelationship = prevRelationship,
                    MaxRelationship = row.Relationship,
                    MinCp = row.MinCp,
                    MaxCp = row.MaxCp,
                    RequiredLevel = TableSheets.Instance.ItemRequirementSheet[row.WeaponItemId].Level
                });
                prevRelationship = row.Relationship + 1;
            }

            scroll.UpdateData(scrollModels);
            base.Initialize();
        }

        public void Show(bool ignoreShowAnimation = false)
        {
            // var orderedSheet = Game.Game.instance.TableSheets.RuneLevelBonusSheet.OrderedList;
            // var models = orderedSheet.Select(row =>
            // {
            //     var currentLevelBonus = row.RuneLevel;
            //     var nextLevelBonus = orderedSheet
            //         .FirstOrDefault(nextRow => nextRow.Id == row.Id + 1)?
            //         .RuneLevel - 1;
            //
            //     var runeLevelBonusSheet = Game.Game.instance.TableSheets.RuneLevelBonusSheet;
            //     var rewardMin = RuneFrontHelper.CalculateRuneLevelBonusReward(
            //         currentLevelBonus, runeLevelBonusSheet);
            //     int? rewardMax = nextLevelBonus.HasValue
            //         ? RuneFrontHelper.CalculateRuneLevelBonusReward(nextLevelBonus.Value, runeLevelBonusSheet)
            //         : null;
            //
            //     return new RuneLevelBonusEffectCell.Model
            //     {
            //         LevelBonusMin = currentLevelBonus,
            //         LevelBonusMax = nextLevelBonus,
            //         RewardMin = rewardMin,
            //         RewardMax = rewardMax
            //     };
            // }).ToList();
            //
            // var currentModel = models.FirstOrDefault(model =>
            //     runeLevelBonus >= model.LevelBonusMin &&
            //     runeLevelBonus < model.LevelBonusMax);
            //
            // scroll.CurrentModel = currentModel;
            // scroll.UpdateData(models, true);
            // scroll.JumpTo(currentModel);

            base.Show(ignoreShowAnimation);
        }
    }
}
