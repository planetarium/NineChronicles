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

        private Player _player;

        public override void Show()
        {
            var questList = States.Instance.currentAvatarState.Value.questList;
            _player = Game.Game.instance.stage.GetPlayer();
            questInfo.gameObject.SetActive(true);
            foreach (var quest in questList)
            {
                var newInfo = Instantiate(questInfo, list.content);
                newInfo.Set(quest);
            }
            questInfo.gameObject.SetActive(false);
            base.Show();
        }

        public override void Close()
        {
            foreach (Transform child in  list.content.transform)
            {
                Destroy(child.gameObject);
            }

            base.Close();
        }
    }
}
