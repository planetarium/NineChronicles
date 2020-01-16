using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blocks;
using Nekoyume.Action;
using Nekoyume.State;
using Nekoyume.TableData;
using UnityEngine;

using Libplanet.Blockchain;

namespace Nekoyume.BlockChain
{
    public static class BlockHelper
    {
        // Editor가 아닌 환경에서 사용할 제네시스 블록의 파일명입니다.
        // 만약 이 값을 수정할 경우 entrypoint.sh도 같이 수정할 필요가 있습니다.
        public const string GenesisBlockNameProd = "genesis-block";

        // Editor 환경에서 사용할 제네시스 블록의 파일명입니다.
        public const string GenesisBlockNameDev = "genesis-block-dev";

        public static string GenesisBlockPathProd => BlockPath(GenesisBlockNameProd);

        public static string GenesisBlockPathDev => BlockPath(GenesisBlockNameDev);

        /// <summary>
        /// 블록은 인코딩하여 파일로 내보냅니다.
        /// </summary>
        /// <param name="path">블록이 저장될 파일경로.</param>
        public static void ExportBlock(
            Block<PolymorphicAction<ActionBase>> block,
            string path)
        {
            byte[] encoded = block.Serialize();
            File.WriteAllBytes(path, encoded);
        }

        /// <summary>
        /// 파일로 부터 블록을 읽어옵니다.
        /// </summary>
        /// <param name="path">블록이 저장되어있는 파일경로.</param>
        /// <returns>읽어들인 블록 객체.</returns>
        public static Block<PolymorphicAction<ActionBase>> ImportBlock(string path)
        {
            if (File.Exists(path))
            {
                var buffer = File.ReadAllBytes(path);
                return Block<PolymorphicAction<ActionBase>>.Deserialize(buffer);
            }
            else
            {
                return null;
            }
        }

        public static Block<PolymorphicAction<ActionBase>> MineGenesisBlock()
        {
            var tableSheets = TableSheets.GetTableCsvAssets();
            var initialStatesAction = new InitializeStates
            {
                RankingState = new RankingState(),
                ShopState = new ShopState(),
                TableSheetsState = new TableSheetsState(tableSheets),
            };
            var actions = new PolymorphicAction<ActionBase>[]
            {
                initialStatesAction,
            };
            return
                BlockChain<PolymorphicAction<ActionBase>>.MakeGenesisBlock(actions);
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

        public static string BlockPath(string filename) => Path.Combine(Application.streamingAssetsPath, filename);

        // Copied from Planetarium.Nekoyume.LibplanetEditor.
        public static void DeleteAllEditor()
        {
            var path = StorePath.GetDefaultStoragePath(StorePath.Env.Development);
            DeleteAll(path);
        }
        
        private static void DeleteAll(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
    }
}
