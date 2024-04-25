using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Lib9c.Model.Order;
using Libplanet.Action.State;
using Libplanet.KeyStore;
using Nekoyume.Battle;
using Nekoyume.Extensions;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;
using Org.BouncyCastle.Crypto.Digests;
using UnityEngine;
using ZXing;
using ZXing.QrCode;
using Inventory = Nekoyume.Model.Item.Inventory;
using FormatException = System.FormatException;
using UnityEngine.Networking;

namespace Nekoyume.Helper
{
    public static class Util
    {
        public const int VisibleEnhancementEffectLevel = 10;
        private const string StoredSlotIndex = "AutoSelectedSlotIndex_";
        private static readonly List<int> CrystalEquipmentRecipes = new() { 158, 159, 160 };
        private static readonly Vector2 Pivot = new(0.5f, 0.5f);
        private static Dictionary<string, Sprite> CachedDownloadTextures = new Dictionary<string, Sprite>();

        private static Dictionary<string, byte[]> CachedDownloadTexturesRaw =
            new Dictionary<string, byte[]>();

        public const float GridScrollerAdjustCellCount = 20;

        public static async Task<Order> GetOrder(Guid orderId)
        {
            var address = Order.DeriveAddress(orderId);
            return await UniTask.RunOnThreadPool(async () =>
            {
                var state = await Game.Game.instance.Agent.GetStateAsync(
                    ReservedAddresses.LegacyAccount,
                    address);
                if (state is Dictionary dictionary)
                {
                    return OrderFactory.Deserialize(dictionary);
                }

                return null;
            }, configureAwait: false);
        }

        public static async Task<string> GetItemNameByOrderId(Guid orderId, bool isNonColored = false)
        {
            var order = await GetOrder(orderId);
            if (order == null)
            {
                return string.Empty;
            }

            var address = Addresses.GetItemAddress(order.TradableId);
            return await UniTask.RunOnThreadPool(async () =>
            {
                var state = await Game.Game.instance.Agent.GetStateAsync(
                    ReservedAddresses.LegacyAccount,
                    address);
                if (state is Dictionary dictionary)
                {
                    var itemBase = ItemFactory.Deserialize(dictionary);
                    return isNonColored
                        ? itemBase.GetLocalizedNonColoredName()
                        : itemBase.GetLocalizedName();
                }

                return string.Empty;
            }, configureAwait: false);
        }

        public static async Task<ItemBase> GetItemBaseByTradableId(Guid tradableId, long requiredBlockExpiredIndex)
        {
            var address = Addresses.GetItemAddress(tradableId);
            return await UniTask.RunOnThreadPool(async () =>
            {
                var state = await Game.Game.instance.Agent.GetStateAsync(
                    ReservedAddresses.LegacyAccount,
                    address);
                if (state is Dictionary dictionary)
                {
                    var itemBase = ItemFactory.Deserialize(dictionary);
                    var tradableItem = itemBase as ITradableItem;
                    tradableItem.RequiredBlockIndex = requiredBlockExpiredIndex;
                    return tradableItem as ItemBase;
                }

                return null;
            }, configureAwait: false);
        }

        public static int GetHourglassCount(Inventory inventory, long currentBlockIndex)
        {
            if (inventory is null)
            {
                return 0;
            }

            var count = 0;
            var materials =
                inventory.Items.OrderByDescending(x => x.item.ItemType == ItemType.Material);
            var hourglass = materials.Where(x => x.item.ItemSubType == ItemSubType.Hourglass);
            foreach (var item in hourglass)
            {
                if (item.item is TradableMaterial tradableItem)
                {
                    if (tradableItem.RequiredBlockIndex > currentBlockIndex)
                    {
                        continue;
                    }
                }

                count += item.count;
            }

            return count;
        }

        public static bool TryGetStoredAvatarSlotIndex(out int slotIndex)
        {
            if (Game.Game.instance.Agent is null)
            {
                NcDebug.LogError("[Util.TryGetStoredSlotIndex] agent is null");
                slotIndex = 0;
                return false;
            }

            var agentAddress = Game.Game.instance.Agent.Address;
            var key = $"{StoredSlotIndex}{agentAddress}";
            var hasKey = PlayerPrefs.HasKey(key);
            slotIndex = hasKey ? PlayerPrefs.GetInt(key) : 0;
            return hasKey;
        }

        public static void SaveAvatarSlotIndex(int slotIndex)
        {
            if (Game.Game.instance.Agent is null)
            {
                NcDebug.LogError("[Util.SaveSlotIndex] agent is null");
                return;
            }

            var agentAddress = Game.Game.instance.Agent.Address;
            var key = $"{StoredSlotIndex}{agentAddress}";
            PlayerPrefs.SetInt(key, slotIndex);
        }

