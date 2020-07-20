using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.Net;
using Libplanet.Net.Protocols;
using Libplanet.Store;
using Microsoft.Extensions.Hosting;
using Nito.AsyncEx;
using Serilog;

namespace Libplanet.Standalone.Hosting
{
    public class LibplanetNodeService<T> : IHostedService, IDisposable
        where T : IAction, new()
    {
        public readonly BaseStore Store;

        public readonly BlockChain<T> BlockChain;

        public readonly Swarm<T> Swarm;

        public AsyncManualResetEvent BootstrapEnded { get; }

        public AsyncManualResetEvent PreloadEnded { get; }

        public PrivateKey PrivateKey { get; private set; }

        private Func<BlockChain<T>, Swarm<T>, PrivateKey, CancellationToken, Task> _minerLoopAction;

        private LibplanetNodeServiceProperties<T> _properties;

        private Progress<PreloadState> _preloadProgress;

        private bool _ignoreBootstrapFailure;

        private CancellationToken _swarmCancellationToken;

        private CancellationTokenSource _miningCancellationTokenSource;

        public LibplanetNodeService(
            LibplanetNodeServiceProperties<T> properties,
            IBlockPolicy<T> blockPolicy,
            Func<BlockChain<T>, Swarm<T>, PrivateKey, CancellationToken, Task> minerLoopAction,
            Progress<PreloadState> preloadProgress,
            bool ignoreBootstrapFailure = false
        )
        {
            _properties = properties;

            var genesisBlock = LoadGenesisBlock(properties);

            var iceServers = _properties.IceServers;

            Store = LoadStore(
                _properties.StorePath,
                _properties.StoreType,
                _properties.StoreStatesCacheSize);

            var chainIds = Store.ListChainIds().ToList();
            Log.Debug($"Number of chain ids: {chainIds.Count()}");

            foreach (var chainId in chainIds)
            {
                Log.Debug($"chainId: {chainId}");
            }

            BlockChain = new BlockChain<T>(blockPolicy, Store, genesisBlock, _properties.Render);
            _minerLoopAction = minerLoopAction;
            Swarm = new Swarm<T>(
                BlockChain,
                _properties.PrivateKey,
                _properties.AppProtocolVersion,
                trustedAppProtocolVersionSigners: _properties.TrustedAppProtocolVersionSigners,
                host: _properties.Host,
                listenPort: _properties.Port,
                iceServers: iceServers,
                workers: 50,
                differentAppProtocolVersionEncountered: _properties.DifferentAppProtocolVersionEncountered
            );

            PreloadEnded = new AsyncManualResetEvent();
            BootstrapEnded = new AsyncManualResetEvent();

            _preloadProgress = preloadProgress;
            _ignoreBootstrapFailure = ignoreBootstrapFailure;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var peers = _properties.Peers.ToImmutableArray();

            if (peers.Any())
            {
                try
                {
                    await Swarm.BootstrapAsync(
                        peers,
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(5),
                        depth: 1,
                        cancellationToken: cancellationToken);
                    BootstrapEnded.Set();
                }
                catch (PeerDiscoveryException e)
                {
                    Log.Error(e, "Bootstrap failed: {Exception}", e);

                    if (!_ignoreBootstrapFailure)
                    {
                        throw;
                    }
                }

                await Swarm.PreloadAsync(
                    TimeSpan.FromSeconds(5),
                    _preloadProgress,
                    _properties.TrustedStateValidators,
                    cancellationToken: cancellationToken
                );
                PreloadEnded.Set();
            }
            else
            {
                BootstrapEnded.Set();
                PreloadEnded.Set();
            }

            _swarmCancellationToken = cancellationToken;

            try
            {
                await Swarm.StartAsync(
                    cancellationToken: cancellationToken,
                    millisecondsBroadcastTxInterval: 15000);
            }
            catch (Exception e)
            {
                Log.Error(e, "Unexpected exception occurred during Swarm.StartAsync(). {e}", e);
            }
        }

        // 이 privateKey는 swarm에서 사용하는 privateKey와 다를 수 있습니다.
        public void StartMining(PrivateKey privateKey)
        {
            if (PrivateKey is null)
            {
                throw new InvalidOperationException(
                    $"An exception occurred during {nameof(StartMining)}(). " +
                    $"{nameof(PrivateKey)} is null.");
            }

            if (BlockChain is null)
            {
                throw new InvalidOperationException(
                    $"An exception occurred during {nameof(StartMining)}(). " +
                    $"{nameof(BlockChain)} is null.");
            }

            if (Swarm is null)
            {
                throw new InvalidOperationException(
                    $"An exception occurred during {nameof(StartMining)}(). " +
                    $"{nameof(Swarm)} is null.");
            }

            PrivateKey = privateKey;
            _miningCancellationTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(_swarmCancellationToken);
            Task.Run(
                () => _minerLoopAction(BlockChain, Swarm, privateKey, _miningCancellationTokenSource.Token),
                _miningCancellationTokenSource.Token);
        }

        public void StopMining()
        {
            _miningCancellationTokenSource?.Cancel();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopMining();
            return Swarm.StopAsync(cancellationToken);
        }

        private BaseStore LoadStore(string path, string type, int statesCacheSize)
        {
            BaseStore store = null;

            if (type == "rocksdb")
            {
                try
                {
                    store = new RocksDBStore.RocksDBStore(path, statesCacheSize: statesCacheSize);
                    Log.Debug("RocksDB is initialized.");
                }
                catch (TypeInitializationException e)
                {
                    Log.Error("RocksDB is not available. DefaultStore will be used. {0}", e);
                }
            }
            else
            {
                var message = type is null
                    ? "Storage Type is not specified"
                    : $"Storage Type {type} is not supported";
                Log.Debug($"{message}. DefaultStore will be used.");
            }

            return store ?? new DefaultStore(
                path, flush: false, compress: true, statesCacheSize: statesCacheSize);
        }

        private Block<T> LoadGenesisBlock(LibplanetNodeServiceProperties<T> properties)
        {
            if (!(properties.GenesisBlock is null))
            {
                return properties.GenesisBlock;
            }
            else if (!string.IsNullOrEmpty(properties.GenesisBlockPath))
            {
                var uri = new Uri(_properties.GenesisBlockPath);
                using var client = new WebClient();
                var rawGenesisBlock = client.DownloadData(uri);
                return Block<T>.Deserialize(rawGenesisBlock);
            }
            else
            {
                throw new ArgumentException(
                    $"At least, one of {nameof(LibplanetNodeServiceProperties<T>.GenesisBlock)} or {nameof(LibplanetNodeServiceProperties<T>.GenesisBlockPath)} must be set.");
            }
        }

        public void Dispose()
        {
            Log.Debug($"Disposing {nameof(LibplanetNodeService<T>)}...");

            Swarm?.Dispose();
            Log.Debug("Swarm disposed.");

            Store?.Dispose();
            Log.Debug("Store disposed.");
        }
    }
}
