using Nekoyume.Game.Character;
using Nekoyume.TableData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UniRx;
using Nekoyume.Game.Controller;

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