        public static bool IsUsableItem(ItemBase itemBase)
        {
            var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
            if (currentAvatarState is null || itemBase is null)
            {
                return false;
            }

            return currentAvatarState.level >= GetItemRequirementLevel(itemBase);
        }

        public static int GetItemRequirementLevel(ItemBase itemBase)
        {
            var sheets = Game.Game.instance.TableSheets;
            var requirementSheet = sheets.ItemRequirementSheet;
            var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
            if (currentAvatarState is null)
            {
                return 0;
            }

            switch (itemBase.ItemType)
            {
                case ItemType.Equipment:
                    var equipment = (Equipment)itemBase;
                    if (!requirementSheet.TryGetValue(itemBase.Id, out var equipmentRow))
                    {
                        NcDebug.LogError($"[ItemRequirementSheet] item id does not exist {itemBase.Id}");
                        return 0;
                    }

                    var recipeSheet = sheets.EquipmentItemRecipeSheet;
                    var subRecipeSheet = sheets.EquipmentItemSubRecipeSheetV2;
                    var itemOptionSheet = sheets.EquipmentItemOptionSheet;
                    var isMadeWithMimisbrunnrRecipe = equipment.IsMadeWithMimisbrunnrRecipe(
                        recipeSheet,
                        subRecipeSheet,
                        itemOptionSheet
                    );

                    return isMadeWithMimisbrunnrRecipe ? equipmentRow.MimisLevel : equipmentRow.Level;
                default:
                    return requirementSheet.TryGetValue(itemBase.Id, out var row) ? row.Level : 0;
            }
        }

        public static bool CanBattle(
            List<Equipment> equipments,
            List<Costume> costumes,
            IEnumerable<int> foodIds)
        {
            var isValidated = false;
            var tableSheets = Game.Game.instance.TableSheets;
            try
            {
                var costumeIds = costumes.Select(costume => costume.Id);
                States.Instance.CurrentAvatarState.ValidateItemRequirement(
                    costumeIds.Concat(foodIds).ToList(),
                    equipments,
                    tableSheets.ItemRequirementSheet,
                    tableSheets.EquipmentItemRecipeSheet,
                    tableSheets.EquipmentItemSubRecipeSheetV2,
                    tableSheets.EquipmentItemOptionSheet,
                    States.Instance.CurrentAvatarState.address.ToHex());
                isValidated = true;
            }
            catch (Exception e)
            {
                NcDebug.LogError(
                    $"Check the player is equipped with the valid equipment.\nException: {e}");
            }

            return isValidated;
        }

        public static int GetGridItemCount(float cellSize, float spacing, float size)
        {
            var s = size;
            var count = 0;
            while (s >= cellSize)
            {
                s -= cellSize;
                s -= spacing;
                count++;
                if (s < 0)
                {
                    return count;
                }
            }

            return count;
        }

        public static int TotalCP(BattleType battleType)
        {
            var avatarState = Game.Game.instance.States.CurrentAvatarState;
            var level = avatarState.level;
            var characterSheet = Game.Game.instance.TableSheets.CharacterSheet;
            if (!characterSheet.TryGetValue(avatarState.characterId, out var row))
            {
                throw new SheetRowNotFoundException("CharacterSheet", avatarState.characterId);
            }

            var costumeSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
            var runeOptionSheet = Game.Game.instance.TableSheets.RuneOptionSheet;
            var (equipments, costumes) = States.Instance.GetEquippedItems(battleType);
            var runeStates = States.Instance.GetEquippedRuneStates(battleType);
            var runeOptionInfos = GetRuneOptions(runeStates, runeOptionSheet);

            var allRuneState = States.Instance.AllRuneState;
            var runeListSheet = Game.Game.instance.TableSheets.RuneListSheet;
            var runeLevelBonusSheet = Game.Game.instance.TableSheets.RuneLevelBonusSheet;
            var runeLevelBonus = RuneHelper.CalculateRuneLevelBonus(allRuneState,
                runeListSheet, runeLevelBonusSheet);

            var collectionState = Game.Game.instance.States.CollectionState;
            var collectionSheet = Game.Game.instance.TableSheets.CollectionSheet;
            var collectionStatModifiers = collectionState.GetEffects(collectionSheet);
            return CPHelper.TotalCP(equipments, costumes, runeOptionInfos, level, row, costumeSheet,
                collectionStatModifiers, runeLevelBonus);
        }

