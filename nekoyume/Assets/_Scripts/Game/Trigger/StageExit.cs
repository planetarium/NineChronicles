using System.Collections;
using Nekoyume.Action;
using Nekoyume.Data;
using Nekoyume.Game.Character;
using Nekoyume.Game.Factory;
using Nekoyume.Game.Item;
using Nekoyume.Game.Util;
using UnityEngine;

namespace Nekoyume.Game.Trigger
{
    public class StageExit : MonoBehaviour
    {
        private void StageReward()
        {
            var stage = GetComponentInParent<Stage>();
            var selector = new WeightedSelector<int>();
            var tables = this.GetRootComponent<Tables>();
            foreach (var pair in tables.BoxDrop)
            {
                var dropData = pair.Value;
                if (stage.Id != dropData.StageId)
                    continue;

                if (dropData.Weight <= 0)
                    continue;

                selector.Add(dropData.BoxId, dropData.Weight);
            }

            if (selector.Count <= 0)
                return;

            var player = stage.GetComponentInChildren<Player>();
            var dropItemFactory = GetComponentInParent<DropItemFactory>();
            var dropItem = dropItemFactory.Create(selector.Select(), player.transform.position);
            if (dropItem != null) Event.OnGetItem.Invoke(dropItem.GetComponent<DropItem>());
        }
    }
}
