using System;
using System.Collections;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Libplanet;
using Libplanet.Crypto;
using Nekoyume.Data.Table;
using Nekoyume.Game.Character;
using UnityEngine;

namespace Nekoyume.Action
{
    [Serializable]
    internal class SaveData
    {
        public Model.Avatar Avatar;
        public long? LastBlockId;
    }

    public class ActionManager : MonoBehaviour
    {
        public static ActionManager Instance { get; private set; }
        public event EventHandler<Model.Avatar> DidAvatarLoaded;
        public event EventHandler CreateAvatarRequired;
        public Model.Avatar Avatar { get; private set; }

        private Agent agent;
        private long? lastBlockId;
        private string _saveFilePath;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Instance = this;

            PrivateKey privateKey = null;
            var privateKeyHex = PlayerPrefs.GetString("private_key", "");

            if (string.IsNullOrEmpty(privateKeyHex))
            {
                privateKey = PrivateKey.Generate();
                PlayerPrefs.SetString("private_key", ByteUtil.Hex(privateKey.Bytes));
            }
            else
            {
                privateKey = new PrivateKey(ByteUtil.ParseHex(privateKeyHex));
            }

            _saveFilePath = Path.Combine(Application.persistentDataPath, "avatar.dat");
            LoadStatus();

            var path = Path.Combine(Application.persistentDataPath, "planetarium");
            agent = new Agent(privateKey, path);
            agent.DidReceiveAction += ReceiveAction;

            Debug.Log($"User Address: 0x{agent.UserAddress.Hex()}");

        }

        private void ReceiveAction(object sender, Model.Avatar e)
        {
            Model.Avatar avatar = Avatar;
            Avatar = e;
            SaveStatus();
            if (avatar == null)
            {
                DidAvatarLoaded?.Invoke(this, Avatar);
            }

        }

        public void StartSync()
        {
            if (Avatar != null)
            {
                DidAvatarLoaded?.Invoke(this, Avatar);
            }

            var address = agent.UserAddress;
            var states = agent.blocks.GetStates(new[] {address});
            var avatar = (Model.Avatar) states.GetValueOrDefault(address);
            if (avatar == null)
            {
                CreateAvatarRequired?.Invoke(this, null);
            }

            StartCoroutine(agent.Sync());
        }

        public IEnumerator Sync()
        {
            return agent.Sync();
        }

        public void CreateNovice(string nickName)
        {
            var action = new CreateNovice();
            ProcessAction(action);
        }

        private void LoadStatus()
        {
            if (!File.Exists(_saveFilePath)) return;

            var formatter = new BinaryFormatter();
            using (FileStream stream = File.Open(_saveFilePath, FileMode.Open))
            {
                var data = (SaveData) formatter.Deserialize(stream);
                Avatar = data.Avatar;
                lastBlockId = data.LastBlockId;
            }
        }

        private void SaveStatus()
        {
            var data = new SaveData
            {
                Avatar = Avatar,
                LastBlockId = lastBlockId
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

        public void HackAndSlash(Player player, int id)
        {
            var action = new HackAndSlash
            {
                hp = player.HP,
                zone = id,
                exp = player.EXP,
                level = player.Level,
                dead = player.IsDead(),
                items = player.SerializeItems(),
            };
            ProcessAction(action);
        }

        public void MoveZone(int i)
        {
            var action = new MoveZone(i);
            ProcessAction(action);
        }

        public void Sleep(Stats statsData)
        {
            var action = new Sleep();
            ProcessAction(action);
        }

        private void ProcessAction(ActionBase action)
        {
            agent.StageTransaction(new ActionBase[] {action});
            // TODO Delete StartMine when block.mine be async
            StartMine();
        }
    }
}
