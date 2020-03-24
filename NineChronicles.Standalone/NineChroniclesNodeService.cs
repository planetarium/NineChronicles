using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Bencodex.Types;
using Grpc.Core;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Crypto;
using Libplanet.Net;
using Libplanet.Standalone.Hosting;
using Libplanet.Tx;
using MagicOnion.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nito.AsyncEx;
using Serilog;

using NineChroniclesActionType = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace NineChronicles.Standalone
{
    public class NineChroniclesNodeService
    {
        private LibplanetNodeService<NineChroniclesActionType> NodeService { get; set; }

        private LibplanetNodeServiceProperties Properties { get; }

        private RpcNodeServiceProperties RpcProperties { get; }

        private Func<WhiteListSheet> GetWhiteListSheet { get; set; }

        public AsyncAutoResetEvent BootstrapEnded => NodeService.BootstrapEnded;

        public AsyncAutoResetEvent PreloadEnded => NodeService.PreloadEnded;

        public NineChroniclesNodeService(
            LibplanetNodeServiceProperties properties,
            RpcNodeServiceProperties rpcNodeServiceProperties)
        {
            Properties = properties;
            RpcProperties = rpcNodeServiceProperties;

            // BlockPolicy shared through Lib9c.
            IBlockPolicy<PolymorphicAction<ActionBase>> blockPolicy = BlockPolicy.GetPolicy(
                properties.MinimumDifficulty,
                IsSignerAuthorized
            );
            async Task minerLoopAction(
                BlockChain<NineChroniclesActionType> chain,
                Swarm<NineChroniclesActionType> swarm,
                PrivateKey privateKey,
                CancellationToken cancellationToken)
            {
                var miner = new Miner(chain, swarm, privateKey);
                while (!cancellationToken.IsCancellationRequested)
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

            NodeService = new LibplanetNodeService<NineChroniclesActionType>(
                Properties,
                blockPolicy,
                minerLoopAction
            );

            GetWhiteListSheet = () =>
            {
                var state = NodeService.BlockChain?.GetState(TableSheetsState.Address);
                if (state is null)
                {
                    return null;
                }

                var tableSheetsState = new TableSheetsState((Dictionary)state);
                return TableSheets.FromTableSheetsState(tableSheetsState).WhiteListSheet;
            };
        }

        public async Task Run(CancellationToken cancellationToken = default)
        {
            IHostBuilder hostBuilder = Host.CreateDefaultBuilder();
            if (RpcProperties.RpcServer)
            {
                hostBuilder = hostBuilder
                    .UseMagicOnion(
                        new ServerPort(RpcProperties.RpcListenHost, RpcProperties.RpcListenPort, ServerCredentials.Insecure)
                    )
                    .ConfigureServices((ctx, services) =>
                    {
                        services.AddHostedService(provider => new ActionEvaluationPublisher(
                            NodeService.BlockChain,
                            IPAddress.Loopback.ToString(),
                            RpcProperties.RpcListenPort
                        ));
                    });
            }

            await hostBuilder.ConfigureServices((ctx, services) =>
            {
                services.AddHostedService(provider => NodeService);
                services.AddSingleton(provider => NodeService.BlockChain);
            }).RunConsoleAsync(cancellationToken);
        }

        private bool IsSignerAuthorized(Transaction<PolymorphicAction<ActionBase>> transaction)
        {
            var signerPublicKey = transaction.PublicKey;
            var whiteListSheet = GetWhiteListSheet?.Invoke();

            return whiteListSheet is null
                   || whiteListSheet.Count == 0
                   || whiteListSheet.Values.Any(row => signerPublicKey.Equals(row.PublicKey));
        }

    }
}
