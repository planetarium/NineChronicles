using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Cocona;
using Grpc.Core;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Crypto;
using Libplanet.Net;
using Libplanet.Standalone.Hosting;
using MagicOnion.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            string storeType = null,
            string storePath = null,
            [Option("ice-server", new [] { 'I', })]
            string[] iceServerStrings = null,
            [Option("peer")]
            string[] peerStrings = null,
            bool rpc = false,
            [Option("rpc-host")]
            string rpcHost = null,
            [Option("rpc-port")]
            int? rpcPort = null
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
                Peers = peerStrings.Select(LoadPeer),
                StoreType = storeType,
                StorePath = storePath,
            };

            // BlockPolicy shared through Lib9c.
            IBlockPolicy<PolymorphicAction<ActionBase>> blockPolicy = BlockPolicy.GetPolicy();
            async Task minerLoopAction(
                BlockChain<NineChroniclesActionType> chain, 
                Swarm<NineChroniclesActionType> swarm, 
                PrivateKey privateKey, 
                CancellationToken cancellationToken)
            {
                var miner = new Miner(chain, swarm, privateKey);
                while (true)
                {
                    Log.Debug("Miner called.");
                    try
                    {
                        await miner.MineBlockAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Exception occurred.");
                    }
                }
            }

            var nodeService = new LibplanetNodeService<NineChroniclesActionType>(
                properties, 
                blockPolicy, 
                minerLoopAction
            );

            IHostBuilder hostBuilder = Host.CreateDefaultBuilder();

            if (rpc)
            {
                if (string.IsNullOrEmpty(rpcHost))
                {
                    throw new CommandExitedException(
                        "--rpc-host must be required when --rpc had been set.",
                        -1
                    );
                }
                else if (!(rpcPort is int rpcPortValue))
                {
                    throw new CommandExitedException(
                        "--rpc-port must be required when --rpc had been set.",
                        -1
                    );
                }
                else
                {
                    hostBuilder = hostBuilder
                        .UseMagicOnion(
                            new ServerPort(rpcHost, rpcPortValue, ServerCredentials.Insecure)
                        )
                        .ConfigureServices((ctx, services) =>
                        {
                            services.AddHostedService(provider => new ActionEvaluationPublisher(
                                nodeService.BlockChain,
                                rpcHost,
                                rpcPortValue
                            ));
                        });
                }
            }

            await hostBuilder.ConfigureServices((ctx, services) =>
            {
                services.AddHostedService(provider => nodeService);
                services.AddSingleton(provider => nodeService.BlockChain);
            }).RunConsoleAsync();
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
