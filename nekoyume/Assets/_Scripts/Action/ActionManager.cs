using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using Libplanet;
using Libplanet.Crypto;
using Libplanet.Net;
using Nekoyume.Data;
using Nekoyume.Game;
using Nekoyume.Game.Item;
using Nekoyume.Model;
using NetMQ;
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
                return Enumerable.Range(0, 3).Select(index => string.Format(AvatarFileFormat, index))
                    .Select(fileName => Path.Combine(Application.persistentDataPath, fileName))
                    .Select(path => LoadStatus(path)).ToList();
            }
        }


        private Agent agent;
        public BattleLog battleLog;
        public static ActionManager Instance { get; private set; }
        public Model.Avatar Avatar { get; private set; }
        public event EventHandler<Model.Avatar> DidAvatarLoaded;
        public const string PrivateKeyFormat = "private_key_{0}";
        public const string AvatarFileFormat = "avatar_{0}.dat";
        public const string PeersFileName = "peers.dat";
        public const string IceServersFileName = "ice_servers.dat";
        public const string ChainIdKey = "chain_id";
        public static Address shopAddress => default(Address);
        public Shop shop;
        public Tables tables;

        private IEnumerator _miner;
        private IEnumerator _txProcessor;
        private IEnumerator _avatarUpdator;
        private IEnumerator _shopUpdator;
        private IEnumerator _swarmRunner;

        public Address userAddress => agent.UserAddress;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Instance = this;
            tables = GameObject.Find("Game").GetComponent<Tables>();
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

            StartCoroutine(_avatarUpdator);
            StartCoroutine(_shopUpdator);
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

        public void UpdateItems(List<Inventory.InventoryItem> items)
        {
            Avatar.Items = items;
            SaveStatus();
        }

        private void ProcessAction(ActionBase action)
        {
            agent.queuedActions.Enqueue(action);
        }

        public void HackAndSlash(List<Equipment> equipments)
        {
            var action = new HackAndSlash
            {
                Equipments = equipments,
            };
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

            var storePath = Path.Combine(Application.persistentDataPath, "planetarium");
            var peers = LoadPeers();
            var iceServers = LoadIceServers();

            string host = GetCommandLineOption("host");
            int portStr;
            int? port = int.TryParse(GetCommandLineOption("port"), out portStr) 
                ? (int?)portStr 
                : null;

            agent = new Agent(
                privateKey: privateKey, 
                path: storePath, 
                chainId: chainId, 
                peers: peers, 
                iceServers: iceServers,
                host: host,
                port: port
            );
            agent.DidReceiveAction += ReceiveAction;
            agent.UpdateShop += UpdateShop;

            _txProcessor = agent.CoTxProcessor();
            _avatarUpdator = agent.CoAvatarUpdator();
            _shopUpdator = agent.CoShopUpdator();
            _swarmRunner = agent.CoSwarmRunner();

            StartCoroutine(_txProcessor);
            StartCoroutine(_swarmRunner);

            if (!HasCommandLineSwitch("no-miner"))
            {
                _miner = agent.CoMiner();
                StartCoroutine(_miner);
            }

            Debug.Log($"User Address: 0x{agent.UserAddress.ToHex()}");
        }

        private string GetCommandLineOption(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == $"--{name}") 
                {
                    return args[i+1];
                }
            }
            
            return null;
        }

        private bool HasCommandLineSwitch(string name)
        {
            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (arg == $"--{name}") 
                {
                    return true;
                }
            }
            
            return false;
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
        
        public void Combination(List<UI.Model.CountEditableItem<UI.Model.Inventory.Item>> materials)
        {
            var action = new CombinationRenew();
            materials.ForEach(m => action.Materials.Add(new CombinationRenew.ItemModel(m)));
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
        
        public void OnDestroy() 
        {
            if (agent != null)
            {
                PlayerPrefs.SetString(ChainIdKey, agent.ChainId.ToString());
                agent.Dispose();
            }
            
            NetMQConfig.Cleanup(false);
        }

        private IEnumerable<IceServer> LoadIceServers()
        {
            foreach (string line in LoadConfigLines(IceServersFileName)) 
            {
                var uri = new Uri(line);
                string[] userInfo = uri.UserInfo.Split(':');

                yield return new IceServer(new[] {uri}, userInfo[0], userInfo[1]);
            }
        }

        private IEnumerable<Peer> LoadPeers()
        {
            foreach (string line in LoadConfigLines(PeersFileName))
            {
                string[] tokens = line.Split(',');
                var pubKey = new PublicKey(ByteUtil.ParseHex(tokens[0]));
                string host = tokens[1];
                int port = int.Parse(tokens[2]);
                int version = int.Parse(tokens[3]);

                yield return new Peer(pubKey, new DnsEndPoint(host, port), version);
            }
        }


        private IEnumerable<string> LoadConfigLines(string fileName)
        {
            string userPath = Path.Combine(
                Application.persistentDataPath,
                fileName
            );
            string content;
            
            if (File.Exists(userPath))
            {
                content = File.ReadAllText(userPath);
            }
            else 
            {
                string assetName = Path.GetFileNameWithoutExtension(fileName);
                content = Resources.Load<TextAsset>($"Config/{assetName}").text;
            }

            foreach (var line in Regex.Split(content, "\n|\r|\r\n"))
            {
                if (!string.IsNullOrEmpty(line.Trim()))
                {
                    yield return line;
                }
            }
        }
    }
}
