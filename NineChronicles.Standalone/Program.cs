using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Cocona;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Crypto;
using Libplanet.Net;
using Libplanet.Standalone.Hosting;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Serilog;

using NineChroniclesActionType = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>; 

namespace NineChronicles.Standalone
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            await CoconaLiteApp.RunAsync<Program>(args);
        }

        [Command(Description = "Run standalone application with options.")]
        public async Task Run(
            [Option('V')]
            int appProtocolVersion,
            [Option('G')]
            string genesisBlockPath, 
            bool noMiner,
            [Option('H')]
            string host = null,
            [Option('P')]
            ushort? port = null,
            [Option("private-key")]
            string privateKeyString = null,
            string storePath = null,
            [Option("ice-server", new [] { 'I', })]
            string[] iceServerStrings = null,
            [Option("peer")]
            string[] peerStrings = null
        )
        {
            // Setup logger.
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .MinimumLevel.Debug().CreateLogger();

            var privateKey = string.IsNullOrEmpty(privateKeyString)
                ? new PrivateKey()
                : new PrivateKey(ByteUtil.ParseHex(privateKeyString));

            peerStrings ??= Array.Empty<string>();
            iceServerStrings ??= Array.Empty<string>();

            LibplanetNodeServiceProperties properties = new LibplanetNodeServiceProperties
            {
                Host = host,
                Port = port,
                AppProtocolVersion = appProtocolVersion,
                GenesisBlockPath = genesisBlockPath,
                NoMiner = noMiner,
                PrivateKey = privateKey,
                IceServers = iceServerStrings.Select(LoadIceServer),
                Peers = peerStrings.Select(LoadPeer).ToArray(),
                StorePath = storePath,
            };

            var blockAction = new RewardGold { Gold = 1 };
            var blockPolicy =
                new BlockPolicy<PolymorphicAction<ActionBase>>(blockAction, TimeSpan.FromSeconds(10), 100000, 2048);
            Action<BlockChain<NineChroniclesActionType>, Swarm<NineChroniclesActionType>, PrivateKey> minerLoopAction =
                (chain, swarm, privateKey) =>
                {
                    var miner = new Miner(chain, swarm, privateKey);
                    while (true)
                    {
                        Log.Debug("Miner called.");
                        try
                        {
                            miner.MineBlock();
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Exception occurred.");
                        }
                    }
                };

            var service = new LibplanetNodeService<NineChroniclesActionType>(properties, blockPolicy, minerLoopAction);
            var cancellationToken = new CancellationToken();
            await service.StartAsync(cancellationToken);
        }

        private static IceServer LoadIceServer(string iceServerInfo)
        {
            var uri = new Uri(iceServerInfo);
            string[] userInfo = uri.UserInfo.Split(':');
        
            return new IceServer(new[] { uri }, userInfo[0], userInfo[1]);
        }
        
        private static BoundPeer LoadPeer(string peerInfo)
        {
            var tokens = peerInfo.Split(',');
            var pubKey = new PublicKey(ByteUtil.ParseHex(tokens[0]));
            var host = tokens[1];
            var port = int.Parse(tokens[2]);
        
            return new BoundPeer(pubKey, new DnsEndPoint(host, port), 0);
        }
    }
}
