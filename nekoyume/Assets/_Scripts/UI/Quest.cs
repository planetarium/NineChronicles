using Nekoyume.BlockChain;
using Nekoyume.Game.Character;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Quest : Widget
    {
        public ScrollRect list;
        public QuestInfo questInfo;

        public override void Show()
        {
            var questList = States.Instance.currentAvatarState.Value.questList;
            questInfo.gameObject.SetActive(true);
            foreach (var quest in questList)
            {
                var newInfo = Instantiate(questInfo, list.content);
                newInfo.Set(quest);
            }
            questInfo.gameObject.SetActive(false);
            base.Show();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            foreach (Transform child in list.content.transform)
            {
                Destroy(child.gameObject);
            }

            list.verticalNormalizedPosition = 1f;

            base.Close(ignoreCloseAnimation);
        }
    }
}
