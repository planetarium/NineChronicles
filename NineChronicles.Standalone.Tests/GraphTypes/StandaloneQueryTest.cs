using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bencodex;
using Bencodex.Types;
using GraphQL;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.KeyStore;
using Libplanet.Net;
using Libplanet.Standalone.Hosting;
using Libplanet.Store;
using LiteDB;
using NineChronicles.Standalone.Tests.Common.Actions;
using Serilog;
using Xunit;
using Xunit.Abstractions;
using IPAddress = Org.BouncyCastle.Utilities.Net.IPAddress;

namespace NineChronicles.Standalone.Tests.GraphTypes
{
    public class StandaloneQueryTest : GraphQLTestBase
    {
        public StandaloneQueryTest(ITestOutputHelper output) : base(output)
        {
            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console().CreateLogger();
        }

        [Fact]
        public async Task GetState()
        {
            var codec = new Codec();
            var miner = new Address();

            const int repeat = 10;
            foreach (long index in Enumerable.Range(1, repeat))
            {
                await BlockChain.MineBlock(miner);

                var result = await ExecuteQueryAsync($"query {{ state(address: \"{miner.ToHex()}\") }}");

                var data = (Dictionary<string, object>) result.Data;
                var state = (Integer)codec.Decode(ByteUtil.ParseHex((string) data["state"]));

                // TestRewardGold에서 miner에게 1 gold 씩 주므로 block index와 같을 것입니다.
                Assert.Equal((Integer)index, state);
            }
        }

        [Theory]
        [InlineData(2)]
        [InlineData(16)]
        [InlineData(32)]
        public async Task ListPrivateKeys(int repeat)
        {
            var generatedProtectedPrivateKeys = new List<ProtectedPrivateKey>();
            foreach (var _ in Enumerable.Range(0, repeat))
            {
                var (protectedPrivateKey, _) = CreateProtectedPrivateKey();
                generatedProtectedPrivateKeys.Add(protectedPrivateKey);
                KeyStore.Add(protectedPrivateKey);
            }

            var result = await ExecuteQueryAsync("query { keyStore { protectedPrivateKeys { address } } }");

            var data = (Dictionary<string, object>) result.Data;
            var keyStoreResult = (Dictionary<string, object>) data["keyStore"];
            var protectedPrivateKeyAddresses =
                keyStoreResult["protectedPrivateKeys"].As<List<object>>()
                    .Cast<Dictionary<string, object>>()
                    .Select(x => x["address"].As<string>())
                    .ToImmutableList();

            foreach (var protectedPrivateKey in generatedProtectedPrivateKeys)
            {
                Assert.Contains(protectedPrivateKey.Address.ToString(), protectedPrivateKeyAddresses);
            }

            var (notStoredProtectedPrivateKey, _) = CreateProtectedPrivateKey();
            Assert.DoesNotContain(notStoredProtectedPrivateKey.Address.ToString(), protectedPrivateKeyAddresses);
        }

        [Fact]
        public async Task DecryptedPrivateKey()
        {
            var (protectedPrivateKey, passphrase) = CreateProtectedPrivateKey();
            var privateKey = protectedPrivateKey.Unprotect(passphrase);
            KeyStore.Add(protectedPrivateKey);

            var result = await ExecuteQueryAsync($"query {{ keyStore {{ decryptedPrivateKey(address: \"{privateKey.ToAddress()}\", passphrase: \"{passphrase}\") }} }}");

            var data = (Dictionary<string, object>) result.Data;
            var keyStoreResult = (Dictionary<string, object>) data["keyStore"];
            var decryptedPrivateKeyResult = (string) keyStoreResult["decryptedPrivateKey"];

            Assert.Equal(ByteUtil.Hex(privateKey.ByteArray), decryptedPrivateKeyResult);
        }

