using System.Collections.Generic;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class RuneListView : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI titleText;

        [SerializeField]
        private List<RuneListItemView> items;

        public void Set(RuneListItem model, RuneListScroll.ContextModel context)
        {
            titleText.text = model.RuneTitle;
            foreach (var item in items)
            {
                item.gameObject.SetActive(false);
            }

            for (var i = 0; i < model.Runes.Count; i++)
            {
                items[i].gameObject.SetActive(true);
                items[i].Set(model.Runes[i], context);
            }
        }
    }
}
