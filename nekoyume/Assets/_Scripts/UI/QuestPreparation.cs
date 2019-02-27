using System.Collections;
using Nekoyume.Action;
using UnityEngine;

namespace Nekoyume.UI
{
    public class QuestPreparation : Widget
    {
        public void QuestClick()
        {
            StartCoroutine(QuestAsync());
        }

        private IEnumerator QuestAsync()
        {
            Close();
            var currentAvatar = ActionManager.Instance.Avatar;
            ActionManager.Instance.HackAndSlash();
            while (currentAvatar.Equals(ActionManager.Instance.Avatar))
            {
                yield return new WaitForSeconds(1.0f);
            }

            Game.Event.OnStageStart.Invoke();
        }

        public override void Show()
        {
            GetComponentInChildren<Inventory>().Show();
            base.Show();
        }
    }
}
