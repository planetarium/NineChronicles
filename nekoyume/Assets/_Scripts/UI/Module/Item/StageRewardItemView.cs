using Nekoyume.Game.Character;
using Nekoyume.TableData;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class StageRewardItemView: VanillaItemView
    {
        public RectTransform RectTransform { get; private set; }
        public ItemSheet.Row Data { get; private set; }

        public TouchHandler touchHandler;

        protected void Awake()
        {
            RectTransform = GetComponent<RectTransform>();
        }

        protected void OnDestroy()
        {
            Clear();
        }

        public override void SetData(ItemSheet.Row itemRow)
        {
            base.SetData(itemRow);
            Data = itemRow;
        }
    }
}
