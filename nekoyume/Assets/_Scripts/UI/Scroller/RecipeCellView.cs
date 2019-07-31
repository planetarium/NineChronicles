using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EnhancedUI.EnhancedScroller;

namespace Nekoyume.UI.Scroller
{
    [RequireComponent(typeof(RectTransform))]
    public class RecipeCellView : EnhancedScrollerCellView
    {
        public GameObject obj;

        #region Mono

        private void Awake()
        {
            this.ComponentFieldsNotNullTest();
        }

        #endregion

        public void SetData()
        {
            obj.SetActive(true);
        }
    }
}
