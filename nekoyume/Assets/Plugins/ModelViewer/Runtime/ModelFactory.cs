// #define MODEL_FACTORY_DEBUG_ENABLED

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Quest;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Material = Nekoyume.Model.Item.Material;
#if MODEL_FACTORY_DEBUG_ENABLED
using UnityEngine;
#endif

namespace ModelViewer.Runtime
{
    public static class ModelFactory
    {
        private static bool _initialized;
        private static Dictionary<Type, Type> _sheetRowTypeAndSheetTypes;
        private static TableSheets _tableSheets;
        private static Dictionary<Type, object> _sheetTypeAndFirstRowOfTableSheets;

        public static object Create(Type type)
        {
#if MODEL_FACTORY_DEBUG_ENABLED
            Debug.Log($"[ModelFactory] Create `{type?.FullName ?? "null"}`");
#endif

            Init();

            if (type == null)
            {
                return null;
            }

            // Check primitive end enum types
            if (type.IsPrimitive || type.IsEnum)
            {
                return Activator.CreateInstance(type);
            }

            // Check string type
            if (type == typeof(string))
            {
                return string.Empty;
            }

            // Check interface type
            if (type.IsInterface)
            {
                // Check generic interface type
                if (type.IsGenericType)
                {
                    var genericTypeDef = type.GetGenericTypeDefinition();
                    if (typeof(IEnumerable<>).IsAssignableFrom(genericTypeDef))
                    {
                        var elementType = type.GetGenericArguments()[0];
                        var arr = Array.CreateInstance(elementType, 1);
                        arr.SetValue(Create(elementType), 0);
                        return arr;
                    }

                    return null;
                }

                if (typeof(ITradableItem).IsAssignableFrom(type))
                {
                    var row = _tableSheets.EquipmentItemSheet.First!;
                    return ItemFactory.CreateItemUsable(row, Guid.NewGuid(), 0L);
                }

                return null;
            }

            // Check assignable to ISheet type
            if (typeof(ISheet).IsAssignableFrom(type))
            {
                return typeof(TableSheets).GetProperty(type.Name)?.GetValue(_tableSheets)
                       ?? Activator.CreateInstance(type);
            }

            // Check SheetRow<> type
            // NOTE: This check should be placed after ISheet check because
            //       to use specific row in _tableSheets.
            if (GetGenericBaseTypeRecursively(typeof(SheetRow<>), type) != null)
            {
                if (!_sheetRowTypeAndSheetTypes.TryGetValue(type, out var sheetType))
                {
                    return null;
                }

                return _sheetTypeAndFirstRowOfTableSheets.TryGetValue(
                    sheetType,
                    out var firstRow)
                    ? firstRow
                    : null;
            }

            // Check specific types
            if (type == typeof(Guid))
            {
                return Guid.NewGuid();
            }

            if (type == typeof(PublicKey))
            {
                return new PrivateKey().PublicKey;
            }

            if (type == typeof(Address))
            {
                return new PrivateKey().ToAddress();
            }

            if (type == typeof(Currency))
            {
                return Currency.Legacy("NCG", 2, minters: null);
            }

            if (type == typeof(FungibleAssetValue))
            {
                return new FungibleAssetValue(
                    Create<Currency>(),
                    0,
                    0);
            }

            if (type == typeof(Inventory.Item))
            {
                return new Inventory.Item(
                    ItemFactory.CreateMaterial(_tableSheets.MaterialItemSheet.First),
                    1);
            }

            if (type == typeof(ShopItem))
            {
                return new ShopItem(
                    Create<Address>(),
                    Create<Address>(),
                    Guid.NewGuid(),
                    Create<FungibleAssetValue>(),
                    0L,
                    Create<ITradableItem>());
            }

