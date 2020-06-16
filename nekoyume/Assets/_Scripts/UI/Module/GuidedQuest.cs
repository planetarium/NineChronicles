using System.Collections.Generic;
using Nekoyume.UI.Scroller;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class GuidedQuest : MonoBehaviour
    {
        [SerializeField]
        private List<GuidedQuestCell> cells = null;

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
