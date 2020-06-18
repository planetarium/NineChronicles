namespace Lib9c.Tests
{
    using System.Collections.Immutable;
    using System.Threading.Tasks;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Blockchain;
    using Libplanet.Blockchain.Policies;
    using Libplanet.Blocks;
    using Libplanet.Crypto;
    using Libplanet.Store;
    using Libplanet.Tx;
    using Nekoyume.Action;
    using Nekoyume.BlockChain;
    using Nekoyume.Model.State;
    using Xunit;

    public class BlockPolicyTest
    {
        [Fact]
        public void DoesTransactionFollowsPolicyWithEmpty()
        {
            var adminPrivateKey = new PrivateKey();
            var adminAddress = new Address(adminPrivateKey.PublicKey);
            IBlockPolicy<PolymorphicAction<ActionBase>> policy = BlockPolicy.GetPolicy(10000);
            Block<PolymorphicAction<ActionBase>> genesis =
                BlockChain<PolymorphicAction<ActionBase>>.MakeGenesisBlock(
                    new PolymorphicAction<ActionBase>[]
                    {
                        new InitializeStates()
                        {
                            RankingState = new RankingState(),
                            ShopState = new ShopState(),
                            TableSheetsState = new TableSheetsState(),
                            WeeklyArenaAddresses = WeeklyArenaState.Addresses,
                            GameConfigState = new GameConfigState(),
                            RedeemCodeState = new RedeemCodeState(Dictionary.Empty
                                .Add("address", RedeemCodeState.Address.Serialize())
                                .Add("map", Dictionary.Empty)
                            ),
                            AdminAddressState = new AdminState(adminAddress, 1500000),
                            ActivatedAccountsState = new ActivatedAccountsState(),
                        },
                    }
                );
            using var store = new DefaultStore(null);
            _ = new BlockChain<PolymorphicAction<ActionBase>>(
                policy,
                store,
                genesis
            );
            Transaction<PolymorphicAction<ActionBase>> tx = Transaction<PolymorphicAction<ActionBase>>.Create(
                0,
                new PrivateKey(),
                genesis.Hash,
                new PolymorphicAction<ActionBase>[] { });

            Assert.True(policy.DoesTransactionFollowsPolicy(tx));
        }

        [Fact]
        public async Task DoesTransactionFollowsPolicy()
        {
            var adminPrivateKey = new PrivateKey();
            var adminAddress = adminPrivateKey.ToAddress();
            var activatedPrivateKey = new PrivateKey();
            var activatedAddress = activatedPrivateKey.ToAddress();

            IBlockPolicy<PolymorphicAction<ActionBase>> policy = BlockPolicy.GetPolicy(10000);
            Block<PolymorphicAction<ActionBase>> genesis =
                BlockChain<PolymorphicAction<ActionBase>>.MakeGenesisBlock(
                    new PolymorphicAction<ActionBase>[]
                    {
                        new InitializeStates()
                        {
                            RankingState = new RankingState(),
                            ShopState = new ShopState(),
                            TableSheetsState = new TableSheetsState(),
                            WeeklyArenaAddresses = WeeklyArenaState.Addresses,
                            GameConfigState = new GameConfigState(),
                            RedeemCodeState = new RedeemCodeState(Dictionary.Empty
                                .Add("address", RedeemCodeState.Address.Serialize())
                                .Add("map", Dictionary.Empty)
                            ),
                            AdminAddressState = new AdminState(adminAddress, 1500000),
                            ActivatedAccountsState = new ActivatedAccountsState(
                                new[]
                                {
                                    activatedAddress,
                                    adminAddress,
                                }.ToImmutableHashSet()
                            ),
                        },
                    }
                );
            using var store = new DefaultStore(null);
            var blockChain = new BlockChain<PolymorphicAction<ActionBase>>(
                policy,
                store,
                genesis
            );
            Transaction<PolymorphicAction<ActionBase>> txByStranger =
                Transaction<PolymorphicAction<ActionBase>>.Create(
                    0,
                    new PrivateKey(),
                    genesis.Hash,
                    new PolymorphicAction<ActionBase>[] { }
                );

            // 새로 만든 키는 활성화 유저 리스트에 없기 때문에 차단됩니다.
            Assert.False(policy.DoesTransactionFollowsPolicy(txByStranger));

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
            Assert.True(policy.DoesTransactionFollowsPolicy(txByNewActivated));
        }
    }
}
