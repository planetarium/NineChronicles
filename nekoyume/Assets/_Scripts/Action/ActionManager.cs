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
using Nekoyume.Game.Item;
using Nekoyume.Helper;
using Nekoyume.Model;
using Nekoyume.State;
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
#if UNITY_EDITOR
        private const string AgentStoreDirName = "planetarium_dev";
#else
        private const string AgentStoreDirName = "planetarium";
#endif
        public const string PeersFileName = "peers.dat";
        public const string IceServersFileName = "ice_servers.dat";
        public const string ChainIdKey = "chain_id";
        
        private string _saveFilePath;
        private Agent _agent;
        
        private IEnumerator _miner;
        private IEnumerator _txProcessor;
        private IEnumerator _swarmRunner;
        private IEnumerator _actionRetryer;

        private IDisposable _rewardGoldActionDisposable;

        public Model.Avatar Avatar => _agent.Avatar;
        public BattleLog battleLog;
        public ShopState ShopState { get; private set; }
        
        #region Mono

        protected override void Awake()
        {
            base.Awake();
            
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        }
        
        protected override void OnDestroy() 
        {
            if (_agent != null)
            {
                PlayerPrefs.SetString(ChainIdKey, _agent.ChainId.ToString());
                _agent.Dispose();
            }
            
            NetMQConfig.Cleanup(false);
            
            base.OnDestroy();
        }
        
        #endregion

        public object GetState(Address address)
        {
            return _agent.GetState(address);
        }

        public void SubscribeAvatarUpdates()
        {
            if (ReferenceEquals(_agent, null))
            {
                throw new NullReferenceException("_agent is null.");
            }
            
            _agent.SubscribeAvatarUpdates();
        }

        #region Init

        public void InitAgent()
        {
            if (!ReferenceEquals(_agent, null))
            {
                return;
            }
            
#if UNITY_EDITOR
            var o = new CommandLineOptions();
#else
            var o = CommnadLineParser.GetCommandLineOptions() ?? new CommandLineOptions();
#endif
            PrivateKey privateKey;
            var key = string.Format(Agent.PrivateKeyFormat, "agent");
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
            var host = "127.0.0.1";
#else
            var peers = o.Peers.Any()
                ? o.Peers.Select(LoadPeer)
                : LoadConfigLines(PeersFileName).Select(LoadPeer);
            var iceServers = LoadIceServers();
            string host = o.Host;
#endif
            int? port = o.Port;

            _agent = new Agent(
                agentPrivateKey: privateKey, 
                path: storePath, 
                chainId: chainId, 
                peers: peers, 
                iceServers: iceServers,
                host: host,
                port: port
            );
            _agent.PreloadStarted += (_, __) =>
            {
                UI.Widget.Find<UI.LoadingScreen>()?.Show();
            };
            _agent.PreloadEnded += (_, __) =>
            {
                StartNullableCoroutine(_miner);
                ShopState = GetState(AddressBook.Shop) as ShopState ?? new ShopState();
                UI.Widget.Find<UI.LoadingScreen>()?.Close();
            };
            _miner = o.NoMiner ? null : _agent.CoMiner();
            
            StartSystemCoroutines(_agent);
        }
        
        public void InitAvatar(int index)
        {
            if (ReferenceEquals(_agent, null))
            {
                throw new NullReferenceException("_agent is null.");
            }

            _agent.InitAvatar(index);
        }

        #endregion
        
        #region Actions
        
        public void CreateNovice(string nickName)
        {
            var action = new CreateNovice
            {
                name = nickName,
                avatarAddress = AddressBook.Avatar.Value,
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
                    var context = (AvatarState) eval.OutputStates.GetState(eval.InputContext.Signer);
                    var result = context.GetGameActionResult<Sell.ResultModel>();
                    
                    // 인벤토리에서 빼기.
                    // ToDo. SubscribeAvatarUpdates()에서 동기화 중. 분리할 예정.
//                    Avatar.RemoveEquipmentItemFromItems(result.shopItem.item.Data.id, result.shopItem.count);
                    
                    // 상점에 넣기.
                    ShopState.Register(AddressBook.Avatar.Value, result.shopItem);

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
                    var context = (AvatarState) eval.OutputStates.GetState(eval.InputContext.Signer);
                    var result = context.GetGameActionResult<SellCancelation.ResultModel>();
                    
                    // 상점에서 빼기.
                    var shopItem = ShopState.Unregister(result.owner, result.shopItem.productId);
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
                    var context = (AvatarState) eval.OutputStates.GetState(eval.InputContext.Signer);
                    var result = context.GetGameActionResult<Buy.ResultModel>();
                    
                    // 상점에서 빼기.
                    var shopItem = ShopState.Unregister(result.owner, result.shopItem.productId);
                    // 인벤토리에 넣기.
                    // ToDo. SubscribeAvatarUpdates()에서 동기화 중. 분리할 예정.
//                    Avatar.AddEquipmentItemToItems(shopItem.item.Data.id, shopItem.count);

                    return result;
                }); // Last() is for completion
        }
        
        #endregion

#if !UNITY_EDITOR
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

        private IEnumerable<IceServer> LoadIceServers()
        {
            foreach (string line in LoadConfigLines(IceServersFileName)) 
            {
                var uri = new Uri(line);
                string[] userInfo = uri.UserInfo.Split(':');

                yield return new IceServer(new[] {uri}, userInfo[0], userInfo[1]);
            }
        }
#endif
        
        private void StartSystemCoroutines(Agent agent)
        {
            _txProcessor = agent.CoTxProcessor();
            _swarmRunner = agent.CoSwarmRunner();
            _actionRetryer = agent.CoActionRetryer();
            
            StartNullableCoroutine(_txProcessor);
            StartNullableCoroutine(_actionRetryer);
            StartNullableCoroutine(_swarmRunner);
        }
        
        private Coroutine StartNullableCoroutine(IEnumerator routine)
        {
            return ReferenceEquals(routine, null) ? null : StartCoroutine(routine);
        }
        
        private void ProcessAction(GameAction action)
        {
            action.Id = action.Id.Equals(default(Guid)) ? Guid.NewGuid() : action.Id;
            _agent.QueuedActions.Enqueue(action);
        }
    }
}
