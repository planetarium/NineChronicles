using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Subscription;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.Net;
using Libplanet.Standalone.Hosting;
using NineChronicles.Standalone.Tests.Common.Actions;
using Xunit;
using Xunit.Abstractions;

namespace NineChronicles.Standalone.Tests.GraphTypes
{
    public class StandaloneSubscriptionTest : GraphQLTestBase
    {
        public StandaloneSubscriptionTest(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task SubscribeTipChangedEvent()
        {
            var miner = new Address();

            const int repeat = 10;
            foreach (long index in Enumerable.Range(1, repeat))
            {
                await BlockChain.MineBlock(miner);

                var result = await ExecuteQueryAsync("subscription { tipChanged { index hash } }");

                Assert.IsType<SubscriptionExecutionResult>(result);
                var subscribeResult = (SubscriptionExecutionResult) result;
                Assert.Equal(index, StandaloneContextFx.BlockChain.Tip.Index);
                var stream = subscribeResult.Streams.Values.FirstOrDefault();
                var rawEvents = await stream.Take((int)index);
                Assert.NotNull(rawEvents);

                var events = (Dictionary<string, object>) rawEvents.Data;
                var tipChangedEvent = (Dictionary<string, object>) events["tipChanged"];
                Assert.Equal(index, tipChangedEvent["index"]);
                Assert.Equal(BlockChain[index].Hash.ToByteArray(), ByteUtil.ParseHex((string) tipChangedEvent["hash"]));
            }
        }

        [Fact]
        public async Task SubscribePreloadProgress()
        {
            var cts = new CancellationTokenSource();

            var apvPrivateKey = new PrivateKey();
            var apv = AppProtocolVersion.Sign(apvPrivateKey, 0);
            var genesisBlock = BlockChain<EmptyAction>.MakeGenesisBlock();

            // 에러로 인하여 NineChroniclesNodeService 를 사용할 수 없습니다. https://git.io/JfS0M
            // 따라서 LibplanetNodeService로 비슷한 환경을 맞춥니다.
            // 1. 노드를 생성합니다.
            var seedNode = CreateLibplanetNodeService<EmptyAction>(genesisBlock, apv, apvPrivateKey.PublicKey);
            await StartAsync(seedNode.Swarm, cts.Token);

            // 2. Progress를 넘겨 preloadProgress subscription 과 연결합니다.
            var service = CreateLibplanetNodeService<EmptyAction>(
                genesisBlock,
                apv,
                apvPrivateKey.PublicKey,
                new Progress<PreloadState>(state =>
                {
                    StandaloneContextFx.PreloadStateSubject.OnNext(state);
                }),
                new [] { seedNode.Swarm.AsPeer });

            var miner = new PrivateKey().ToAddress();
            await seedNode.BlockChain.MineBlock(miner);
            var result = await ExecuteQueryAsync("subscription { preloadProgress { currentPhase totalPhase extra { type currentCount totalCount } } }");
            Assert.IsType<SubscriptionExecutionResult>(result);

            service.StartAsync(cts.Token);

            await service.PreloadEnded.WaitAsync(cts.Token);

            var subscribeResult = (SubscriptionExecutionResult) result;
            var stream = subscribeResult.Streams.Values.FirstOrDefault();

            // BlockHashDownloadState  : 1
            // BlockDownloadState      : 1
            // BlockVerificationState  : 1
            // ActionExecutionState    : 1
            const int preloadStatesCount = 4;
            var preloadProgressRecords =
                new List<(long currentPhase, long totalPhase, string type, long currentCount, long totalCount)>();
            var expectedPreloadProgress = new List<(long currentPhase, long totalPhase, string type, long currentCount, long totalCount)>
            {
                (1, 5, "BlockHashDownloadState", 1, 1),
                (2, 5, "BlockDownloadState", 1, 1),
                (3, 5, "BlockVerificationState", 1, 1),
                (5, 5, "ActionExecutionState", 1, 1),
            };
            foreach (var index in Enumerable.Range(1, preloadStatesCount))
            {
                var rawEvents = await stream.Take(index);
                var preloadProgress = (Dictionary<string, object>)((Dictionary<string, object>)rawEvents.Data)["preloadProgress"];
                var preloadProgressExtra = (Dictionary<string, object>)preloadProgress["extra"];

                preloadProgressRecords.Add((
                    (long)preloadProgress["currentPhase"],
                    (long)preloadProgress["totalPhase"],
                    (string)preloadProgressExtra["type"],
                    (long)preloadProgressExtra["currentCount"],
                    (long)preloadProgressExtra["totalCount"]));
            }

            Assert.True(preloadProgressRecords.ToImmutableHashSet().SetEquals(expectedPreloadProgress));

            await seedNode.StopAsync(cts.Token);
            await service.StopAsync(cts.Token);
        }

        [Fact(Timeout = 15000)]
        public async Task SubscribeDifferentAppProtocolVersionEncounter()
        {   
            var result = await ExecuteQueryAsync(@"
                subscription {
                    differentAppProtocolVersionEncounter {
                        peer
                        peerVersion {
                            version
                            signer
                            signature
                            extra
                        }
                        localVersion {
                            version
                            signer
                            signature
                            extra
                        }
                    }
                }
            ");
            var subscribeResult = (SubscriptionExecutionResult) result;
            var stream = subscribeResult.Streams.Values.FirstOrDefault();
            Assert.NotNull(stream);

            Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await stream.Take(1).Timeout(TimeSpan.FromMilliseconds(5000)).FirstAsync();
            });

            var apvPrivateKey = new PrivateKey();
            var apv1 = AppProtocolVersion.Sign(apvPrivateKey, 1);
            var apv2 = AppProtocolVersion.Sign(apvPrivateKey, 0);
            var peer = new Peer(apvPrivateKey.PublicKey, apv1);
            StandaloneContextFx.DifferentAppProtocolVersionEncounterSubject.OnNext(
                new DifferentAppProtocolVersionEncounter
                {
                    Peer = peer,
                    PeerVersion = apv1,
                    LocalVersion = apv2,
                }
            );
            var rawEvents = await stream.Take(1);
            var rawEvent = (Dictionary<string, object>)rawEvents.Data;
            var differentAppProtocolVersionEncounter =
                (Dictionary<string, object>)rawEvent["differentAppProtocolVersionEncounter"];
            Assert.Equal(
                peer.ToString(),
                differentAppProtocolVersionEncounter["peer"]
            );
            var peerVersion =
                (Dictionary<string, object>)differentAppProtocolVersionEncounter["peerVersion"];
            Assert.Equal(apv1.Version, peerVersion["version"]);
            Assert.Equal(apv1.Signer, new Address(((string)peerVersion["signer"]).Substring(2)));
            Assert.Equal(apv1.Signature, ByteUtil.ParseHex((string)peerVersion["signature"]));
            Assert.Equal(apv1.Extra, peerVersion["extra"]);
            var localVersion =
                (Dictionary<string, object>)differentAppProtocolVersionEncounter["localVersion"];
            Assert.Equal(apv2.Version, localVersion["version"]);
            Assert.Equal(apv2.Signer, new Address(((string)localVersion["signer"]).Substring(2)));
            Assert.Equal(apv2.Signature, ByteUtil.ParseHex((string)localVersion["signature"]));
            Assert.Equal(apv2.Extra, localVersion["extra"]);
        }
    }
}
