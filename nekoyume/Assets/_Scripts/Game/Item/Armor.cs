using System;
using Nekoyume.Helper;
using Nekoyume.TableData;
using UnityEngine;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Armor : Equipment
    {
        public Armor(EquipmentItemSheet.Row data, Guid id) : base(data, id)
        {
        }

        public override Sprite GetIconSprite()
        {
            return SpriteHelper.GetItemIcon(Data.Id);
        }
    }
}
