using System;
using System.Collections.Generic;
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

        public AsyncAutoResetEvent BootstrapEnded { get; }

        public AsyncAutoResetEvent PreloadEnded { get; }

        private readonly IBlockPolicy<T> _blockPolicy;

        private Func<BlockChain<T>, Swarm<T>, PrivateKey, CancellationToken, Task> _minerLoopAction;

        private PrivateKey _privateKey;

        private Address _address;

        private LibplanetNodeServiceProperties _properties;

        private Progress<PreloadState> _preloadProgress;

        public LibplanetNodeService(
            LibplanetNodeServiceProperties properties,
            IBlockPolicy<T> blockPolicy,
            Func<BlockChain<T>, Swarm<T>, PrivateKey, CancellationToken, Task> minerLoopAction,
            Progress<PreloadState> preloadProgress
        )
        {
            _properties = properties;

            var uri = new Uri(_properties.GenesisBlockPath);
            using var client = new WebClient();
            var rawGenesisBlock = client.DownloadData(uri);
            var genesisBlock = Block<T>.Deserialize(rawGenesisBlock);

            var iceServers = _properties.IceServers;

            Store = LoadStore(_properties.StorePath, _properties.StoreType);
            _blockPolicy = blockPolicy;
            BlockChain = new BlockChain<T>(_blockPolicy, Store, genesisBlock);
            _privateKey = _properties.PrivateKey;
            _address = _privateKey.PublicKey.ToAddress();
            _minerLoopAction = minerLoopAction;
            Swarm = new Swarm<T>(
                BlockChain,
                _privateKey,
                _properties.AppProtocolVersion,
                trustedAppProtocolVersionSigners: _properties.TrustedAppProtocolVersionSigners,
                host: _properties.Host,
                listenPort: _properties.Port,
                iceServers: iceServers,
                workers: 50,
                differentAppProtocolVersionEncountered: _properties.DifferentAppProtocolVersionEncountered
            );

            PreloadEnded = new AsyncAutoResetEvent();
            BootstrapEnded = new AsyncAutoResetEvent();

            _preloadProgress = preloadProgress;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var peers = _properties.Peers.ToImmutableArray();
            if (peers.Any())
            {
                var trustedStateValidators = peers.Select(p => p.Address).ToImmutableHashSet();
                await Swarm.BootstrapAsync(
                    peers,
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(5),
                    depth: 1,
                    cancellationToken: cancellationToken);
                BootstrapEnded.Set();

                await Swarm.PreloadAsync(
                    TimeSpan.FromSeconds(5),
                    _preloadProgress,
                    trustedStateValidators, 
                    cancellationToken: cancellationToken
                );
                PreloadEnded.Set();
            }

            var tasks = new List<Task>
            {
                Swarm.StartAsync(cancellationToken: cancellationToken, millisecondsBroadcastTxInterval: 15000),
            };

            if (!_properties.NoMiner)
            {
                var minerLoopTask = Task.Run(
                    async () => await _minerLoopAction(BlockChain, Swarm, _privateKey, cancellationToken),
                    cancellationToken: cancellationToken);
                tasks.Add(minerLoopTask);
            }

            await Task.WhenAll(tasks);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Swarm.StopAsync(cancellationToken);
        }

        private BaseStore LoadStore(string path, string type)
        {
            BaseStore store = null;

            if (type == "rocksdb")
            {
                try
                {
                    store = new RocksDBStore.RocksDBStore(path);
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

            return store ?? new DefaultStore(path, flush: false, compress: true);
        }

        public void Dispose()
        {
            Store?.Dispose();
            Swarm?.Dispose();
        }
    }
}
