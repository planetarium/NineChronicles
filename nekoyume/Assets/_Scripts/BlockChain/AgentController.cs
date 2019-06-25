using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
#if !UNITY_EDITOR
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
#endif
using Libplanet;
using Libplanet.Crypto;
using Libplanet.Net;
using Nekoyume.Helper;
using Nekoyume.State;
using NetMQ;
using UnityEngine;

namespace Nekoyume.BlockChain
{
    /// <summary>
    /// Agent를 구동시킨다.
    /// </summary>
    public class AgentController : MonoSingleton<AgentController>
    {
        public const string PrivateKeyFormat = "private_key_{0}";
#if UNITY_EDITOR
        private const string AgentStoreDirName = "planetarium_dev";
        private const string DefaultHost = "127.0.0.1";
#else
        private const string PeersFileName = "peers.dat";
        private const string IceServersFileName = "ice_servers.dat";
        private const string AgentStoreDirName = "planetarium";
#endif
        private const string ChainIdKey = "chain_id";
        
        private static readonly string StorePath = Path.Combine(Application.persistentDataPath, AgentStoreDirName);
        
        public static Agent Agent { get; private set; }
        
        private static IEnumerator _miner;
        private static IEnumerator _txProcessor;
        private static IEnumerator _swarmRunner;
        
        public static void Initialize(Action<bool> callback)
        {
            if (!ReferenceEquals(Agent, null))
            {
                return;
            }   
            
            instance.InitAgent(callback);
        }

        private void InitAgent(Action<bool> callback)
        {
            var options = GetOptions();
            var privateKey = GetPrivateKey(options);
            var chainId = GetChainId();
            var peers = GetPeers(options);
            var iceServers = GetIceServers();
            var host = GetHost(options);
            int? port = options.Port;

            Agent = new Agent(
                privateKey: privateKey, 
                path: StorePath, 
                chainId: chainId, 
                peers: peers, 
                iceServers: iceServers,
                host: host,
                port: port
            );
            Agent.PreloadStarted += (_, __) =>
            {
                UI.Widget.Find<UI.LoadingScreen>()?.Show();
            };
            Agent.PreloadEnded += (_, __) =>
            {
                // 에이전트의 준비단계가 끝나면 에이전트의 상태를 한 번 동기화 한다.
                States.Instance.agentState.Value = (AgentState) Agent.GetState(Agent.Address) ??
                                     new AgentState(Agent.Address);
                // 에이전트에 포함된 모든 아바타의 상태를 한 번씩 동기화 한다.
                foreach (var pair in States.Instance.agentState.Value.avatarAddresses)
                {
                    var avatarState = (AvatarState) Agent.GetState(pair.Value);
                    States.Instance.avatarStates.Add(pair.Key, avatarState);
                }
                // 랭킹의 상태를 한 번 동기화 한다.
                States.Instance.rankingState.Value = (RankingState) Agent.GetState(RankingState.Address) ?? new RankingState();
                // 상점의 상태를 한 번 동기화 한다.
                States.Instance.shopState.Value = (ShopState) Agent.GetState(ShopState.Address) ?? new ShopState();
                // 그리고 모든 액션에 대한 랜더를 핸들링하기 시작한다.
                ActionRenderHandler.Instance.Start();
                // 그리고 마이닝을 시작한다.
                StartNullableCoroutine(_miner);
                callback(true);
            };
            _miner = options.NoMiner ? null : Agent.CoMiner();
            
            StartSystemCoroutines(Agent);
        }
        
        private static CommandLineOptions GetOptions()
        {
#if UNITY_EDITOR
            return new CommandLineOptions();
#else
            return CommnadLineParser.GetCommandLineOptions() ?? new CommandLineOptions();
#endif
        }

        private static PrivateKey GetPrivateKey(CommandLineOptions options)
        {
            PrivateKey privateKey;
            var key = string.Format(PrivateKeyFormat, "agent");
            var privateKeyHex = options.PrivateKey ?? PlayerPrefs.GetString(key, "");

            if (string.IsNullOrEmpty(privateKeyHex))
            {
                privateKey = new PrivateKey();
                PlayerPrefs.SetString(key, ByteUtil.Hex(privateKey.ByteArray));
            }
            else
            {
                privateKey = new PrivateKey(ByteUtil.ParseHex(privateKeyHex));
            }

            return privateKey;
        }

        private static Guid GetChainId()
        {
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

            return chainId;
        }

        private static IEnumerable<Peer> GetPeers(CommandLineOptions options)
        {
#if UNITY_EDITOR
            return new Peer[]{ };
#else
            return options.Peers?.Any() ?? false
                ? options.Peers.Select(LoadPeer)
                : LoadConfigLines(PeersFileName).Select(LoadPeer);
#endif
        }

        private static IEnumerable<IceServer> GetIceServers()
        {
#if UNITY_EDITOR
            return null;
#else
            return LoadIceServers();
#endif
        }

        private static string GetHost(CommandLineOptions options)
        {
#if UNITY_EDITOR
            return DefaultHost;
#else
            return options.Host;
#endif
        }
        
#if !UNITY_EDITOR
        private static Peer LoadPeer(string peerInfo)
        {
            string[] tokens = peerInfo.Split(',');
            var pubKey = new PublicKey(ByteUtil.ParseHex(tokens[0]));
            string host = tokens[1];
            int port = int.Parse(tokens[2]);

            return new Peer(pubKey, new DnsEndPoint(host, port));
        }

        private static IEnumerable<string> LoadConfigLines(string fileName)
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

        private static IEnumerable<IceServer> LoadIceServers()
        {
            foreach (string line in LoadConfigLines(IceServersFileName)) 
            {
                var uri = new Uri(line);
                string[] userInfo = uri.UserInfo.Split(':');

                yield return new IceServer(new[] {uri}, userInfo[0], userInfo[1]);
            }
        }
#endif

        #region Mono

        protected override void OnDestroy()
        {
            ActionRenderHandler.Instance.Stop();
            if (Agent != null)
            {
                PlayerPrefs.SetString(ChainIdKey, Agent.ChainId.ToString());
                Agent.Dispose();
            }            
            NetMQConfig.Cleanup(false);
            
            base.OnDestroy();
        }

        #endregion
        
        private void StartSystemCoroutines(Agent agent)
        {
            _txProcessor = agent.CoTxProcessor();
            _swarmRunner = agent.CoSwarmRunner();
            
            StartNullableCoroutine(_txProcessor);
            StartNullableCoroutine(_swarmRunner);
        }
        
        private Coroutine StartNullableCoroutine(IEnumerator routine)
        {
            return ReferenceEquals(routine, null) ? null : StartCoroutine(routine);
        }
    }
}
