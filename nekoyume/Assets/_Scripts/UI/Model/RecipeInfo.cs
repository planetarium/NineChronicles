using Nekoyume.BlockChain;
using Nekoyume.Data;
using Nekoyume.Game.Item;
using System.Linq;
using Nekoyume.TableData;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class RecipeInfo
    {
        public class MaterialInfo
        {
            public int id;
            public int amount = 1;
            public bool isEnough;
            public bool isObtained;

            public MaterialInfo(int id)
            {
                this.id = id;
                var inventory = States.Instance.CurrentAvatarState.Value.inventory;
                isEnough = inventory.HasItem(id);
                isObtained = true;
            }
        }

        public int recipeId;
        public int resultId;
        public int resultAmount = 1;
        public string resultName;
        public MaterialInfo[] materialInfos = new MaterialInfo[5];

        public RecipeInfo(int id, int resultId, params int[] materialIds)
        {
            recipeId = id;
            this.resultId = resultId;
            resultName = GetEquipmentName(resultId);

            for (int i = 0; i < materialInfos.Length; ++i)
            {
                materialInfos[i] = new MaterialInfo(materialIds[i]);
            }
        }

        private string GetEquipmentName(int id)
        {
            if (id == 0)
                return string.Empty;

            return Game.Game.instance.TableSheets.ItemSheet.TryGetValue(id, out var itemRow)
                ? itemRow.GetLocalizedName()
                : string.Empty;
        }
    }
}
