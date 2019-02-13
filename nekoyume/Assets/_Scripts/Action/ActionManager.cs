using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Libplanet;
using Libplanet.Crypto;
using Nekoyume.Data.Table;
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

        public void MoveStage(int stage)
        {
            var action = new MoveStage {stage = stage};
            ProcessAction(action);
        }

        public void Sleep(Stats statsData)
        {
            var action = new Sleep();
            ProcessAction(action);
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

            var fileName = string.Format(AvatarFileFormat, index);
            _saveFilePath = Path.Combine(Application.persistentDataPath, fileName);
            Avatar = LoadStatus(_saveFilePath);

            var path = Path.Combine(Application.persistentDataPath, "planetarium");
            agent = new Agent(privateKey, path);
            agent.DidReceiveAction += ReceiveAction;

            Debug.Log($"User Address: 0x{agent.UserAddress.ToHex()}");
            StartMine();
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
    }
}