            if (type == typeof(Buy7.BuyerResult))
            {
                var shopItem = Create<ShopItem>();
                return new Buy7.BuyerResult
                {
                    id = shopItem.ProductId,
                    shopItem = shopItem,
                    itemUsable = shopItem.ItemUsable,
                    costume = shopItem.Costume,
                    tradableFungibleItem = shopItem.TradableFungibleItem,
                    tradableFungibleItemCount = shopItem.TradableFungibleItemCount,
                };
            }

            if (type == typeof(Buy7.SellerResult))
            {
                var shopItem = Create<ShopItem>();
                return new Buy7.SellerResult
                {
                    id = shopItem.ProductId,
                    shopItem = shopItem,
                    itemUsable = shopItem.ItemUsable,
                    costume = shopItem.Costume,
                    gold = Create<FungibleAssetValue>(),
                };
            }

            if (type == typeof(CombinationConsumable5.ResultModel))
            {
                var row = _tableSheets.ConsumableItemRecipeSheet.First!;
                var materials = row.Materials.ToDictionary(
                    mi => ItemFactory.CreateMaterial(_tableSheets.MaterialItemSheet, mi.Id),
                    mi => mi.Count);
                var consumable = ItemFactory.CreateItemUsable(
                    _tableSheets.ConsumableItemSheet[row.ResultConsumableItemId],
                    Guid.NewGuid(),
                    0L);
                return new CombinationConsumable5.ResultModel
                {
                    id = Guid.NewGuid(),
                    actionPoint = row.RequiredActionPoint,
                    materials = materials,
                    itemUsable = consumable,
                    recipeId = row.Id,
                };
            }

            if (type == typeof(DailyReward2.DailyRewardResult))
            {
                return new DailyReward2.DailyRewardResult
                {
                    id = Guid.NewGuid(),
                    materials = new Dictionary<Material, int>
                    {
                        {
                            ItemFactory.CreateMaterial(_tableSheets.MaterialItemSheet.First),
                            1
                        },
                    }
                };
            }

            if (type == typeof(ItemEnhancement.ResultModel))
            {
                var row = _tableSheets.EquipmentItemSheet.First!;
                var preItemUsable = (Equipment)ItemFactory.CreateItemUsable(
                    row,
                    Guid.NewGuid(),
                    0L);
                var itemUsable = new Equipment((Dictionary)preItemUsable.Serialize());
                itemUsable.LevelUpV1();
                return new ItemEnhancement.ResultModel
                {
                    id = Guid.NewGuid(),
                    preItemUsable = preItemUsable,
                    itemUsable = itemUsable,
                    materialItemIdList = new[] { Guid.NewGuid() },
                    actionPoint = 0,
                    enhancementResult = ItemEnhancement.EnhancementResult.Success,
                    gold = 0,
                    CRYSTAL = new FungibleAssetValue(
                        Currency.Legacy("CRYSTAL", 18, minters: null),
                        0,
                        0),
                };
            }

            if (type == typeof(SellCancellation.Result))
            {
                var shopItem = Create<ShopItem>();
                return new SellCancellation.Result
                {
                    id = shopItem.ProductId,
                    shopItem = shopItem,
                    itemUsable = shopItem.ItemUsable,
                    costume = shopItem.Costume,
                    tradableFungibleItem = shopItem.TradableFungibleItem,
                    tradableFungibleItemCount = shopItem.TradableFungibleItemCount,
                };
            }
            // ~Check specific types

            var tuples = type
                .GetConstructors(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic)
                .Select(ctorInfo => (ctorInfo, paramInfos: ctorInfo.GetParameters()))
                .ToArray();
            // Use default constructor
            foreach (var (ctorInfo, paramInfos) in tuples)
            {
                if (paramInfos.Length > 0)
                {
                    continue;
                }

#if MODEL_FACTORY_DEBUG_ENABLED
                Debug.Log($"[ModelFactory] {type.FullName}: invoke default constructor");
#endif
                return ctorInfo.Invoke(new object[] { });
            }

