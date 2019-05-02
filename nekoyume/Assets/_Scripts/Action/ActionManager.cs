using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using Nekoyume.Manager;
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

    public class ActionManager : MonoSingleton<ActionManager>
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
        public Model.Avatar Avatar { get; private set; }
        public event EventHandler<Model.Avatar> DidAvatarLoaded;
        public const string PrivateKeyFormat = "private_key_{0}";
        public const string AvatarFileFormat = "avatar_{0}.dat";
        public const string PeersFileName = "peers.dat";
        public const string IceServersFileName = "ice_servers.dat";
        public const string ChainIdKey = "chain_id";
        public static Address shopAddress => default(Address);

        public static Address RankingAddress => new Address(new byte[]
            {
                0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x1
            }
        );
        public Shop shop;

        private IEnumerator _miner;
        private IEnumerator _txProcessor;
        private IEnumerator _avatarUpdator;
        private IEnumerator _shopUpdator;
        private IEnumerator _swarmRunner;

        private IEnumerator _actionRetryer;

        public Address agentAddress => agent.AgentAddress;
        public Address AvatarAddress => agent.AvatarAddress;
        public RankingBoard rankingBoard;

#if UNITY_EDITOR
        private const string AgentStoreDirName = "planetarium_dev";
#else
        private const string AgentStoreDirName = "planetarium";
#endif

        protected override void Awake()
        {
            base.Awake();
            
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            DontDestroyOnLoad(gameObject);
            Tables.instance.EmptyMethod();
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

        public void StartAvatarCoroutines()
        {
            if (Avatar != null)
            {
                DidAvatarLoaded?.Invoke(this, Avatar);
            }

            StartNullableCoroutine(_avatarUpdator);
            StartNullableCoroutine(_shopUpdator);
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

        private void ProcessAction(GameAction action)
        {
            action.Id = Guid.NewGuid();
            agent.QueuedActions.Enqueue(action);
        }

        public void HackAndSlash(List<Equipment> equipments, List<Food> foods, int stage)
        {
            var action = new HackAndSlash
            {
                Equipments = equipments,
                Foods = foods,
                Stage = stage,
            };
            ProcessAction(action);

            var itemIDs = equipments.Select(e => e.Data.id).Concat(foods.Select(f => f.Data.id)).ToArray();
            AnalyticsManager.instance.Battle(itemIDs);
        }

        public void InitAgent()
        {
            PrivateKey privateKey = null;
            var key = string.Format(PrivateKeyFormat, "agent");
            var privateKeyHex = GetCommandLineOption("private-key")
                                ?? PlayerPrefs.GetString(key, "");

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

            var storePath = Path.Combine(Application.persistentDataPath, AgentStoreDirName);

#if UNITY_EDITOR
            var peers = new Peer[]{ };
            IceServer[] iceServers = null;
            string host = "127.0.0.1";
#else
            var peers = LoadPeers();
            var iceServers = LoadIceServers();
            string host = GetCommandLineOption("host");
#endif
            int portStr;
            int? port = int.TryParse(GetCommandLineOption("port"), out portStr) 
                ? (int?)portStr 
                : null;

            agent = new Agent(
                agentPrivateKey: privateKey, 
                path: storePath, 
                chainId: chainId, 
                peers: peers, 
                iceServers: iceServers,
                host: host,
                port: port
            );
            _txProcessor = agent.CoTxProcessor();
            _swarmRunner = agent.CoSwarmRunner();
            _actionRetryer = agent.CoActionRetryer();

            if (!HasCommandLineSwitch("no-miner"))
            {
                _miner = agent.CoMiner();   
            }

            agent.PreloadStarted += (_, __) => 
            {
                var loadingScreen = UI.Widget.Find<UI.LoadingScreen>();
                loadingScreen.Show();
            };

            agent.PreloadEnded += (_, __) => 
            {
                var loadingScreen = UI.Widget.Find<UI.LoadingScreen>();
                loadingScreen.Close();
            };

            agent.PreloadEnded += (_, __) =>
            {
                StartNullableCoroutine(_miner);
            };
        }

        public void StartSystemCoroutines()
        {
            StartNullableCoroutine(_txProcessor);
            StartNullableCoroutine(_actionRetryer);
            StartNullableCoroutine(_swarmRunner);
        }

        private Coroutine StartNullableCoroutine(IEnumerator routine)
        {
            if (ReferenceEquals(routine, null))
            {
                return null;
            }

            return StartCoroutine(routine);
        }

        public void InitAvatar(int index)
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

            agent.AvatarPrivateKey = privateKey;

            var fileName = string.Format(AvatarFileFormat, index);
            _saveFilePath = Path.Combine(Application.persistentDataPath, fileName);
            Avatar = LoadStatus(_saveFilePath);
            agent.DidReceiveAction += ReceiveAction;
            agent.UpdateShop += UpdateShop;

            _avatarUpdator = agent.CoAvatarUpdator();
            _shopUpdator = agent.CoShopUpdator();

            Debug.Log($"Agent Address: 0x{agent.AgentAddress.ToHex()}");
            Debug.Log($"Avatar Address: 0x{agent.AvatarAddress.ToHex()}");
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
        
        public void Combination(List<UI.Model.CountEditableItem<UI.Model.Inventory.Item>> materials)
        {
            var action = new Combination();
            materials.ForEach(m => action.Materials.Add(new Combination.ItemModel(m)));
            ProcessAction(action);
            AnalyticsManager.instance.OnEvent(AnalyticsManager.EventName.ActionCombination);
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

        protected override void OnDestroy() 
        {
            if (agent != null)
            {
                PlayerPrefs.SetString(ChainIdKey, agent.ChainId.ToString());
                agent.Dispose();
            }
            
            NetMQConfig.Cleanup(false);
            
            base.OnDestroy();
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
