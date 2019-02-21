using System.Collections;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Game.Trigger;
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
            btnQuest.GetComponent<Button>().enabled = true;
            var avatar = ActionManager.Instance.Avatar;
            var enabled = !avatar.Dead;
            btnQuest.SetActive(enabled);
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
            StartCoroutine(QuestAsync());
        }

        private IEnumerator QuestAsync()
        {
            btnQuest.SetActive(false);
            btnCombine.SetActive(false);
            var currentAvatar = ActionManager.Instance.Avatar;
            ActionManager.Instance.HackAndSlash();
            while (currentAvatar.Equals(ActionManager.Instance.Avatar))
            {
                yield return new WaitForSeconds(1.0f);
            }

            Game.Event.OnStageStart.Invoke();
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
