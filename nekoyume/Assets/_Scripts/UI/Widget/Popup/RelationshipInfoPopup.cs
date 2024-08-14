using System.Collections.Generic;
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
            var prevRelationship = 0;
            foreach (var row in TableSheets.Instance.CustomEquipmentCraftRelationshipSheet.Values)
            {
                var model = new RelationshipInfoCell.Model
                {
                    MinRelationship = prevRelationship,
                    MaxRelationship = row.Relationship,
                    MinCp = row.MinCp,
                    MaxCp = row.MaxCp,
                    RequiredLevel = TableSheets.Instance.ItemRequirementSheet[row.WeaponItemId]
                        .Level
                };
                scrollModels.Add(model);
                prevRelationship = row.Relationship + 1;

                if (currentModel == null && row.Relationship >= ReactiveAvatarState.Relationship)
                {
                    currentModel = model;
                }
            }

            scroll.CurrentModel = currentModel;
            scroll.UpdateData(scrollModels);
            base.Show(ignoreShowAnimation);
        }
    }
}
