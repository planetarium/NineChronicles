using System.Collections;
using Nekoyume.Game.Character;
using Nekoyume.Move;
using UnityEngine;


namespace Nekoyume.Game.Trigger
{
    public class StageExit : MonoBehaviour
    {
        public bool Sleep;

        private void Awake()
        {
            Event.OnStageClear.AddListener(OnStageClear);
        }

        private void OnStageClear()
        {
            StartCoroutine(WaitStageExit());
        }

        private IEnumerator WaitStageExit()
        {
            var cam = Camera.main.gameObject.GetComponent<ActionCamera>();
            cam.target = null;

            StageReward();

            var stage = GetComponentInParent<Stage>();
            var player = stage.GetComponentInChildren<Player>();
            var id = stage.Id + 1;
            MoveManager.Instance.HackAndSlash(player, id);

            yield return new WaitForSeconds(1.0f);
            if (Sleep)
                Event.OnPlayerSleep.Invoke();
            else
                Event.OnStageEnter.Invoke();
            Sleep = false;
        }

        private void StageReward()
        {
            var stage = GetComponentInParent<Stage>();
            var selector = new Util.WeightedSelector<int>();
            var tables = this.GetRootComponent<Data.Tables>();
            foreach (var pair in tables.BoxDrop)
            {
                Data.Table.BoxDrop dropData = pair.Value;
                if (stage.Id != dropData.StageId)
                    continue;

                if (dropData.Weight <= 0)
                    continue;

                selector.Add(dropData.BoxId, dropData.Weight);
            }

            if (selector.Count <= 0)
                return;

            var player = stage.GetComponentInChildren<Character.Player>();
            var dropItemFactory = GetComponentInParent<Factory.DropItemFactory>();
            var dropItem = dropItemFactory.Create(selector.Select(), player.transform.position);
            if (dropItem != null)
            {
                Event.OnGetItem.Invoke(dropItem.GetComponent<Item.DropItem>());
            }
        }
    }
}
