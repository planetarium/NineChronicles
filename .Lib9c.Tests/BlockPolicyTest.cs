namespace Lib9c.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Threading.Tasks;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Blockchain;
    using Libplanet.Blockchain.Policies;
    using Libplanet.Blocks;
    using Libplanet.Crypto;
    using Libplanet.Store;
    using Libplanet.Tx;
    using Nekoyume.Action;
    using Nekoyume.BlockChain;
    using Nekoyume.Model;
    using Nekoyume.Model.State;
    using Serilog.Core;
    using Xunit;

    public class BlockPolicyTest
    {
        [Fact]
        public void DoesTransactionFollowsPolicyWithEmpty()
        {
            var adminPrivateKey = new PrivateKey();
            var adminAddress = new Address(adminPrivateKey.PublicKey);
            var blockPolicySource = new BlockPolicySource(Logger.None);
            IBlockPolicy<PolymorphicAction<ActionBase>> policy = blockPolicySource.GetPolicy(10000, 100);
            Block<PolymorphicAction<ActionBase>> genesis = MakeGenesisBlock(adminAddress, ImmutableHashSet<Address>.Empty);

            using var store = new DefaultStore(null);
            var blockChain = new BlockChain<PolymorphicAction<ActionBase>>(
                policy,
                store,
                store,
                genesis,
                renderers: new[] { blockPolicySource.BlockRenderer }
            );
            Transaction<PolymorphicAction<ActionBase>> tx = Transaction<PolymorphicAction<ActionBase>>.Create(
                0,
                new PrivateKey(),
                genesis.Hash,
                new PolymorphicAction<ActionBase>[] { });

            Assert.True(policy.DoesTransactionFollowsPolicy(tx, blockChain));
        }

        [Fact]
        public async Task DoesTransactionFollowsPolicy()
        {
            var adminPrivateKey = new PrivateKey();
            var adminAddress = adminPrivateKey.ToAddress();
            var activatedPrivateKey = new PrivateKey();
            var activatedAddress = activatedPrivateKey.ToAddress();

            var blockPolicySource = new BlockPolicySource(Logger.None);
            IBlockPolicy<PolymorphicAction<ActionBase>> policy = blockPolicySource.GetPolicy(10000, 100);
            Block<PolymorphicAction<ActionBase>> genesis = MakeGenesisBlock(
                adminAddress,
                ImmutableHashSet.Create(activatedAddress).Add(adminAddress)
            );
            using var store = new DefaultStore(null);
            var blockChain = new BlockChain<PolymorphicAction<ActionBase>>(
                policy,
                store,
                store,
                genesis,
                renderers: new[] { blockPolicySource.BlockRenderer }
            );
            Transaction<PolymorphicAction<ActionBase>> txByStranger =
                Transaction<PolymorphicAction<ActionBase>>.Create(
                    0,
                    new PrivateKey(),
                    genesis.Hash,
                    new PolymorphicAction<ActionBase>[] { }
                );

            // 새로 만든 키는 활성화 유저 리스트에 없기 때문에 차단됩니다.
            Assert.False(policy.DoesTransactionFollowsPolicy(txByStranger, blockChain));

            var newActivatedPrivateKey = new PrivateKey();
            var newActivatedAddress = newActivatedPrivateKey.ToAddress();

            // 관리자 계정으로 활성화 시킵니다.
            Transaction<PolymorphicAction<ActionBase>> invitationTx = blockChain.MakeTransaction(
                adminPrivateKey,
                new PolymorphicAction<ActionBase>[] { new AddActivatedAccount(newActivatedAddress) }
            );
            blockChain.StageTransaction(invitationTx);
            await blockChain.MineBlock(adminAddress);

            Transaction<PolymorphicAction<ActionBase>> txByNewActivated =
                Transaction<PolymorphicAction<ActionBase>>.Create(
                    0,
                    newActivatedPrivateKey,
                    genesis.Hash,
                    new PolymorphicAction<ActionBase>[] { }
                );

            // 활성화 된 계정이기 때문에 테스트에 성공합니다.
            Assert.True(policy.DoesTransactionFollowsPolicy(txByNewActivated, blockChain));
        }

        [Fact]
        public async Task ValidateNextBlockWithAuthorizedMinersState()
        {
            var adminPrivateKey = new PrivateKey();
            var adminAddress = adminPrivateKey.ToAddress();
            var miners = new[]
            {
                new Address(
                    new byte[]
                    {
                        0x01, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00,
                    }
                ),
                new Address(
                    new byte[]
                    {
                        0x02, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00,
                    }
                ),
            };
            var stranger = new Address(
                new byte[]
                {
                    0x03, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00,
                }
            );

            var blockPolicySource = new BlockPolicySource(Logger.None);
            IBlockPolicy<PolymorphicAction<ActionBase>> policy = blockPolicySource.GetPolicy(10000, 100);
            Block<PolymorphicAction<ActionBase>> genesis = MakeGenesisBlock(
                adminAddress,
                ImmutableHashSet<Address>.Empty,
                new AuthorizedMinersState(miners, 2, 4)
            );
            using var store = new DefaultStore(null);
            var blockChain = new BlockChain<PolymorphicAction<ActionBase>>(
                policy,
                store,
                store,
                genesis,
                renderers: new[] { blockPolicySource.BlockRenderer }
            );

            if (policy is BlockPolicy bp)
            {
                bp.AuthorizedMinersState = new AuthorizedMinersState(
                    (Dictionary)blockChain.GetState(AuthorizedMinersState.Address)
                    );
            }

            await blockChain.MineBlock(stranger);

            await Assert.ThrowsAsync<InvalidMinerException>(async () =>
            {
                await blockChain.MineBlock(stranger);
            });

            await blockChain.MineBlock(miners[0]);

            // it's okay because next block index is 3
            await blockChain.MineBlock(stranger);

            // it isn't :(
            await Assert.ThrowsAsync<InvalidMinerException>(async () =>
            {
                await blockChain.MineBlock(stranger);
            });

            await blockChain.MineBlock(miners[1]);

            // it's okay because block index exceeds limitations.
            await blockChain.MineBlock(stranger);
        }

        [Fact]
        public async Task GetNextBlockDifficultyWithAuthorizedMinersState()
        {
            var adminPrivateKey = new PrivateKey();
            var adminAddress = adminPrivateKey.ToAddress();
            var miner = new PrivateKey().ToAddress();
            var miners = new[] { miner };

            var blockPolicySource = new BlockPolicySource(Logger.None);
            IBlockPolicy<PolymorphicAction<ActionBase>> policy = blockPolicySource.GetPolicy(4096, 100);
            Block<PolymorphicAction<ActionBase>> genesis = MakeGenesisBlock(
                adminAddress,
                ImmutableHashSet<Address>.Empty,
                new AuthorizedMinersState(miners, 2, 6)
            );
            using var store = new DefaultStore(null);
            var blockChain = new BlockChain<PolymorphicAction<ActionBase>>(
                policy,
                store,
                store,
                genesis,
                renderers: new[] { blockPolicySource.BlockRenderer }
            );

            if (policy is BlockPolicy bp)
            {
                bp.AuthorizedMinersState = new AuthorizedMinersState(
                    (Dictionary)blockChain.GetState(AuthorizedMinersState.Address)
                    );
            }

            var dateTimeOffset = DateTimeOffset.MinValue;

            // Index 1
            Assert.Equal(4096, policy.GetNextBlockDifficulty(blockChain));

            dateTimeOffset += TimeSpan.FromSeconds(1);
            await blockChain.MineBlock(miner, dateTimeOffset);

            // Index 2, target index
            Assert.Equal(4096, policy.GetNextBlockDifficulty(blockChain));

            dateTimeOffset += TimeSpan.FromSeconds(1);
            await blockChain.MineBlock(miner, dateTimeOffset);

            // Index 3
            Assert.Equal(4096, policy.GetNextBlockDifficulty(blockChain));

            dateTimeOffset += TimeSpan.FromSeconds(1);
            await blockChain.MineBlock(miner, dateTimeOffset);

            // Index 4, target index
            Assert.Equal(4096, policy.GetNextBlockDifficulty(blockChain));

            dateTimeOffset += TimeSpan.FromSeconds(1);
            await blockChain.MineBlock(miner, dateTimeOffset);

            // Index 5
            Assert.Equal(4096, policy.GetNextBlockDifficulty(blockChain));

            dateTimeOffset += TimeSpan.FromSeconds(1);
            await blockChain.MineBlock(miner, dateTimeOffset);

            // Index 6, target index
            Assert.Equal(4096, policy.GetNextBlockDifficulty(blockChain));

            dateTimeOffset += TimeSpan.FromSeconds(1);
            await blockChain.MineBlock(miner, dateTimeOffset);

            // Index 7
            Assert.Equal(4098, policy.GetNextBlockDifficulty(blockChain));

            dateTimeOffset += TimeSpan.FromSeconds(1);
            await blockChain.MineBlock(miner, dateTimeOffset);

            // Index 8
            Assert.Equal(4100, policy.GetNextBlockDifficulty(blockChain));

            dateTimeOffset += TimeSpan.FromSeconds(1);
            await blockChain.MineBlock(miner, dateTimeOffset);

            // Index 9
            Assert.Equal(4102, policy.GetNextBlockDifficulty(blockChain));

            dateTimeOffset += TimeSpan.FromSeconds(1);
            await blockChain.MineBlock(miner, dateTimeOffset);

            // Index 10
            Assert.Equal(4104, policy.GetNextBlockDifficulty(blockChain));

            dateTimeOffset += TimeSpan.FromSeconds(1);
            await blockChain.MineBlock(miner, dateTimeOffset);

            // Index 11
            Assert.Equal(4106, policy.GetNextBlockDifficulty(blockChain));

            dateTimeOffset += TimeSpan.FromSeconds(20);
            await blockChain.MineBlock(miner, dateTimeOffset);

            // Index 12
            Assert.Equal(4104, policy.GetNextBlockDifficulty(blockChain));
        }

        [Fact]
        public void ValidateNextBlockWithManyTransactions()
        {
            var adminPrivateKey = new PrivateKey();
            var adminAddress = new Address(adminPrivateKey.PublicKey);
            var blockPolicySource = new BlockPolicySource(Logger.None);
            IBlockPolicy<PolymorphicAction<ActionBase>> policy = blockPolicySource.GetPolicy(3000, 10);
            Block<PolymorphicAction<ActionBase>> genesis = MakeGenesisBlock(adminAddress, ImmutableHashSet<Address>.Empty);

            using var store = new DefaultStore(null);
            var blockChain = new BlockChain<PolymorphicAction<ActionBase>>(
                policy,
                store,
                store,
                genesis
            );

            int nonce = 0;
            List<Transaction<PolymorphicAction<ActionBase>>> GenerateTransactions(int count)
            {
                var list = new List<Transaction<PolymorphicAction<ActionBase>>>();
                for (int i = 0; i < count; i++)
                {
                    list.Add(Transaction<PolymorphicAction<ActionBase>>.Create(
                        nonce++,
                        adminPrivateKey,
                        genesis.Hash,
                        new PolymorphicAction<ActionBase>[] { }
                    ));
                }

                return list;
            }

            Assert.Equal(1, blockChain.Count);
            Block<PolymorphicAction<ActionBase>> block1 = Block<PolymorphicAction<ActionBase>>.Mine(
                index: 1,
                difficulty: policy.GetNextBlockDifficulty(blockChain),
                previousTotalDifficulty: blockChain.Tip.TotalDifficulty,
                miner: adminAddress,
                previousHash: blockChain.Tip.Hash,
                timestamp: DateTimeOffset.MinValue,
                transactions: GenerateTransactions(5));
            blockChain.Append(block1);
            Assert.Equal(2, blockChain.Count);
            Assert.True(blockChain.ContainsBlock(block1.Hash));
            Block<PolymorphicAction<ActionBase>> block2 = Block<PolymorphicAction<ActionBase>>.Mine(
                index: 2,
                difficulty: policy.GetNextBlockDifficulty(blockChain),
                previousTotalDifficulty: blockChain.Tip.TotalDifficulty,
                miner: adminAddress,
                previousHash: blockChain.Tip.Hash,
                timestamp: DateTimeOffset.MinValue,
                transactions: GenerateTransactions(10));
            blockChain.Append(block2);
            Assert.Equal(3, blockChain.Count);
            Assert.True(blockChain.ContainsBlock(block2.Hash));
            Block<PolymorphicAction<ActionBase>> block3 = Block<PolymorphicAction<ActionBase>>.Mine(
                index: 3,
                difficulty: policy.GetNextBlockDifficulty(blockChain),
                previousTotalDifficulty: blockChain.Tip.TotalDifficulty,
                miner: adminAddress,
                previousHash: blockChain.Tip.Hash,
                timestamp: DateTimeOffset.MinValue,
                transactions: GenerateTransactions(11));
            Assert.Throws<InvalidTxCountException>(() => blockChain.Append(block3));
            Assert.Equal(3, blockChain.Count);
            Assert.False(blockChain.ContainsBlock(block3.Hash));
        }

        private Block<PolymorphicAction<ActionBase>> MakeGenesisBlock(
            Address adminAddress,
            IImmutableSet<Address> activatedAddresses,
            AuthorizedMinersState authorizedMinersState = null,
            DateTimeOffset? timestamp = null
        )
        {
            var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
            var privateKey = new PrivateKey();
            (ActivationKey activationKey, PendingActivationState pendingActivation) =
                ActivationKey.Create(privateKey, nonce);

            return BlockChain<PolymorphicAction<ActionBase>>.MakeGenesisBlock(
                    new PolymorphicAction<ActionBase>[]
                    {
                        new InitializeStates(
                            rankingState: new RankingState(),
                            shopState: new ShopState(),
                            tableSheets: TableSheetsImporter.ImportSheets(),
                            gameConfigState: new GameConfigState(),
                            redeemCodeState: new RedeemCodeState(Dictionary.Empty
                                .Add("address", RedeemCodeState.Address.Serialize())
                                .Add("map", Dictionary.Empty)
                            ),
                            adminAddressState: new AdminState(adminAddress, 1500000),
                            activatedAccountsState: new ActivatedAccountsState(activatedAddresses),
                            goldCurrencyState: new GoldCurrencyState(
                                new Currency("NCG", 2, minter: null)
                            ),
                            goldDistributions: new GoldDistribution[0],
                            pendingActivationStates: new[] { pendingActivation },
                            authorizedMinersState: authorizedMinersState
                        ),
                    },
                    timestamp: timestamp ?? DateTimeOffset.MinValue
                );
        }
    }
}
