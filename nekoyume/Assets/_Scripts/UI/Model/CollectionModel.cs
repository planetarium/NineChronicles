using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;

namespace Nekoyume.UI.Model
{
    public class CollectionModel
    {
        public CollectionSheet.Row Row { get; }
        public ItemType ItemType { get; }

        public bool Active { get; set; }
        public List<CollectionMaterial> Materials { get; }

        public bool CanActivate => Materials.All(material => material.Enough);

        public CollectionModel(
            CollectionSheet.Row row,
            ItemType itemType)
        {
            Row = row;
            ItemType = itemType;

            Active = false;
            Materials = new List<CollectionMaterial>();
        }

        public static List<StatModifier> GetEffect()
        {
            var collectionSheet = Game.Game.instance.TableSheets.CollectionSheet;
            var collectionState = Game.Game.instance.States.CollectionState;
            var data = collectionSheet.Values
                .Where(row => collectionState.Ids.Contains(row.Id))
                .SelectMany(row => row.StatModifiers);

            return data.ToList();
        }
    }

    public static class CollectionModelExtension
    {
        // Get Models
        public static void SetModels(this List<CollectionModel> models)
        {
            if (!models.Any())
            {
                var collectionSheet = Game.Game.instance.TableSheets.CollectionSheet;
                var itemSheet = Game.Game.instance.TableSheets.ItemSheet;

                foreach (var row in collectionSheet.Values)
                {
                    var model = new CollectionModel(
                        row,
                        itemSheet[row.Materials.First().ItemId].ItemType);

                    models.Add(model);
                }
            }

            var activateCollectionIds = Game.Game.instance.States.CollectionState.Ids;
            foreach (var model in models.Where(model => !model.Active))
            {
                model.Active = activateCollectionIds.Contains(model.Row.Id);
            }
        }

        // models를 생성
        // models의 active를 갱신

        // materials를 생성
        // materials의 condition를 갱신
        // => material이 정상적으로 생성되었는지 확인 -> (active는 건드릴것 없고) materials의 condition을 갱신

        // Update Materials
        public static void UpdateMaterials(this List<CollectionModel> models)
        {
            var itemSheet = Game.Game.instance.TableSheets.ItemSheet;
            var inventory = Game.Game.instance.States.CurrentAvatarState.inventory;

            foreach (var model in models)
            {
                // materials를 생성
                if (!model.Materials.Any())
                {
                    foreach (var requiredMaterial in model.Row.Materials)
                    {
                        var collectionMaterial = new CollectionMaterial(
                            requiredMaterial,
                            itemSheet[requiredMaterial.ItemId].Grade,
                            model.ItemType,
                            model.Active);

                        model.Materials.Add(collectionMaterial);
                    }
                }

                if (model.Active)
                {
                    continue;
                }

                foreach (var collectionMaterial in model.Materials)
                {
                    collectionMaterial.SetCondition(inventory);
                }
            }
        }

        public static (int activeCount, List<StatModifier> stats) GetEffect(this List<CollectionModel> models)
        {
            models = models.Where(model => model.Active).ToList();
            return (models.Count, models.SelectMany(model => model.Row.StatModifiers).ToList());
        }

        public static IEnumerable<CollectionModel> Sort(
            this IEnumerable<CollectionModel> models,
            ItemType itemType, StatType statType)
        {
            return models
                .Where(model => model.ItemType == itemType &&
                                model.Row.StatModifiers.Any(stat => stat.CheckStat(statType)))
                .OrderByDescending(model => model.CanActivate)
                .ToList();
        }

        public static bool CheckStat(this StatModifier stat, StatType statTabType)
        {
            StatType[] etcTypes =
            {
                StatType.CRI,
                StatType.DRV, StatType.DRR, StatType.CDMG,
                StatType.ArmorPenetration, StatType.Thorn,
            };

            switch (statTabType)
            {
                // All
                case StatType.NONE:
                    return true;

                // StatTypes in Tab
                case StatType.HP:
                case StatType.ATK:
                case StatType.DEF:
                case StatType.HIT:
                case StatType.SPD:
                    return statTabType == stat.StatType;

                // Etc
                default:
                    return etcTypes.Contains(stat.StatType);
            }
        }
    }
}
