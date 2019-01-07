using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using Libplanet;
using Libplanet.Action;
using Libplanet.Crypto;
using Nekoyume.Data.Table;
using Nekoyume.Game.Character;
using UnityEngine;
using Avatar = Nekoyume.Model.Avatar;

namespace Nekoyume.Action
{
    [Serializable]
    internal class SaveData
    {
        public Avatar Avatar;
        public long? LastBlockId;
    }

    public class ActionManager : MonoBehaviour
    {
        public static ActionManager Instance { get; private set; }
        public event EventHandler<Avatar> DidAvatarLoaded;
        public event EventHandler<Avatar> DidSleep;
        public event EventHandler CreateAvatarRequired;
        public Avatar Avatar { get; private set; }

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

            agent = new Agent(privateKey);

            Debug.Log($"User Address: 0x{agent.UserAddress.Hex()}");

//            StartMine();
        }

        public void StartSync()
        {
            if (Avatar != null)
            {
                DidAvatarLoaded?.Invoke(this, Avatar);
            }

            Address address = agent.UserAddress;
            AddressStateMap states = agent.blocks.GetStates(new[] {address});
            var avatar = (Avatar) states.GetValueOrDefault(address);
            if (avatar == null)
            {
                CreateAvatarRequired?.Invoke(this, null);
            }
            Debug.Log(avatar);
        }

        public IEnumerator Sync()
        {
            return agent.Sync();
        }

        public void CreateNovice(string nickName)
        {
            var action = new CreateNovice(nickName);
            agent.StageTransaction(new ActionBase[] {action});
            StartMine();
            while (true)
            {
                var states = agent.blocks.GetStates(new[] {agent.UserAddress});
                if (states != null)
                {
                    Debug.Log(states);
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
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
            throw new NotImplementedException();
        }

        public void HackAndSlash(Player player, int id)
        {
            throw new NotImplementedException();
        }

        public void MoveZone(int i)
        {
            throw new NotImplementedException();
        }

        public void Sleep(Stats statsData)
        {
            throw new NotImplementedException();
        }
    }
}