        [Fact]
        public async Task NodeStatus()
        {
            var cts = new CancellationTokenSource();

            var apvPrivateKey = new PrivateKey();
            var apv = AppProtocolVersion.Sign(apvPrivateKey, 0);
            var genesisBlock = BlockChain<EmptyAction>.MakeGenesisBlock();

            LibplanetNodeService<T> CreateLibplanetNodeService<T>(Block<T> genesisBlock, AppProtocolVersion appProtocolVersion, IEnumerable<Peer> peers = null)
                where T : IAction, new()
            {
                var properties = new LibplanetNodeServiceProperties<T>
                {
                    Host = System.Net.IPAddress.Loopback.ToString(),
                    AppProtocolVersion = appProtocolVersion,
                    GenesisBlock = genesisBlock,
                    StoreStatesCacheSize = 2,
                    PrivateKey = new PrivateKey(),
                    Port = null,
                    MinimumDifficulty = 1024,
                    NoMiner = true,
                    Render = false,
                    Peers = peers ?? ImmutableHashSet<Peer>.Empty,
                    TrustedAppProtocolVersionSigners = ImmutableHashSet<PublicKey>.Empty.Add(apvPrivateKey.PublicKey)
                };

                return new LibplanetNodeService<T>(
                    properties,
                    new BlockPolicy<T>(),
                    async (chain, swarm, privateKey, cancellationToken) => { },
                    null);
            }


            // 에러로 인하여 NineChroniclesNodeService 를 사용할 수 없습니다. https://git.io/JfS0M
            // 따라서 LibplanetNodeService로 비슷한 환경을 맞춥니다.
            // 1. 노드를 생성합니다.
            var seedNode = CreateLibplanetNodeService<EmptyAction>(genesisBlock, apv);
            await StartAsync(seedNode.Swarm, cts.Token);
            var service = CreateLibplanetNodeService<EmptyAction>(genesisBlock, apv, new [] { seedNode.Swarm.AsPeer });

            // 2. NineChroniclesNodeService.ConfigureStandaloneContext(standaloneContext)를 호출합니다.
            // BlockChain 객체 공유 및 PreloadEnded, BootstrapEnded 이벤트 훅의 처리를 합니다.
            // BlockChain 객체 공유는 액션 타입이 달라 생략합니다.
            service.BootstrapEnded.WaitAsync()
                .ContinueWith(task => StandaloneContextFx.BootstrapEnded = true);
            service.PreloadEnded.WaitAsync()
                .ContinueWith(task => StandaloneContextFx.PreloadEnded = true);

            var bootstrapEndedTask = service.BootstrapEnded.WaitAsync();
            var preloadEndedTask = service.PreloadEnded.WaitAsync();

            async Task<Dictionary<string, bool>> QueryNodeStatus()
            {
                var result = await ExecuteQueryAsync("query { nodeStatus { bootstrapEnded preloadEnded } }");
                var data = (Dictionary<string, object>) result.Data;
                var nodeStatusData = (Dictionary<string, object>) data["nodeStatus"];
                return nodeStatusData.ToDictionary(pair => pair.Key, pair => (bool)pair.Value);
            }

            var nodeStatus = await QueryNodeStatus();
            Assert.False(nodeStatus["bootstrapEnded"]);
            Assert.False(nodeStatus["preloadEnded"]);

            service.StartAsync(cts.Token);

            await bootstrapEndedTask;
            await preloadEndedTask;

            // ContinueWith으로 넘긴 태스크가 실행되기를 기다립니다.
            await Task.Delay(1000);

            nodeStatus = await QueryNodeStatus();
            Assert.True(nodeStatus["bootstrapEnded"]);
            Assert.True(nodeStatus["preloadEnded"]);

            await seedNode.StopAsync(cts.Token);
        }

        private (ProtectedPrivateKey, string) CreateProtectedPrivateKey()
        {
            string CreateRandomBase64String()
            {
                var random = new Random();
                Span<byte> buffer = stackalloc byte[16];
                random.NextBytes(buffer);
                return Convert.ToBase64String(buffer);
            }

            // 랜덤 문자열을 생성하여 passphrase로 사용합니다.
            var passphrase = CreateRandomBase64String();
            return (ProtectedPrivateKey.Protect(new PrivateKey(), passphrase), passphrase);
        }

        private async Task<Task> StartAsync<T>(
            Swarm<T> swarm,
            CancellationToken cancellationToken = default
        )
            where T : IAction, new()
        {
            Task task = swarm.StartAsync(200, 200, cancellationToken);
            await swarm.WaitForRunningAsync();
            return task;
        }

    }
}