        public static (int previousCP, int currentCP) GetCpChanged(
            CollectionState previousState, CollectionState currentState)
        {
            var avatarState = Game.Game.instance.States.CurrentAvatarState;
            var level = avatarState.level;
            var characterSheet = Game.Game.instance.TableSheets.CharacterSheet;
            var row = characterSheet[avatarState.characterId];

            var costumeSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
            var runeOptionSheet = Game.Game.instance.TableSheets.RuneOptionSheet;
            var (equipments, costumes) = States.Instance.GetEquippedItems(BattleType.Adventure);
            var runeStates = States.Instance.GetEquippedRuneStates(BattleType.Adventure);
            var runeOptionInfos = GetRuneOptions(runeStates, runeOptionSheet);

            var allRuneState = States.Instance.AllRuneState;
            var runeListSheet = Game.Game.instance.TableSheets.RuneListSheet;
            var runeLevelBonusSheet = Game.Game.instance.TableSheets.RuneLevelBonusSheet;
            var runeLevelBonus = RuneHelper.CalculateRuneLevelBonus(allRuneState,
                runeListSheet, runeLevelBonusSheet);

            var collectionSheet = Game.Game.instance.TableSheets.CollectionSheet;
            var previousCp = CPHelper.TotalCP(equipments, costumes, runeOptionInfos, level, row,
                costumeSheet, previousState.GetEffects(collectionSheet), runeLevelBonus);
            var currentCp = CPHelper.TotalCP(equipments, costumes, runeOptionInfos, level, row,
                costumeSheet, currentState.GetEffects(collectionSheet), runeLevelBonus);
            return (previousCp, currentCp);
        }

        public static (int previousCP, int currentCP) GetCpChanged(
            AllRuneState previousState, AllRuneState currentState)
        {
            var avatarState = Game.Game.instance.States.CurrentAvatarState;
            var level = avatarState.level;
            var characterSheet = Game.Game.instance.TableSheets.CharacterSheet;
            var row = characterSheet[avatarState.characterId];

            var costumeSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
            var runeOptionSheet = Game.Game.instance.TableSheets.RuneOptionSheet;
            var (equipments, costumes) = States.Instance.GetEquippedItems(BattleType.Adventure);

            var previousRuneStates =
                States.Instance.GetEquippedRuneStates(previousState, BattleType.Adventure);
            var previousRuneOptionInfos = GetRuneOptions(previousRuneStates, runeOptionSheet);
            var currentRuneStates =
                States.Instance.GetEquippedRuneStates(currentState, BattleType.Adventure);
            var currentRuneOptionInfos = GetRuneOptions(currentRuneStates, runeOptionSheet);

            var runeListSheet = Game.Game.instance.TableSheets.RuneListSheet;
            var runeLevelBonusSheet = Game.Game.instance.TableSheets.RuneLevelBonusSheet;

            var prevRuneLevelBonus = RuneHelper.CalculateRuneLevelBonus(previousState,
                runeListSheet, runeLevelBonusSheet);
            var currentRuneLevelBonus = RuneHelper.CalculateRuneLevelBonus(currentState,
                runeListSheet, runeLevelBonusSheet);

            var collectionState = Game.Game.instance.States.CollectionState;
            var collectionSheet = Game.Game.instance.TableSheets.CollectionSheet;
            var collectionStatModifiers = collectionState.GetEffects(collectionSheet);

            var previousCp = CPHelper.TotalCP(equipments, costumes, previousRuneOptionInfos,
                level, row, costumeSheet, collectionStatModifiers, prevRuneLevelBonus);
            var currentCp = CPHelper.TotalCP(equipments, costumes, currentRuneOptionInfos,
                level, row, costumeSheet, collectionStatModifiers, currentRuneLevelBonus);
            return (previousCp, currentCp);
        }

        public static List<RuneOptionSheet.Row.RuneOptionInfo> GetRuneOptions(
            List<RuneState> runeStates,
            RuneOptionSheet sheet)
        {
            var result = new List<RuneOptionSheet.Row.RuneOptionInfo>();
            foreach (var runeState in runeStates)
            {
                if (!sheet.TryGetValue(runeState.RuneId, out var row))
                {
                    continue;
                }

                if (!row.LevelOptionMap.TryGetValue(runeState.Level, out var statInfo))
                {
                    continue;
                }

                result.Add(statInfo);
            }

            return result;
        }

        public static int GetRuneCp(RuneState runeState)
        {
            var runeOptionSheet = Game.Game.instance.TableSheets.RuneOptionSheet;
            if (!runeOptionSheet.TryGetValue(runeState.RuneId, out var optionRow))
            {
                return 0;
            }
            if (!optionRow.LevelOptionMap.TryGetValue(runeState.Level, out var option))
            {
                return 0;
            }

            return option.Cp;
        }

