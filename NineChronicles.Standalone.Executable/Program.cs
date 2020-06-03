using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Cocona;
using Libplanet;
using Libplanet.Crypto;
using Libplanet.Net;
using Libplanet.Standalone.Hosting;
using Microsoft.Extensions.Hosting;
using NineChronicles.Standalone.Properties;
using Serilog;

namespace NineChronicles.Standalone.Executable
{
    public class Program : CoconaLiteConsoleAppBase
    {
        static async Task Main(string[] args)
        {
            await CoconaLiteApp.RunAsync<Program>(args);
        }

        [Command(Description = "Run standalone application with options.")]
        public async Task Run(
            bool noMiner = false,
            [Option("app-protocol-version", new[] { 'V' }, Description = "App protocol version token")]
            string appProtocolVersionToken = null,
            [Option('G')]
            string genesisBlockPath = null,
            [Option('H')]
            string host = null,
            [Option('P')]
            ushort? port = null,
            [Option('D')]
            int minimumDifficulty = 5000000,
            [Option("private-key")]
            string privateKeyString = null,
            string storeType = null,
            string storePath = null,
            [Option("ice-server", new [] { 'I', })]
            string[] iceServerStrings = null,
            [Option("peer")]
            string[] peerStrings = null,
            [Option("no-trusted-state-validators")]
            bool noTrustedStateValidators = false,
            [Option("trusted-app-protocol-version-signer", new[] { 'T' },
                    Description = "Trustworthy signers who claim new app protocol versions")]
            string[] trustedAppProtocolVersionSigners = null,
            bool rpcServer = false,
            string rpcListenHost = "0.0.0.0",
            int? rpcListenPort = null,
            [Option("graphql-server")]
            bool graphQLServer = false,
            [Option("graphql-host")]
            string graphQLHost = "0.0.0.0",
            [Option("graphql-port")]
            int? graphQLPort = null
        )
        {
            // Setup logger.
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .MinimumLevel.Debug().CreateLogger();

            IHostBuilder hostBuilder = Host.CreateDefaultBuilder();

            // GraphQL 서비스를 실행할 때는 런처를 통해 실행 된 GraphQL을 이용하여 노드 서비스가 실행되게 설계되었습니다.
            // 따라서 graphQLServer가 true라면, 노드 서비스가 실행되지 않는 것이 의도된 사항입니다.
            if (graphQLServer)
            {
                var graphQLNodeServiceProperties = new GraphQLNodeServiceProperties
                {
                    GraphQLServer = graphQLServer,
                    GraphQLListenHost = graphQLHost,
                    GraphQLListenPort = graphQLPort,
                };

                await StandaloneServices.RunGraphQLAsync(
                    graphQLNodeServiceProperties,
                    hostBuilder,
                    Context.CancellationToken);
            }
            else
            {
                if (appProtocolVersionToken is null)
                {
                    throw new CommandExitedException(
                        "--app-protocol-version must be required.",
                        -1
                    );
                }

                if (genesisBlockPath is null)
                {
                    throw new CommandExitedException(
                        "--genesis-block-path must be required.",
                        -1
                    );
                }

                var privateKey = string.IsNullOrEmpty(privateKeyString)
                    ? new PrivateKey()
                    : new PrivateKey(ByteUtil.ParseHex(privateKeyString));

                peerStrings ??= Array.Empty<string>();
                iceServerStrings ??= Array.Empty<string>();

                var iceServers = iceServerStrings.Select(LoadIceServer).ToImmutableArray();
                var peers = peerStrings.Select(LoadPeer).ToImmutableArray();

                IImmutableSet<Address> trustedStateValidators;
                if (noTrustedStateValidators)
                {
                    trustedStateValidators = ImmutableHashSet<Address>.Empty;
                }
                else
                {
                    trustedStateValidators = peers.Select(p => p.Address).ToImmutableHashSet();
                }

                var properties = new LibplanetNodeServiceProperties
                {
                    Host = host,
                    Port = port,
                    AppProtocolVersion = AppProtocolVersion.FromToken(appProtocolVersionToken),
                    TrustedAppProtocolVersionSigners = trustedAppProtocolVersionSigners
                        ?.Select(s => new PublicKey(ByteUtil.ParseHex(s)))
                        ?.ToHashSet(),
                    GenesisBlockPath = genesisBlockPath,
                    NoMiner = noMiner,
                    PrivateKey = privateKey,
                    IceServers = iceServers,
                    Peers = peers,
                    TrustedStateValidators = trustedStateValidators,
                    StoreType = storeType,
                    StorePath = storePath,
                    StoreStatesCacheSize = 5000,
                    MinimumDifficulty = minimumDifficulty,
                };

                var rpcProperties = new RpcNodeServiceProperties
                {
                    RpcServer = rpcServer
                };

                var nineChroniclesProperties = new NineChroniclesNodeServiceProperties
                {
                    Rpc = rpcProperties,
                    Libplanet = properties
                };

                if (rpcServer)
                {
                    if (string.IsNullOrEmpty(rpcListenHost))
                    {
                        throw new CommandExitedException(
                            "--rpc-listen-host must be required when --rpc-server is present.",
                            -1
                        );
                    }

                    if (!(rpcListenPort is int rpcPortValue))
                    {
                        throw new CommandExitedException(
                            "--rpc-listen-port must be required when --rpc-server is present.",
                            -1
                        );
                    }

                    rpcProperties.RpcListenHost = rpcListenHost;
                    rpcProperties.RpcListenPort = rpcPortValue;
                }

                await StandaloneServices.RunHeadlessAsync(
                    nineChroniclesProperties,
                    hostBuilder,
                    Context.CancellationToken);
            }
        }

        private static IceServer LoadIceServer(string iceServerInfo)
        {
            try
            {
                var uri = new Uri(iceServerInfo);
                string[] userInfo = uri.UserInfo.Split(':');

                return new IceServer(new[] {uri}, userInfo[0], userInfo[1]);
            }
            catch (Exception e)
            {
                throw new CommandExitedException(
                    $"--ice-server '{iceServerInfo}' seems invalid.\n" +
                    $"{e.GetType()} {e.Message}\n" +
                    $"{e.StackTrace}",
                    -1);
            }
        }

        private static BoundPeer LoadPeer(string peerInfo)
        {
            var tokens = peerInfo.Split(',');
            if (tokens.Length != 3)
            {
                throw new CommandExitedException(
                    $"--peer '{peerInfo}', should have format <pubkey>,<host>,<port>",
                    -1);
            }

            if (!(tokens[0].Length == 130 || tokens[0].Length == 66))
            {
                throw new CommandExitedException(
                    $"--peer '{peerInfo}', a length of public key must be 130 or 66 in hexadecimal," +
                    $" but the length of given public key '{tokens[0]}' doesn't.",
                    -1);
            }

            try
            {
                var pubKey = new PublicKey(ByteUtil.ParseHex(tokens[0]));
                var host = tokens[1];
                var port = int.Parse(tokens[2]);

                // FIXME: It might be better to make Peer.AppProtocolVersion property nullable...
                return new BoundPeer(pubKey, new DnsEndPoint(host, port), default(AppProtocolVersion));
            }
            catch (Exception e)
            {
                throw new CommandExitedException(
                    $"--peer '{peerInfo}' seems invalid.\n" +
                    $"{e.GetType()} {e.Message}\n" +
                    $"{e.StackTrace}",
                    -1);
            }
        }
    }
}
