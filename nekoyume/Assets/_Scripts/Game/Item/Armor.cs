using System;
using Nekoyume.Helper;
using UnityEngine;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Armor : Equipment
    {
        public Armor(Data.Table.Item data, Guid id)
            : base(data, id)
        {
        }

        public override Sprite GetIconSprite()
        {
            return SpriteHelper.GetItemIcon(Data.resourceId);
        }
    }
}
