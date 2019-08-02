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
            public ReactiveProperty<int> id = new ReactiveProperty<int>(0);
            public Sprite sprite;
            public bool isEnough;
            public bool isObtained;
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
                materialInfos[i] = new MaterialInfo();
                materialInfos[i].id.Subscribe(mid => SetSprite(materialInfos[i], mid));
                SetMaterialInfo(materialInfos[i], materialIds[i]);
            }
        }

        public void SetSprite(MaterialInfo info, int id)
        {
            info.sprite = ItemBase.GetSprite(id);
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

        private void SetMaterialInfo(MaterialInfo info, int id)
        {
            info.id.Value = id;
            var inventory = States.Instance.currentAvatarState.Value.inventory;
            info.isEnough = inventory.HasItem(id);
            info.isObtained = true;
        }
    }
}
