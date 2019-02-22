using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Libplanet;
using Libplanet.Crypto;
using Nekoyume.Game;
using Nekoyume.Game.Item;
using Nekoyume.Model;
using UnityEngine;

namespace Nekoyume.Action
{
    [Serializable]
    internal class SaveData
    {
        public Model.Avatar Avatar;
    }

    public class ActionManager : MonoBehaviour
    {
        private string _saveFilePath;
        public List<Model.Avatar> Avatars
        {
            get
            {
                return Enumerable.Range(0, 3).Select(index => string.Format(AvatarFileFormat, index)).Select(fileName => Path.Combine(Application.persistentDataPath, fileName)).Select(path => LoadStatus(path)).Where(avatar => avatar != null).ToList();
            }
        }


        private Agent agent;
        public BattleLog battleLog;
        public static ActionManager Instance { get; private set; }
        public Model.Avatar Avatar { get; private set; }
        public event EventHandler<Model.Avatar> DidAvatarLoaded;
        public const string PrivateKeyFormat = "private_key_{0}";
        public const string AvatarFileFormat = "avatar_{0}.dat";
        public const string ChainIdKey = "chain_id";
        public static Address shopAddress => default(Address);
        public Shop shop;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }

        private void ReceiveAction(object sender, Context ctx)
        {
            var avatar = Avatar;
            Avatar = ctx.avatar;
            SaveStatus();
            if (avatar == null)
            {
                DidAvatarLoaded?.Invoke(this, Avatar);
            }
            battleLog = ctx.battleLog;
        }

        public void StartSync()
        {
            if (Avatar != null)
            {
                DidAvatarLoaded?.Invoke(this, Avatar);
            }

            StartCoroutine(agent.SyncShop());
            StartCoroutine(Sync());
        }

        private IEnumerator Sync()
        {
            return agent.Sync();
        }

        public void CreateNovice(string nickName)
        {
            var action = new CreateNovice
            {
                name = nickName
            };
            ProcessAction(action);
        }

        private Model.Avatar LoadStatus(string path)
        {
            if (File.Exists(path))
            {
                var formatter = new BinaryFormatter();
                using (FileStream stream = File.Open(path, FileMode.Open))
                {
                    var data = (SaveData) formatter.Deserialize(stream);
                    return data.Avatar;
                }
            }
            return null;
        }

        private void SaveStatus()
        {
            var data = new SaveData
            {
                Avatar = Avatar,
            };
            var formatter = new BinaryFormatter();
            using (FileStream stream = File.Open(_saveFilePath, FileMode.OpenOrCreate))
            {
                formatter.Serialize(stream, data);
            }
        }

        private void StartMine()
        {
            StartCoroutine(agent.Mine());
        }

        public void UpdateItems(string serializeItems)
        {
            Avatar.Items = serializeItems;
            SaveStatus();
        }

        private void ProcessAction(ActionBase action)
        {
            agent.queuedActions.Enqueue(action);
        }

        public void HackAndSlash()
        {
            var action = new HackAndSlash();
            ProcessAction(action);
        }

        public void Init(int index)
        {
            PrivateKey privateKey = null;
            var key = string.Format(PrivateKeyFormat, index);
            var privateKeyHex = PlayerPrefs.GetString(key, "");

            if (string.IsNullOrEmpty(privateKeyHex))
            {
                privateKey = new PrivateKey();
                PlayerPrefs.SetString(key, ByteUtil.Hex(privateKey.ByteArray));
            }
            else
            {
                privateKey = new PrivateKey(ByteUtil.ParseHex(privateKeyHex));
            }            

            Guid chainId;
            var chainIdStr = PlayerPrefs.GetString(ChainIdKey, "");
            if (string.IsNullOrEmpty(chainIdStr))
            {
                chainId = Guid.NewGuid();
                PlayerPrefs.SetString(ChainIdKey, chainId.ToString());
            }
            else 
            {
                chainId = Guid.Parse(chainIdStr);
            }

            var fileName = string.Format(AvatarFileFormat, index);
            _saveFilePath = Path.Combine(Application.persistentDataPath, fileName);
            Avatar = LoadStatus(_saveFilePath);

            var path = Path.Combine(Application.persistentDataPath, "planetarium");
            agent = new Agent(privateKey, path, chainId);
            agent.DidReceiveAction += ReceiveAction;
            agent.UpdateShop += UpdateShop;

            Debug.Log($"User Address: 0x{agent.UserAddress.ToHex()}");
            StartMine();
        }

        private void UpdateShop(object sender, Shop newShop)
        {
            shop = newShop;
        }

        public void Combination()
        {
            var action = new Combination
            {
                material_1 = 101001,
                material_2 = 101001,
                material_3 = 101001,
                result = 301001,
            };
            ProcessAction(action);
        }

        public void Sell(List<ItemBase> items, decimal price)
        {
            var action = new Sell
            {
                Items = items,
                Price = price,
            };
            ProcessAction(action);
        }
    }
}
