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
using Nekoyume.Helper;
using Nekoyume.Model;
using NetMQ;
using UniRx;
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

        private IEnumerator _miner;
        private IEnumerator _txProcessor;
        private IEnumerator _swarmRunner;

        private IEnumerator _actionRetryer;

        public Address agentAddress => agent.AgentAddress;
        public Address AvatarAddress => agent.AvatarAddress;
        
        public Shop Shop { get; private set; }

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

        private void ReceiveAction(Context ctx)
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

        public object GetState(Address address)
        {
            return agent.GetState(address);
        }

        /// <summary>
        /// FixMe. 모든 액션에 대한 랜더 단계에서 아바타 주소의 상태를 얻어 오고 있음.
        /// 모든 액션 생성 단계에서 각각의 변경점을 업데이트 하는 방향으로 수정해볼 필요성 있음.
        /// CreateNovice와 HackAndSlash 액션의 처리를 개선해서 테스트해 볼 예정.
        /// 시작 전에 양님에게 문의!
        /// </summary>
        public void SubscribeAvatarUpdates()
        {
            if (Avatar != null)
            {
                DidAvatarLoaded?.Invoke(this, Avatar);
            }

            ActionBase.EveryRender(AvatarAddress).ObserveOnMainThread().Subscribe(eval =>
            {
                var ctx = (Context) eval.OutputStates.GetState(AvatarAddress);
                if (!(ctx?.avatar is null))
                {
                    ReceiveAction(ctx);
                }
            }).AddTo(this);
        }

        /// <summary>
        /// LoginDetail 이라는 UI 클래스에서 불리고 있는 것을 게임 초기화 로직으로 옮길 필요성 있음.
        /// </summary>
        public void InitAgent()
        {
#if UNITY_EDITOR
            var o = new CommandLineOptions();
#else
            var o = CommnadLineParser.GetCommandLineOptions() ?? new CommandLineOptions();
#endif
            PrivateKey privateKey = null;
            var key = string.Format(PrivateKeyFormat, "agent");
            var privateKeyHex = o.PrivateKey ?? PlayerPrefs.GetString(key, "");

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
            var peers = o.Peers.Any()
                ? o.Peers.Select(LoadPeer)
                : LoadConfigLines(PeersFileName).Select(LoadPeer);
            var iceServers = LoadIceServers();
            string host = o.Host;
#endif
            int? port = o.Port;

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

            if (!o.NoMiner)
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

            Shop = GetState(shopAddress) as Shop ?? new Shop();
        }

        /// <summary>
        /// Game.Start 에서 불리고 있는 형태이니 위의 InitAgent도 이 함수가 불리는 쪽으로 모아두면 좋겠음. 
        /// </summary>
        public void StartSystemCoroutines()
        {
            StartNullableCoroutine(_txProcessor);
            StartNullableCoroutine(_actionRetryer);
            StartNullableCoroutine(_swarmRunner);
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
            
            Debug.Log($"Agent Address: 0x{agent.AgentAddress.ToHex()}");
            Debug.Log($"Avatar Address: 0x{agent.AvatarAddress.ToHex()}");
        }
        
        public void CreateNovice(string nickName)
        {
            var action = new CreateNovice
            {
                name = nickName,
                avatarAddress = AvatarAddress,
            };
            ProcessAction(action);
        }

        public IObservable<ActionBase.ActionEvaluation<HackAndSlash>> HackAndSlash(
            List<Equipment> equipments,
            List<Food> foods,
            int stage)
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
            return Action.HackAndSlash.EveryRender<HackAndSlash>().SkipWhile(
                eval => !eval.Action.Id.Equals(action.Id)
            ).Take(1).Last();  // Last() is for completion
        }
        
        public IObservable<ActionBase.ActionEvaluation<Combination>> Combination(
            List<UI.Model.CountEditableItem> materials)
        {
            AnalyticsManager.instance.OnEvent(AnalyticsManager.EventName.ActionCombination);
            
            var action = new Combination();
            materials.ForEach(m => action.Materials.Add(new Combination.ItemModel(m)));
            ProcessAction(action);

            return ActionBase.EveryRender<Combination>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread();
        }

        public IObservable<Sell.ResultModel> Sell(int itemId, int count, decimal price)
        {
            var action = new Sell {itemId = itemId, count = count, price = price};
            ProcessAction(action);

            return ActionBase.EveryRender<Sell>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Select(eval =>
                {
                    var context = (Context) eval.OutputStates.GetState(eval.InputContext.Signer);
                    var result = context.GetGameActionResult<Sell.ResultModel>();
                    
                    // 인벤토리에서 빼기.
                    // ToDo. SubscribeAvatarUpdates()에서 동기화 중. 분리할 예정.
//                    Avatar.RemoveEquipmentItemFromItems(result.shopItem.item.Data.id, result.shopItem.count);
                    
                    // 상점에 넣기.
                    Shop.Register(AvatarAddress, result.shopItem);

                    return result;
                }); // Last() is for completion
        }
        
        public IObservable<SellCancelation.ResultModel> SellCancelation(Address owner, Guid productId)
        {
            var action = new SellCancelation {owner = owner, productId = productId};
            ProcessAction(action);

            return ActionBase.EveryRender<SellCancelation>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Select(eval =>
                {
                    var context = (Context) eval.OutputStates.GetState(eval.InputContext.Signer);
                    var result = context.GetGameActionResult<SellCancelation.ResultModel>();
                    
                    // 상점에서 빼기.
                    var shopItem = Shop.Unregister(result.owner, result.shopItem.productId);
                    // 인벤토리에 넣기.
                    // ToDo. SubscribeAvatarUpdates()에서 동기화 중. 분리할 예정.
//                    Avatar.AddEquipmentItemToItems(shopItem.item.Data.id, shopItem.count);

                    return result;
                }); // Last() is for completion
        }
        
        public IObservable<Buy.ResultModel> Buy(Address owner, Guid productId)
        {
            var action = new Buy {owner = owner, productId = productId};
            ProcessAction(action);

            return ActionBase.EveryRender<Buy>()
                .Where(eval => eval.Action.Id.Equals(action.Id))
                .Take(1)
                .Last()
                .ObserveOnMainThread()
                .Select(eval =>
                {
                    var context = (Context) eval.OutputStates.GetState(eval.InputContext.Signer);
                    var result = context.GetGameActionResult<Buy.ResultModel>();
                    
                    // 상점에서 빼기.
                    var shopItem = Shop.Unregister(result.owner, result.shopItem.productId);
                    // 인벤토리에 넣기.
                    // ToDo. SubscribeAvatarUpdates()에서 동기화 중. 분리할 예정.
//                    Avatar.AddEquipmentItemToItems(shopItem.item.Data.id, shopItem.count);

                    return result;
                }); // Last() is for completion
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

        private void ProcessAction(GameAction action)
        {
            action.Id = action.Id.Equals(default(Guid)) ? Guid.NewGuid() : action.Id;
            agent.QueuedActions.Enqueue(action);
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

        private Peer LoadPeer(string peerInfo)
        {
            string[] tokens = peerInfo.Split(',');
            var pubKey = new PublicKey(ByteUtil.ParseHex(tokens[0]));
            string host = tokens[1];
            int port = int.Parse(tokens[2]);
            int version = int.Parse(tokens[3]);

            return new Peer(pubKey, new DnsEndPoint(host, port), version);
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
        
        private Coroutine StartNullableCoroutine(IEnumerator routine)
        {
            if (ReferenceEquals(routine, null))
            {
                return null;
            }

            return StartCoroutine(routine);
        }
    }
}
