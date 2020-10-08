namespace Lib9c.Tests.Renderer
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Lib9c.Renderer;
    using Lib9c.Tests.Action;
    using Libplanet;
    using Libplanet.Blocks;
    using Libplanet.Crypto;
    using Libplanet.Tx;
    using Nekoyume.Action;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;
    using NCBlock = Libplanet.Blocks.Block<Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>>;

    public class RendererTest
    {
        public RendererTest(ITestOutputHelper output)
        {
            const string outputTemplate =
                "{Timestamp:HH:mm:ss:ffffffZ} - {Message}";
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(output, outputTemplate: outputTemplate)
                .CreateLogger()
                .ForContext<RendererTest>();
        }

        [Fact]
        public void BlockRendererTest()
        {
            var blockRenderer = new BlockRenderer();
            var branchPointBlock = new Block<NCAction>(
                index: 9,
                difficulty: 10000,
                totalDifficulty: 90000,
                nonce: new Nonce(new byte[] { 0x00, 0x00, 0x00, 0x01 }),
                miner: default,
                previousHash: null,
                timestamp: DateTimeOffset.MinValue,
                transactions: Enumerable.Empty<Transaction<NCAction>>());
            var oldBlock = new Block<NCAction>(
                index: 10,
                difficulty: 10000,
                totalDifficulty: 100000,
                nonce: new Nonce(new byte[] { 0x00, 0x00, 0x00, 0x02 }),
                miner: default,
                previousHash: branchPointBlock.Hash,
                timestamp: DateTimeOffset.MinValue.AddSeconds(1),
                transactions: Enumerable.Empty<Transaction<NCAction>>());
            var newBlock = new Block<NCAction>(
                index: 10,
                difficulty: 10000,
                totalDifficulty: 100000,
                nonce: new Nonce(new byte[] { 0x00, 0x00, 0x00, 0x03 }),
                miner: default,
                previousHash: branchPointBlock.Hash,
                timestamp: DateTimeOffset.MinValue.AddSeconds(2),
                transactions: Enumerable.Empty<Transaction<NCAction>>());

            (NCBlock OldTip, NCBlock NewTip) everyBlockResult = (null, null);
            (NCBlock OldTip, NCBlock NewTip, NCBlock BranchPoint) everyReorgResult = (null, null, null);
            (NCBlock OldTip, NCBlock NewTip, NCBlock BranchPoint) everyReorgEndResult = (null, null, null);

            blockRenderer.EveryBlock().Subscribe(pair => everyBlockResult = pair);
            blockRenderer.EveryReorg().Subscribe(ev => everyReorgResult = ev);
            blockRenderer.EveryReorgEnd().Subscribe(ev => everyReorgEndResult = ev);

            blockRenderer.RenderBlock(branchPointBlock, oldBlock);
            blockRenderer.RenderReorg(oldBlock, newBlock, branchPointBlock);
            blockRenderer.RenderReorgEnd(oldBlock, newBlock, branchPointBlock);

            Assert.Equal(branchPointBlock, everyBlockResult.OldTip);
            Assert.Equal(oldBlock, everyBlockResult.NewTip);
            Assert.Equal(oldBlock, everyReorgResult.OldTip);
            Assert.Equal(newBlock, everyReorgResult.NewTip);
            Assert.Equal(branchPointBlock, everyReorgResult.BranchPoint);
            Assert.Equal(oldBlock, everyReorgEndResult.OldTip);
            Assert.Equal(newBlock, everyReorgEndResult.NewTip);
            Assert.Equal(branchPointBlock, everyReorgEndResult.BranchPoint);
        }

        [Fact]
        public void ValidatingActionRendererTest()
        {
            var renderer = new ValidatingActionRenderer<NCAction>(new DebugPolicy());
            var branchPointBlock = new Block<NCAction>(
                index: 9,
                difficulty: 10000,
                totalDifficulty: 90000,
                nonce: new Nonce(new byte[] { 0x00, 0x00, 0x00, 0x01 }),
                miner: default,
                previousHash: null,
                timestamp: DateTimeOffset.MinValue,
                transactions: Enumerable.Empty<Transaction<NCAction>>());
            var differentBlock = new Block<NCAction>(
                index: 9,
                difficulty: 10000,
                totalDifficulty: 90000,
                nonce: new Nonce(new byte[] { 0x00, 0x00, 0x01, 0x01 }),
                miner: default,
                previousHash: null,
                timestamp: DateTimeOffset.MinValue,
                transactions: Enumerable.Empty<Transaction<NCAction>>());
            var oldBlock = new Block<NCAction>(
                index: 10,
                difficulty: 10000,
                totalDifficulty: 100000,
                nonce: new Nonce(new byte[] { 0x00, 0x00, 0x00, 0x02 }),
                miner: default,
                previousHash: branchPointBlock.Hash,
                timestamp: DateTimeOffset.MinValue.AddSeconds(1),
                transactions: Enumerable.Empty<Transaction<NCAction>>());
            var newBlock = new Block<NCAction>(
                index: 10,
                difficulty: 10000,
                totalDifficulty: 100000,
                nonce: new Nonce(new byte[] { 0x00, 0x00, 0x00, 0x03 }),
                miner: default,
                previousHash: oldBlock.Hash,
                timestamp: DateTimeOffset.MinValue.AddSeconds(2),
                transactions: Enumerable.Empty<Transaction<NCAction>>());
            var privateKey = new PrivateKey();
            NCAction action1 = new HackAndSlash
            {
                costumes = new List<int>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 0,
                stageId = 1,
            };
            NCAction action2 = new HackAndSlash
            {
                costumes = new List<int>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 0,
                stageId = 2,
            };
            NCAction action3 = new HackAndSlash
            {
                costumes = new List<int>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 0,
                stageId = 3,
            };
            var tx1 = Transaction<NCAction>.Create(
                0,
                privateKey,
                null,
                new[] { action1 },
                ImmutableHashSet<Address>.Empty,
                DateTimeOffset.MinValue);
            var tx2 = Transaction<NCAction>.Create(
                1,
                privateKey,
                null,
                new[] { action2, action3 },
                ImmutableHashSet<Address>.Empty,
                DateTimeOffset.MinValue);
            var blockWithTxs = new Block<NCAction>(
                index: 11,
                difficulty: 10000,
                totalDifficulty: 100000,
                nonce: new Nonce(new byte[] { 0x00, 0x00, 0x00, 0x04 }),
                miner: default,
                previousHash: oldBlock.Hash,
                timestamp: DateTimeOffset.MinValue.AddSeconds(3),
                transactions: new[] { tx1, tx2 });

            renderer.RenderBlock(branchPointBlock, oldBlock);
            Assert.Throws<ValidatingActionRenderer<NCAction>.InvalidRenderException>(() =>
                renderer.RenderBlockEnd(differentBlock, oldBlock));

            renderer.ResetRecords();

            renderer.RenderBlock(branchPointBlock, oldBlock);
            Assert.Throws<ValidatingActionRenderer<NCAction>.InvalidRenderException>(() =>
                renderer.RenderBlockEnd(branchPointBlock, newBlock));

            renderer.ResetRecords();

            // Different action render count
            renderer.RenderBlock(oldBlock, blockWithTxs);
            Assert.Throws<ValidatingActionRenderer<NCAction>.InvalidRenderException>(() => renderer.RenderBlockEnd(oldBlock, blockWithTxs));

            renderer.ResetRecords();

            // Different action render
            renderer.RenderBlock(oldBlock, blockWithTxs);
            renderer.RenderAction(
                new RewardGold(),
                new ActionContext { BlockIndex = blockWithTxs.Index },
                new State());
            renderer.RenderAction(
                new RewardGold(),
                new ActionContext { BlockIndex = blockWithTxs.Index },
                new State());
            renderer.RenderAction(
                new RewardGold(),
                new ActionContext { BlockIndex = blockWithTxs.Index },
                new State());
            renderer.RenderAction(
                new RewardGold(),
                new ActionContext { BlockIndex = blockWithTxs.Index },
                new State());
            Assert.Throws<ValidatingActionRenderer<NCAction>.InvalidRenderException>(() => renderer.RenderBlockEnd(oldBlock, blockWithTxs));

            renderer.ResetRecords();

            // Different action order
            renderer.RenderBlock(oldBlock, blockWithTxs);
            renderer.RenderAction(
                action1,
                new ActionContext { BlockIndex = blockWithTxs.Index },
                new State());
            renderer.RenderAction(
                action3,
                new ActionContext { BlockIndex = blockWithTxs.Index },
                new State());
            renderer.RenderAction(
                action2,
                new ActionContext { BlockIndex = blockWithTxs.Index },
                new State());
            renderer.RenderAction(
                new RewardGold(),
                new ActionContext { BlockIndex = blockWithTxs.Index },
                new State());
            Assert.Throws<ValidatingActionRenderer<NCAction>.InvalidRenderException>(() => renderer.RenderBlockEnd(oldBlock, blockWithTxs));

            renderer.ResetRecords();

            renderer.RenderBlock(oldBlock, blockWithTxs);
            renderer.RenderAction(
                action1,
                new ActionContext { BlockIndex = blockWithTxs.Index },
                new State());
            renderer.RenderAction(
                action2,
                new ActionContext { BlockIndex = blockWithTxs.Index },
                new State());
            renderer.RenderAction(
                action3,
                new ActionContext { BlockIndex = blockWithTxs.Index },
                new State());
            renderer.RenderAction(
                new RewardGold(),
                new ActionContext { BlockIndex = blockWithTxs.Index },
                new State());
            renderer.RenderBlockEnd(oldBlock, blockWithTxs);
        }
    }
}
