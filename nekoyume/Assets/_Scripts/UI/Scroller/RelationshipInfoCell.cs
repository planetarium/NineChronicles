﻿using System;
using Nekoyume.Helper;
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

        [SerializeField]
        private GameObject focusObject;

        public override void UpdateContent(Model itemData)
        {
            relationshipSectionText.SetText(
                $"{itemData.MinRelationship}-{(itemData.MaxRelationship != 0 ? itemData.MaxRelationship.ToString() : string.Empty)}");
            cpText.SetText($"{TextHelper.FormatNumber(itemData.MinCp)}-{TextHelper.FormatNumber(itemData.MaxCp)}");
            requiredLevelText.SetText(itemData.RequiredLevel.ToString());
            focusObject.SetActive(Context.CurrentModel.MaxRelationship == itemData.MaxRelationship);
        }
    }
}
