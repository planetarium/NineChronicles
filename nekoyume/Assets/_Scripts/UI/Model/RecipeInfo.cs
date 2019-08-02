using Nekoyume.BlockChain;
using Nekoyume.Data;
using Nekoyume.Game.Item;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class RecipeInfo
    {
        public class MaterialInfo
        {
            public int id;
            public Sprite sprite;
            public bool isEnough;
            public bool isObtained;

            public MaterialInfo(int id, Sprite sprite)
            {
                this.id = id;
                this.sprite = sprite;
                var inventory = States.Instance.currentAvatarState.Value.inventory;
                isEnough = inventory.HasItem(id);
                isObtained = true;
            }
        }

        public int recipeId;
        public string resultName;
        public Sprite resultSprite;
        public MaterialInfo[] materialInfos = new MaterialInfo[5];

        public RecipeInfo(int id, params int[] materialIds)
        {
            recipeId = id;
            resultName = GetEquipmentName(id);
            resultSprite = ItemBase.GetSprite(id);

            for (int i = 0; i < materialInfos.Length; ++i)
            {
                var sprite = ItemBase.GetSprite(materialIds[i]);
                materialInfos[i] = new MaterialInfo(materialIds[i], sprite);
            }
        }

        private string GetEquipmentName(int id)
        {
            if (id == 0) return string.Empty;
            var equips = Tables.instance.ItemEquipment;
            if (equips.ContainsKey(id))
            {
                return equips[id].name;
            }
            else
            {
                Debug.LogError("Item not found!");
                return string.Empty;
            }
        }
    }
}