        public static int GetPortraitId(List<Equipment> equipments, List<Costume> costumes)
        {
            var id = GameConfig.DefaultAvatarArmorId;
            var armor = equipments.FirstOrDefault(x => x.ItemSubType == ItemSubType.Armor);
            if (armor != null)
            {
                id = armor.Id;
            }

            var fullCostume = costumes.FirstOrDefault(x => x.ItemSubType == ItemSubType.FullCostume);
            if (fullCostume != null)
            {
                id = fullCostume.Id;
            }

            return id;
        }

        public static int GetPortraitId(BattleType battleType)
        {
            var (equipments, costumes) = States.Instance.GetEquippedItems(battleType);
            return GetPortraitId(equipments, costumes);
        }

        public static int GetArmorId()
        {
            var (equipments, costumes) = States.Instance.GetEquippedItems(BattleType.Adventure);
            var id = GameConfig.DefaultAvatarArmorId;
            var armor = equipments.FirstOrDefault(x => x.ItemSubType == ItemSubType.Armor);
            if (armor != null)
            {
                id = armor.Id;
            }

            return id;
        }

        public static string ComputeHash(string rawTransaction)
        {
            var offset = rawTransaction.StartsWith("0x") ? 2 : 0;
            var txByte = Enumerable.Range(offset, rawTransaction.Length - offset)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(rawTransaction.Substring(x, 2), 16))
                .ToArray();

            var digest = new KeccakDigest(256);
            digest.BlockUpdate(txByte, 0, txByte.Length);
            var calculatedHash = new byte[digest.GetByteLength()];
            digest.DoFinal(calculatedHash, 0);
            var transactionHash =
                BitConverter.ToString(calculatedHash, 0, 32).Replace("-", "").ToLower();
            return transactionHash;
        }

        public static bool IsEventEquipmentRecipe(int recipeId)
        {
            return CrystalEquipmentRecipes.Contains(recipeId);
        }

        public static int GetTickerGrade(string ticker)
        {
            var grade = 1;
            if (RuneFrontHelper.TryGetRuneData(ticker, out var runeData))
            {
                var sheet = Game.Game.instance.TableSheets.RuneListSheet;
                if (sheet.TryGetValue(runeData.id, out var row))
                {
                    grade = row.Grade;
                }
            }

            var petSheet = Game.Game.instance.TableSheets.PetSheet;
            var petRow = petSheet.Values.FirstOrDefault(x => x.SoulStoneTicker == ticker);
            if (petRow is not null)
            {
                grade = petRow.Grade;
            }

            return grade;
        }

        public static List<string> GetTickers()
        {
            var tickers = new List<string>();

            var runeSheet = Game.Game.instance.TableSheets.RuneSheet;
            tickers.AddRange(runeSheet.Values.Select(r => r.Ticker).ToList());

            var petSheet = Game.Game.instance.TableSheets.PetSheet;
            tickers.AddRange(petSheet.Values.Select(r => r.SoulStoneTicker));
            return tickers;
        }

        /// <summary>
        /// Deserializes a string token, return only 'version'.
        /// <see href="https://github.com/planetarium/libplanet/blob/1.0.0/Libplanet.Net/AppProtocolVersion.cs/#L148">Libplanet.Net.AppProtocolVersion.FromToken()</see>
        /// </summary>
        public static void TryGetAppProtocolVersionFromToken(string token, out int apv)
        {
            apv = 0;
            if (string.IsNullOrEmpty(token))
            {
                NcDebug.LogWarning("apv token is null.");
                return;
            }

            var pos = token.IndexOf('/');
            if (pos < 0)
            {
                NcDebug.LogException(new FormatException("Failed to find the first field delimiter."));
                return;
            }

            int version;
            try
            {
                version = int.Parse(token.Substring(0, pos), CultureInfo.InvariantCulture);
            }
            catch (Exception e) when (e is OverflowException or FormatException)
            {
                NcDebug.LogException(new FormatException($"Failed to parse a version number: {e}", e));
                return;
            }

            apv = version;
        }

        public static string AesEncrypt(string plainText)
        {
            using Aes aesAlg = Aes.Create();
            using SHA256 sha256 = SHA256.Create();
            aesAlg.Key = sha256.ComputeHash(Encoding.UTF8.GetBytes(SystemInfo.deviceUniqueIdentifier));
            byte[] iv = new byte[16];
            Array.Copy(aesAlg.Key, iv, 16);
            aesAlg.IV = iv;

            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            using var msEncrypt = new System.IO.MemoryStream();
            using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
            using (var swEncrypt = new System.IO.StreamWriter(csEncrypt))
            {
                swEncrypt.Write(plainText);
            }

            return Convert.ToBase64String(msEncrypt.ToArray());
        }

