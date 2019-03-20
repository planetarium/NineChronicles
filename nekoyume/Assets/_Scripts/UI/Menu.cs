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
        public GameObject btnTemple;
        public Text LabelInfo;
        
        public void ShowButtons(bool value)
        {
            btnQuest.SetActive(value);
            btnCombine.SetActive(value);
            btnShop.SetActive(value);
            btnTemple.SetActive(value);
        }

        public void ShowRoom()
        {
            Show();
            ShowButtons(true);

            LabelInfo.text = "";
        }

        public void ShowWorld()
        {
            Show();
            ShowButtons(false);

            LabelInfo.text = "";
        }

        public void QuestClick()
        {
            Find<QuestPreparation>()?.Show();
            Find<Status>()?.Close();
            Close();
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
            Find<Shop>().Show();
            Find<Status>()?.Close();
            Close();
        }

        public void CombinationClick()
        {
            Find<Combination>()?.Show();
            Find<Status>()?.Close();
            Close();
        }
    }
}
