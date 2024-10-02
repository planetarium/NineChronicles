using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.State;
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

        public override void Show(bool ignoreShowAnimation = false)
        {
            RelationshipInfoCell.Model currentModel = null;
            var scrollModels = new List<RelationshipInfoCell.Model>();
            var nextRelationship = 0;
            foreach (var row in TableSheets.Instance.CustomEquipmentCraftRelationshipSheet.OrderedList!.Reverse())
            {
                var model = new RelationshipInfoCell.Model
                {
                    MinRelationship = row.Relationship,
                    MaxRelationship = nextRelationship,
                    MinCp = row.CpGroups.Min(cp => cp.MinCp),
                    MaxCp = row.CpGroups.Max(cp => cp.MaxCp),
                    RequiredLevel = TableSheets.Instance.ItemRequirementSheet[row.WeaponItemId]
                        .Level
                };
                scrollModels.Add(model);
                nextRelationship = row.Relationship - 1;

                if (currentModel == null && row.Relationship <= ReactiveAvatarState.Relationship)
                {
                    currentModel = model;
                }
            }

            scroll.CurrentModel = currentModel;
            scrollModels.Reverse();
            scroll.UpdateData(scrollModels);
            base.Show(ignoreShowAnimation);
        }
    }
}