        public static string AesDecrypt(string encryptedText)
        {
            string result = string.Empty;
            try {
                using Aes aesAlg = Aes.Create();
                using SHA256 sha256 = SHA256.Create();
                aesAlg.Key = sha256.ComputeHash(Encoding.UTF8.GetBytes(SystemInfo.deviceUniqueIdentifier));
                byte[] iv = new byte[16];
                Array.Copy(aesAlg.Key, iv, 16);
                aesAlg.IV = iv;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using var msDecrypt = new System.IO.MemoryStream(Convert.FromBase64String(encryptedText));
                using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
                using var srDecrypt = new System.IO.StreamReader(csDecrypt);
                result = srDecrypt.ReadToEnd();
            }
            catch
            {
                return result;
            }
            return result;
        }

        public static string GetKeystoreJson()
        {
            IKeyStore store;

            if (Platform.IsMobilePlatform())
            {
                string dataPath = Platform.GetPersistentDataPath("keystore");
                store = new Web3KeyStore(dataPath);
            }
            else
            {
                store = Web3KeyStore.DefaultKeyStore;
            }

            if (!store.ListIds().Any())
            {
                return string.Empty;
            }

            var ppk = store.Get(store.ListIds().First());
            var stream = new MemoryStream();
            ppk.WriteJson(stream);
            return Encoding.ASCII.GetString(stream.ToArray());
        }

        public static Texture2D GetQrCodePngFromKeystore()
        {
            var json = GetKeystoreJson();
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            var writer = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions
                {
                    Width = 400,
                    Height = 400,
                },
            };

            var encoded = new Texture2D(400, 400);
            var res = writer.Write(json);
            encoded.SetPixels32(res);

            return encoded;
        }

        public static async UniTask<Sprite> DownloadTexture(string url)
        {
            if (CachedDownloadTextures.TryGetValue(url, out var cachedTexture))
            {
                return cachedTexture;
            }

            if (CachedDownloadTexturesRaw.TryGetValue(url, out var cachedTextureRaw))
            {
                Sprite result = CreateSprite(cachedTextureRaw);
                CachedDownloadTextures.Add(url, result);
                return result;
            }

            if (CachedDownloadTextures.TryGetValue(url, out cachedTexture))
            {
                return cachedTexture;
            }

            try
            {
                var rawData = await DownloadTextureRaw(url);
                if (rawData == null)
                {
                    NcDebug.LogError($"[DownloadTexture] DownloadTextureRaw({url}) is null.");
                    return null;
                }

                var result = CreateSprite(rawData);
                CachedDownloadTextures.Add(url, result);
                return result;
            }
            catch (Exception e)
            {
                NcDebug.LogError($"[DownloadTexture] {url}\n{e}");
                return null;
            }
        }

        private static Sprite CreateSprite(byte[] cachedTextureRaw)
        {
            if (cachedTextureRaw == null)
                return null;
            var myTexture = new Texture2D(0, 0, TextureFormat.RGBA32, false);
            myTexture.LoadImage(cachedTextureRaw);
            var result = Sprite.Create(
                myTexture,
                new Rect(0, 0, myTexture.width, myTexture.height),
                Pivot);
            return result;
        }

        public static async UniTask<byte[]> DownloadTextureRaw(string url)
        {
            if (CachedDownloadTexturesRaw.TryGetValue(url, out var cachedTexture))
            {
                return cachedTexture;
            }

            var req = UnityWebRequestTexture.GetTexture(url);
            req = await req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(req.error);
                if (CachedDownloadTexturesRaw.TryGetValue(url, out cachedTexture))
                {
                    return cachedTexture;
                }

                NcDebug.LogError($"[DownloadTextureRaw] {url}");
                return null;
            }

            var data = ((DownloadHandlerTexture)req.downloadHandler).data;
            CachedDownloadTexturesRaw.TryAdd(url, data);
            return data;
        }

        public static Sprite GetTexture(string url)
        {
            return CachedDownloadTextures.GetValueOrDefault(url);
        }


        public static void SetActiveSafe(this GameObject obj, bool active)
        {
            if (obj != null)
            {
                obj.SetActive(active);
            }
            else
            {
                NcDebug.LogWarning("[SetActiveSafe] fail");
            }
        }
    }
}
