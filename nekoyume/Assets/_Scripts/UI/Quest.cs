using Nekoyume.BlockChain;
using Nekoyume.Game.Character;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Quest : Widget
    {
        public VerticalLayoutGroup verticalLayoutGroup;
        public QuestInfo questInfo;

        private Player _player;

        public override void Show()
        {
            var questList = States.Instance.currentAvatarState.Value.questList;
            _player = Game.Game.instance.stage.GetPlayer();
            var parent = verticalLayoutGroup.transform;
            questInfo.gameObject.SetActive(true);
            foreach (var quest in questList)
            {
                var newInfo = Instantiate(questInfo, parent);
                newInfo.Set(quest);
            }
            questInfo.gameObject.SetActive(false);
            base.Show();
        }

        public override void Close()
        {
            for (var i = 0; i < verticalLayoutGroup.transform.childCount; i++)
            {
                var child = verticalLayoutGroup.transform.GetChild(i);
                Destroy(child.gameObject);
            }

            base.Close();
        }
    }
}
