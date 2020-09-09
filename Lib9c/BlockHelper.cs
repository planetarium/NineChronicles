using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Libplanet.Blockchain;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume
{
    public static class BlockHelper
    {
        public static Block<PolymorphicAction<ActionBase>> MineGenesisBlock(
            IDictionary<string, string> tableSheets,
            GoldDistribution[] goldDistributions)
        {
            if (!tableSheets.TryGetValue(nameof(GameConfigSheet), out var csv))
            {
                throw new KeyNotFoundException(nameof(GameConfigSheet));
            }
            var gameConfigState = new GameConfigState(csv);
            var redeemCodeListSheet = new RedeemCodeListSheet();
            redeemCodeListSheet.Set(tableSheets[nameof(RedeemCodeListSheet)]);


            // FIXME 메인넷때는 따로 지정해야합니다.
            var minterKey = new PrivateKey();
            var ncg = new Currency("NCG", 2, minterKey.ToAddress());
            var initialStatesAction = new InitializeStates
            {
                RankingState = new RankingState(),
                ShopState = new ShopState(),
                TableSheets = (Dictionary<string, string>) tableSheets,
                GameConfigState = gameConfigState,
                RedeemCodeState = new RedeemCodeState(redeemCodeListSheet),
                AdminAddressState = new AdminState(
                    new Address("F9A15F870701268Bd7bBeA6502eB15F4997f32f9"),
                    1500000
                ),
                ActivatedAccountsState = new ActivatedAccountsState(),
                GoldCurrencyState = new GoldCurrencyState(ncg),
                GoldDistributions = goldDistributions
            };
            var actions = new PolymorphicAction<ActionBase>[]
            {
                initialStatesAction,
            };
            return
                BlockChain<PolymorphicAction<ActionBase>>.MakeGenesisBlock(actions, privateKey: minterKey);
        }

        /// <summary>
        /// 블럭의 첫번째 액션의 <see cref="PolymorphicAction{T}.InnerAction"/> 내용을 기준으로 블록을 비교합니다.
        /// </summary>
        /// <param name="blockA">블록.</param>
        /// <param name="blockB">블록.</param>
        /// <returns>블록이 다르다면 true, 같다면 false를 반환합니다.</returns>
        public static bool CompareGenesisBlocks(Block<PolymorphicAction<ActionBase>> blockA,
            Block<PolymorphicAction<ActionBase>> blockB)
        {
            return blockA == null || blockB == null ||
                   !GetHashOfFirstAction(blockA).Equals(GetHashOfFirstAction(blockB));
        }

        /// <summary>
        /// 제네시스 블록에 포함되어 있는 <see cref="InitializeStates"/> 액션의
        /// <see cref="InitializeStates.PlainValue"/>로 부터 <see cref="HashDigest{T}"/> 값을 계산합니다.
        /// </summary>
        /// <param name="block"><see cref="InitializeStates"/> 액션만을 포함하고 있는 제네시스 블록.</param>
        /// <returns><see cref="InitializeStates"/> 액션의 <see cref="InitializeStates.PlainValue"/>
        /// 중 <see cref="GameAction.Id"/>를 제외하고 계산한 <see cref="HashDigest{T}"/>.</returns>
        private static HashDigest<SHA256> GetHashOfFirstAction(Block<PolymorphicAction<ActionBase>> block)
        {
            var initializeStatesAction = (InitializeStates)block.Transactions.First().Actions[0].InnerAction;
            Bencodex.Types.Dictionary plainValue = (Bencodex.Types.Dictionary) initializeStatesAction.PlainValue;
            plainValue = (Bencodex.Types.Dictionary) plainValue.Remove((Text)"id");  // except GameAction.Id.
            var bytes = plainValue.EncodeIntoChunks().SelectMany(b => b).ToArray();
            return Hashcash.Hash(bytes);
        }
    }
}
