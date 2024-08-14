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
            return;
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
            base.Show(ignoreShowAnimation);
        }
    }
}
