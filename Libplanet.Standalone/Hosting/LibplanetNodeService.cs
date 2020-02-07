using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
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
using Serilog;

namespace Libplanet.Standalone.Hosting
{
    public class LibplanetNodeService<T> : IHostedService
        where T : IAction, new()
    {
        public readonly IStore Store;

        public readonly BlockChain<T> BlockChain;

        public readonly Swarm<T> Swarm;

        private readonly IBlockPolicy<T> _blockPolicy;

        private Action<BlockChain<T>, Swarm<T>, PrivateKey> _minerLoopAction;

        private PrivateKey _privateKey;

        private Address _address;

        private LibplanetNodeServiceProperties _properties;

        public LibplanetNodeService(
            LibplanetNodeServiceProperties properties,
            IBlockPolicy<T> blockPolicy,
            Action<BlockChain<T>, Swarm<T>, PrivateKey> minerLoopAction)
        {
            _properties = properties;

            var uri = new Uri(_properties.GenesisBlockPath);
            using var client = new WebClient();
            var rawGenesisBlock = client.DownloadData(uri);
            var genesisBlock = Block<T>.Deserialize(rawGenesisBlock);

            var iceServers = _properties.IceServers;

            Store = new DefaultStore(path: properties.StorePath);
            _blockPolicy = blockPolicy;
            BlockChain = new BlockChain<T>(_blockPolicy, Store, genesisBlock);
            _privateKey = _properties.PrivateKey;
            _address = _privateKey.PublicKey.ToAddress();
            _minerLoopAction = minerLoopAction;
            Swarm = new Swarm<T>(
                BlockChain,
                _privateKey,
                _properties.AppProtocolVersion,
                host: _properties.Host,
                listenPort: _properties.Port,
                iceServers: iceServers);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var peers = _properties.Peers;
            if (peers.Any())
            {
                var trustedStateValidators = peers.Select(p => p.Address).ToImmutableHashSet();
                Swarm.BootstrapAsync(peers, null, null, cancellationToken: cancellationToken)
                    .Wait(cancellationToken);

                Swarm.PreloadAsync(null, new Progress<PreloadState>((state) => Log.Debug("{@state}", state)), trustedStateValidators, cancellationToken: cancellationToken)
                    .Wait(cancellationToken);
            }

            var tasks = new List<Task>
            {
                Swarm.StartAsync(cancellationToken: cancellationToken, millisecondsBroadcastTxInterval: 15000),
            };
    
            if (!_properties.NoMiner)
            {
                var minerLoopTask = Task.Run(
                    () => _minerLoopAction(BlockChain, Swarm, _privateKey),
                    cancellationToken: cancellationToken);
                tasks.Add(minerLoopTask);
            }                                                                                                                                               

            return Task.WhenAll(
                tasks
            );
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Swarm.StopAsync(cancellationToken);
        }
    }
}
