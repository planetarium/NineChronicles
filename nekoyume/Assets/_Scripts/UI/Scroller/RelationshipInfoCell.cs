using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public class RelationshipInfoCell : RectCell<RelationshipInfoCell.Model, RelationshipInfoScroll.ContextModel>
    {
        public class Model
        {
            public int MinRelationship;
            public int MaxRelationship;
            public int MinCp;
            public int MaxCp;
            public int RequiredLevel;
        }

        [SerializeField]
        private TextMeshProUGUI relationshipSectionText;

        [SerializeField]
        private TextMeshProUGUI cpText;

        [SerializeField]
        private TextMeshProUGUI requiredLevelText;

        public override void UpdateContent(Model itemData)
        {
            relationshipSectionText.SetText($"{itemData.MinRelationship}~{itemData.MaxRelationship}");
            cpText.SetText($"{itemData.MinCp}~{itemData.MaxCp}");
            requiredLevelText.SetText(itemData.RequiredLevel.ToString());
        }
    }
}
