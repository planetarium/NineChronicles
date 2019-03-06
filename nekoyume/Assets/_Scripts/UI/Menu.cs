using System.Collections;
using Nekoyume.Action;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Menu : Widget
    {
        public GameObject btnQuest;
        public GameObject btnCombine;
        public GameObject btnShop;
        public Text LabelInfo;

        public void ShowRoom()
        {
            Show();
            btnQuest.SetActive(true);
            LabelInfo.text = "";
        }

        public void ShowWorld()
        {
            Show();
            btnQuest.SetActive(false);
            btnCombine.SetActive(false);

            LabelInfo.text = "";
        }

        public void QuestClick()
        {
            Find<QuestPreparation>().Toggle();
        }

        public void CombineClick()
        {
            StartCoroutine(CombineAsync());
        }

        private IEnumerator CombineAsync()
        {
            btnCombine.SetActive(false);
            var items = ActionManager.Instance.Avatar.Items;
            ActionManager.Instance.Combination();
            while (items != ActionManager.Instance.Avatar.Items)
            {
                yield return new WaitForSeconds(1.0f);
            }

            btnCombine.SetActive(true);
        }

        public void ShopClick()
        {
            Find<Shop>().Toggle();
        }
    }
}
