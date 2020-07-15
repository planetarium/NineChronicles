using System;
using System.Net;
using System.Security.Cryptography;
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
using Nekoyume.Model.State;
using NineChronicles.Standalone.Properties;
using Nito.AsyncEx;
using Serilog;

using NineChroniclesActionType = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace NineChronicles.Standalone
{
    public class NineChroniclesNodeService
    {
        private LibplanetNodeService<NineChroniclesActionType> NodeService { get; set; }

        private LibplanetNodeServiceProperties<NineChroniclesActionType> Properties { get; }

        private RpcNodeServiceProperties? RpcProperties { get; }

        public AsyncManualResetEvent BootstrapEnded => NodeService.BootstrapEnded;

        public AsyncManualResetEvent PreloadEnded => NodeService.PreloadEnded;

        public Swarm<NineChroniclesActionType> Swarm => NodeService?.Swarm;

        public PrivateKey PrivateKey { get; }


        public NineChroniclesNodeService(
            LibplanetNodeServiceProperties<NineChroniclesActionType> properties,
            RpcNodeServiceProperties? rpcNodeServiceProperties,
            Progress<PreloadState> preloadProgress = null,
            bool ignoreBootstrapFailure = false
        )
        {
            Properties = properties;
            RpcProperties = rpcNodeServiceProperties;

            try
            {
                Libplanet.Crypto.CryptoConfig.CryptoBackend = new Secp256K1CryptoBackend<SHA256>();
                Log.Debug("Secp256K1CryptoBackend initialized.");
            }
            catch(Exception e)
            {
                Log.Error("Secp256K1CryptoBackend initialize failed. Use default backend. {e}", e);
            }

            // BlockPolicy shared through Lib9c.
            IBlockPolicy<PolymorphicAction<ActionBase>> blockPolicy = BlockPolicy.GetPolicy(
                properties.MinimumDifficulty
            );
            async Task minerLoopAction(
                BlockChain<NineChroniclesActionType> chain,
                Swarm<NineChroniclesActionType> swarm,
                PrivateKey privateKey,
                CancellationToken cancellationToken)
            {
                var miner = new Miner(chain, swarm, privateKey);
                Log.Debug("Miner called.");
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        if (swarm.Running)
                        {
                            Log.Debug("Start mining.");
                            await miner.MineBlockAsync(cancellationToken);
                        }
                        else
                        {
                            await Task.Delay(1000, cancellationToken);
                        }
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
                minerLoopAction,
                preloadProgress,
                ignoreBootstrapFailure
            );

            // FIXME: Agent.cs와 중복된 코드입니다.
            if (BlockPolicy.ActivatedAccounts is null)
            {
                var rawState = NodeService?.BlockChain?.GetState(ActivatedAccountsState.Address);
                BlockPolicy.UpdateActivationSet(rawState);
            }
        }

        public IHostBuilder Configure(IHostBuilder hostBuilder)
        {
            if (RpcProperties is RpcNodeServiceProperties rpcProperties)
            {
                hostBuilder = hostBuilder
                    .UseMagicOnion(
                        new ServerPort(rpcProperties.RpcListenHost, rpcProperties.RpcListenPort, ServerCredentials.Insecure)
                    )
                    .ConfigureServices((ctx, services) =>
                    {
                        services.AddHostedService(provider => new ActionEvaluationPublisher(
                            NodeService.BlockChain,
                            IPAddress.Loopback.ToString(),
                            rpcProperties.RpcListenPort
                        ));
                    });
            }

            return hostBuilder.ConfigureServices((ctx, services) =>
            {
                services.AddHostedService(provider => NodeService);
                services.AddSingleton(provider => NodeService.Swarm);
                services.AddSingleton(provider => NodeService.BlockChain);
            });
        }

        public void StartMining(PrivateKey privateKey) => NodeService.StartMining(privateKey);

        public void StopMining() => NodeService.StopMining();
    }
}
