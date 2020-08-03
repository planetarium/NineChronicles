using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cocona;
using Libplanet.KeyStore;
using Libplanet.Standalone.Hosting;
using Microsoft.Extensions.Hosting;
using NineChronicles.Standalone.Properties;
using Sentry;
using Serilog;
using NineChroniclesActionType = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace NineChronicles.Standalone.Executable
{
    public class Program : CoconaLiteConsoleAppBase
    {
        const string SentryDsn = "https://ceac97d4a7d34e7b95e4c445b9b5669e@o195672.ingest.sentry.io/5287621";

        static async Task Main(string[] args)
        {
#if SENTRY || ! DEBUG
            using var _ = SentrySdk.Init(ConfigureSentryOptions);
#endif
            await CoconaLiteApp.RunAsync<Program>(args);
        }

        static void ConfigureSentryOptions(SentryOptions o)
        {
            o.SendDefaultPii = true;
            o.Dsn = new Dsn(SentryDsn);
            // TODO: o.Release 설정하면 좋을 것 같은데 빌드 버전 체계가 아직 없어서 어떻게 해야 할 지...
            // https://docs.sentry.io/workflow/releases/?platform=csharp
#if DEBUG
            o.Debug = true;
#endif
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
#if SENTRY || ! DEBUG
            try
            {
#endif
            // Setup logger.
            var loggerConf = new LoggerConfiguration()
                .WriteTo.Console()
                .MinimumLevel.Debug();
#if SENTRY || ! DEBUG
            loggerConf = loggerConf
                .WriteTo.Sentry(o =>
                {
                    o.InitializeSdk = false;
                });
#endif
            Log.Logger = loggerConf.CreateLogger();

            var tasks = new List<Task>();
            try
            {
                IHostBuilder graphQLHostBuilder = Host.CreateDefaultBuilder();

                var standaloneContext = new StandaloneContext
                {
                    KeyStore = Web3KeyStore.DefaultKeyStore,
                };

                if (graphQLServer)
                {
                    var graphQLNodeServiceProperties = new GraphQLNodeServiceProperties
                    {
                        GraphQLServer = graphQLServer,
                        GraphQLListenHost = graphQLHost,
                        GraphQLListenPort = graphQLPort,
                    };


                    var graphQLService = new GraphQLService(graphQLNodeServiceProperties);
                    graphQLHostBuilder =
                        graphQLService.Configure(graphQLHostBuilder, standaloneContext);
                    tasks.Add(graphQLHostBuilder.RunConsoleAsync(Context.CancellationToken));

                    await WaitForGraphQLService(graphQLNodeServiceProperties,
                        Context.CancellationToken);
                }

                if (appProtocolVersionToken is null)
                {
                    throw new CommandExitedException(
                        "--app-protocol-version must be present.",
                        -1
                    );
                }

                if (genesisBlockPath is null)
                {
                    throw new CommandExitedException(
                        "--genesis-block-path must be present.",
                        -1
                    );
                }

                RpcNodeServiceProperties? rpcProperties = null;
                var properties = NineChroniclesNodeServiceProperties
                    .GenerateLibplanetNodeServiceProperties(
                        appProtocolVersionToken,
                        genesisBlockPath,
                        host,
                        port,
                        minimumDifficulty,
                        privateKeyString,
                        storeType,
                        storePath,
                        100,
                        iceServerStrings,
                        peerStrings,
                        noTrustedStateValidators,
                        trustedAppProtocolVersionSigners,
                        noMiner);
                if (rpcServer)
                {
                    rpcProperties = NineChroniclesNodeServiceProperties
                        .GenerateRpcNodeServiceProperties(rpcListenHost, rpcListenPort);
                    properties.Render = true;
                }

                var nineChroniclesProperties = new NineChroniclesNodeServiceProperties()
                {
                    Rpc = rpcProperties,
                    Libplanet = properties
                };

                NineChroniclesNodeService nineChroniclesNodeService =
                    StandaloneServices.CreateHeadless(nineChroniclesProperties, standaloneContext);
                standaloneContext.NineChroniclesNodeService = nineChroniclesNodeService;

                bool startNineChroniclesNodeService = !graphQLServer
                                                      || !(peerStrings is null)
                                                      || !(host is null || port is null);

                if (startNineChroniclesNodeService)
                {
                    if (!properties.NoMiner)
                    {
                        nineChroniclesNodeService.PrivateKey = properties.PrivateKey;
                        nineChroniclesNodeService.StartMining();
                    }

                    IHostBuilder nineChroniclesNodeHostBuilder = Host.CreateDefaultBuilder();
                    nineChroniclesNodeHostBuilder =
                        nineChroniclesNodeService.Configure(nineChroniclesNodeHostBuilder);
                    tasks.Add(
                        nineChroniclesNodeHostBuilder.RunConsoleAsync(Context.CancellationToken));
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Unexpected exception occurred during Run. {e}", e);
            }
            finally
            {
                await Task.WhenAll(tasks);
            }

#if SENTRY || ! DEBUG
            }
            catch (CommandExitedException)
            {
                throw;
            }
            catch (Exception exceptionToCapture)
            {
                SentrySdk.CaptureException(exceptionToCapture);
                throw;
            }
#endif
        }

        private async Task WaitForGraphQLService(
            GraphQLNodeServiceProperties properties,
            CancellationToken cancellationToken)
        {
            using var httpClient = new HttpClient();
            while (!cancellationToken.IsCancellationRequested)
            {
                Log.Debug("Trying to check GraphQL server started...");
                try
                {
                    await httpClient.GetAsync($"http://{properties.GraphQLListenHost}:{properties.GraphQLListenPort}/health-check", cancellationToken);
                    break;
                }
                catch (HttpRequestException e)
                {
                    Log.Error(e, "An exception occurred during connecting to GraphQL server. {e}", e);
                }

                await Task.Delay(1000, cancellationToken);
            }
        }
    }
}