            // Use other constructors which require parameters
            foreach (var (ctorInfo, paramInfos) in tuples)
            {
                // Skip private constructors which require parameters
                if (ctorInfo.IsPrivate)
                {
                    continue;
                }

                var joinedParamInfos = string.Join(
                    ", ",
                    paramInfos.Select(pi => pi.ParameterType.FullName));
                var objects = paramInfos
                    .Select(pi =>
                    {
                        // Check specific types
                        if (pi.ParameterType == typeof(QuestSheet.Row))
                        {
                            if (type == typeof(CombinationEquipmentQuest))
                            {
                                return typeof(CombinationEquipmentQuestSheet.Row);
                            }

                            return null;
                        }

                        if (pi.ParameterType == typeof(AttachmentActionResult))
                        {
                            if (type == typeof(BuyerMail))
                            {
                                return typeof(Buy7.BuyerResult);
                            }

                            if (type == typeof(DailyRewardMail))
                            {
                                return typeof(DailyReward2.DailyRewardResult);
                            }

                            if (type == typeof(ItemEnhanceMail))
                            {
                                return typeof(ItemEnhancement.ResultModel);
                            }

                            if (type == typeof(MonsterCollectionMail))
                            {
                                return typeof(MonsterCollectionResult);
                            }

                            if (type == typeof(SellerMail))
                            {
                                return typeof(Buy7.SellerResult);
                            }

                            return null;
                        }

                        return pi.ParameterType;
                    })
                    .Select(Create)
                    .ToArray();
                var joinedObjects = string.Join(
                    ", ",
                    objects.Select(obj => obj?.GetType().FullName ?? "null"));
                if (objects.Any(obj => obj is null))
                {
#if MODEL_FACTORY_DEBUG_ENABLED
                    Debug.Log($"[ModelFactory] {type.FullName}: " +
                              $"cannot invoke constructor({joinedParamInfos})" +
                              $" with ({joinedObjects})");
#endif
                    continue;
                }

#if MODEL_FACTORY_DEBUG_ENABLED
                Debug.Log($"[ModelFactory] {type.FullName}: " +
                          $"invoke constructor({joinedParamInfos})" +
                          $" with ({joinedObjects})");
#endif
                return ctorInfo.Invoke(objects);
            }

            return null;
        }

        public static T Create<T>()
        {
            var t = Create(typeof(T));
            return t is null ? default : (T)t;
        }

        private static void Init()
        {
            if (_initialized)
            {
                return;
            }

            var iSheetType = typeof(ISheet);
            var sheetType = typeof(Sheet<,>);
            var sheetTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(asm => asm.GetTypes())
                .Where(t => iSheetType.IsAssignableFrom(t) &&
                            t.BaseType is { IsGenericType: true } &&
                            t.BaseType.GetGenericTypeDefinition() == sheetType &&
                            t.BaseType.GetGenericArguments().Length == 2)
                .ToArray();
            _sheetRowTypeAndSheetTypes = sheetTypes.ToDictionary(
                t => t.BaseType!.GetGenericArguments()[1],
                t => t);

            _tableSheets = TableSheetsHelper.MakeTableSheets();

            _sheetTypeAndFirstRowOfTableSheets = typeof(TableSheets).GetProperties()
                .Where(pi => _sheetRowTypeAndSheetTypes.Values.Contains(pi.PropertyType))
                .ToDictionary(
                    pi => pi.PropertyType,
                    pi => pi.PropertyType
                        .GetProperty("First")!
                        .GetValue(pi.GetValue(_tableSheets)));

            _initialized = true;
        }

        private static Type GetGenericBaseTypeRecursively(Type genericBaseType, Type type)
        {
            if (type.BaseType is null)
            {
                return null;
            }

            if (type.BaseType.IsGenericType &&
                type.BaseType.GetGenericTypeDefinition() == genericBaseType)
            {
                return type;
            }

            return GetGenericBaseTypeRecursively(genericBaseType, type.BaseType);
        }
    }
}
