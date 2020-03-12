using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
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
    public class NineChroniclesNodeService
    {
        private LibplanetNodeServiceProperties Properties { get; }

        private RpcNodeServiceProperties RpcProperties { get; } 

        public NineChroniclesNodeService(
            LibplanetNodeServiceProperties properties,
            RpcNodeServiceProperties rpcNodeServiceProperties)
        {
            Properties = properties;
            RpcProperties = rpcNodeServiceProperties;
        } 
        
        public async Task Run(CancellationToken cancellationToken = default)
        {
            // BlockPolicy shared through Lib9c.
            IBlockPolicy<PolymorphicAction<ActionBase>> blockPolicy = BlockPolicy.GetPolicy();
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

            var nodeService = new LibplanetNodeService<NineChroniclesActionType>(
                Properties, 
                blockPolicy, 
                minerLoopAction
            );

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
                            nodeService.BlockChain,
                            IPAddress.Loopback.ToString(),
                            RpcProperties.RpcListenPort
                        ));
                    });
            }

            await hostBuilder.ConfigureServices((ctx, services) =>
            {
                services.AddHostedService(provider => nodeService);
                services.AddSingleton(provider => nodeService.BlockChain);
            }).RunConsoleAsync(cancellationToken);
        }
    }
}
