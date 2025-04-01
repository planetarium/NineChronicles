using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using Bencodex;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Libplanet.Crypto;
using Libplanet.Types.Consensus;
using Nekoyume.Action;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Blockchain
{
    using Libplanet.Types.Blocks;
    public static class BlockManager
    {
        // Editor가 아닌 환경에서 사용할 제네시스 블록의 파일명입니다.
        // 만약 이 값을 수정할 경우 entrypoint.sh도 같이 수정할 필요가 있습니다.
        public const string GenesisBlockName = "genesis-block";

        private static readonly Codec _codec = new();

        public static string GenesisBlockPath()
        {
            // Android should use correct path.
#if UNITY_ANDROID && RUN_ON_MOBILE
            String dataPath = Platform.GetPersistentDataPath(GenesisBlockName);
            return dataPath;
#endif
            return BlockPath(GenesisBlockName);
        }

        /// <summary>
        /// 블록은 인코딩하여 파일로 내보냅니다.
        /// </summary>
        /// <param name="path">블록이 저장될 파일경로.</param>
        public static void ExportBlock(Block block, string path)
        {
            var dict = block.MarshalBlock();
            var encoded = _codec.Encode(dict);
            File.WriteAllBytes(path, encoded);
        }

        /// <summary>
        /// 파일로 부터 블록을 읽어옵니다.
        /// </summary>
        /// <param name="path">블록이 저장되어있는 파일경로.</param>
        /// <returns>읽어들인 블록 객체.</returns>
        public static Block ImportBlock(string path)
        {
            // read temp genesis-block
#if UNITY_ANDROID
            WWW www = new WWW(Platform.GetStreamingAssetsPath("genesis-block"));
            while (!www.isDone)
            {
                //wait
            }

            byte[] buffer = www.bytes;
            Bencodex.Types.Dictionary dict = (Bencodex.Types.Dictionary)_codec.Decode(buffer);

            return BlockMarshaler.UnmarshalBlock(dict);
#else
            if (File.Exists(path))
            {
                var buffer = File.ReadAllBytes(path);
                var dict = (Dictionary)_codec.Decode(buffer);

                return BlockMarshaler.UnmarshalBlock(dict);
            }

            var uri = new Uri(path);
            using (var client = new WebClient())
            {
                var rawGenesisBlock = client.DownloadData(uri);
                var dict = (Dictionary)_codec.Decode(rawGenesisBlock);
                return BlockMarshaler.UnmarshalBlock(dict);
            }
#endif
        }

        public static async UniTask<Block> ImportBlockAsync(string path)
        {
            var uri = new Uri(path);
            // NOTE: Check cached genesis block in local path.
            string localPath;
            if (uri.IsFile)
            {
                localPath = path;
            }
            else if (uri.AbsolutePath.Equals("/genesis-block-9c-main"))
            {
                localPath = Platform.GetStreamingAssetsPath("planets/0x000000000000/genesis");
            }
            else if (uri.AbsolutePath.StartsWith("/planets/0x"))
            {
                localPath = Platform.GetStreamingAssetsPath(uri.LocalPath[1..]);
            }
            else
            {
                localPath = path;
            }

            NcDebug.Log($"[BlockManager] localPath: {localPath}");

#if UNITY_ANDROID
            // NOTE: UnityWebRequest requires to be called in main thread.
            await UniTask.SwitchToMainThread();
            using var loadingRequest = UnityWebRequest.Get(localPath);
            try
            {
                await loadingRequest.SendWebRequest();
                if (loadingRequest.result == UnityWebRequest.Result.Success)
                {
                    NcDebug.Log($"[BlockManager] Load genesis block from local path via UnityWebRequest. {localPath}");
                    var buffer = loadingRequest.downloadHandler.data;
                    var dict = (Dictionary)_codec.Decode(buffer);
                    return BlockMarshaler.UnmarshalBlock(dict);
                }
            }
            catch (Exception e)
            {
                NcDebug.LogError($"[BlockManager] Failed to load genesis block from local path via UnityWebRequest. {localPath}");
                NcDebug.LogError(e);
            }
#else
            if (File.Exists(localPath))
            {
                NcDebug.Log($"[BlockManager] Load genesis block from local path via File IO. {localPath}");
                var buffer = await File.ReadAllBytesAsync(localPath);
                var dict = (Dictionary)_codec.Decode(buffer);
                return BlockMarshaler.UnmarshalBlock(dict);
            }
#endif

            using var client = new WebClient();
            {
                NcDebug.Log($"[BlockManager] Load genesis block from remote path via WebClient. {uri.AbsoluteUri}");
                var rawGenesisBlock = await client.DownloadDataTaskAsync(uri);
                var dict = (Dictionary)_codec.Decode(rawGenesisBlock);
                return BlockMarshaler.UnmarshalBlock(dict);
            }
        }

        public static Block ProposeGenesisBlock(
            PendingActivationState[] pendingActivationStates,
            [CanBeNull] PrivateKey proposer)
        {
            var tableSheets = Game.Game.GetTableCsvAssets();
            var goldDistributionCsvPath = Platform.GetStreamingAssetsPath("GoldDistribution.csv");
            var goldDistributions =
                GoldDistribution.LoadInDescendingEndBlockOrder(goldDistributionCsvPath);
            var initialValidatorSet = new Dictionary<PublicKey, BigInteger>();
            if (proposer is not null)
            {
                initialValidatorSet[proposer.PublicKey] = 10_000_000_000_000_000_000;
            }

            tableSheets.Add("RedeemCodeListSheet", "id,reward_id,public_key");

            return BlockHelper.ProposeGenesisBlock(
                new ValidatorSet(initialValidatorSet.Select(v => new Validator(v.Key, v.Value)).ToList()),
                tableSheets,
                goldDistributions,
                pendingActivationStates,
                new AdminState(new Address("F9A15F870701268Bd7bBeA6502eB15F4997f32f9"), 1500000),
                isActivateAdminAddress: false,
                privateKey:proposer);
        }

        public static string BlockPath(string filename)
        {
            return Platform.GetStreamingAssetsPath(filename);
        }
    }
}
