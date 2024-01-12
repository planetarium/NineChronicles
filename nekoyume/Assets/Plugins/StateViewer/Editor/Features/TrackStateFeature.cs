using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Bencodex.Types;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Cysharp.Threading.Tasks;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Libplanet.Types.Blocks;
using Nekoyume.Model.State;
using StateViewer.Runtime;
using UnityEditor;
using UnityEngine;

namespace StateViewer.Editor.Features
{
    [Serializable]
    public class TrackStateFeature : IStateViewerFeature
    {
        private enum UIState
        {
            Idle,
            Tracking,
        }

        public class SourceCsvRow
        {
            [Index(0), Name("index")]
            public int Index { get; set; }

            [Index(1), Name("block_index")]
            public int BlockIndex { get; set; }

            [Index(2), Name("block_hash")]
            public string BlockHash { get; set; }

            [Index(3), Name("tx_id")]
            public string TxId { get; set; }

            [Index(4), Name("signer_addr")]
            public string SignerAddr { get; set; }

            [Index(5), Name("prev_block_index")]
            public int PrevBlockIndex { get; set; }

            [Index(6), Name("prev_block_hash")]
            public string PrevBlockHash { get; set; }

            public override string ToString() =>
                $"Index: {Index}, BlockIndex: {BlockIndex}, BlockHash: {BlockHash}" +
                $", TxId: {TxId}, SignerAddr: {SignerAddr}, PrevBlockIndex: {PrevBlockIndex}" +
                $", PrevBlockHash: {PrevBlockHash}";
        }

        public class OutputCsvRow : SourceCsvRow
        {
            [Index(7), Name("stake_balance_addr")]
            public string StakeBalanceAddr { get; set; }

            [Index(8), Name("prev_stake_balance")]
            public string PrevStakeBalance { get; set; }

            [Index(9), Name("stake_state_addr")]
            public string StakeStateAddr { get; set; }

            [Index(10), Name("prev_stake_started_block_index")]
            public long PrevStakeStartedBlockIndex { get; set; } = -1L;

            [Index(11), Name("prev_stake_received_block_index")]
            public long PrevStakeReceivedBlockIndex { get; set; } = -1L;

            [Index(12), Name("v4_item_reward_v1_step")]
            public int V4ItemRewardV1Step { get; set; } = -1;

            [Index(13), Name("v4_item_reward_v2_step")]
            public int V4ItemRewardV2Step { get; set; } = -1;

            [Index(14), Name("v5_item_reward_v1_step")]
            public int V5ItemRewardV1Step { get; set; } = -1;

            [Index(15), Name("v5_item_reward_v2_step")]
            public int V5ItemRewardV2Step { get; set; } = -1;

            [Index(16), Name("v4_rune_reward_v1_step")]
            public int V4RuneRewardV1Step { get; set; } = -1;

            [Index(17), Name("v4_rune_reward_v2_step")]
            public int V4RuneRewardV2Step { get; set; } = -1;

            [Index(18), Name("v5_rune_reward_v1_step")]
            public int V5RuneRewardV1Step { get; set; } = -1;

            [Index(19), Name("v5_rune_reward_v2_step")]
            public int V5RuneRewardV2Step { get; set; } = -1;

            [Index(20), Name("v4_currency_reward_v1_step")]
            public int V4CurrencyRewardV1Step { get; set; } = -1;

            [Index(21), Name("v4_currency_reward_v2_step")]
            public int V4CurrencyRewardV2Step { get; set; } = -1;

            [Index(22), Name("v5_currency_reward_v1_step")]
            public int V5CurrencyRewardV1Step { get; set; } = -1;

            [Index(23), Name("v5_currency_reward_v2_step")]
            public int V5CurrencyRewardV2Step { get; set; } = -1;
        }

        private string _headerMsg =
            "This feature only supports the specific case now." +
            "\nThe source CSV must have the following columns: " +
            "\n\tindex, block_index, block_hash, tx_id, signer_addr, " +
            "prev_block_index, prev_block_hash" +
            "\n\n See also: https://gist.github.com/boscohyun/7ea824c8264920f1eb650b6ee66360c5";

        private UIState _uiState = UIState.Idle;

        private readonly StateViewerWindow _editorWindow;

        private readonly Currency _ncg;

        private Vector2 _scrollPosition;

        [SerializeField]
        private string sourceCsv;

        [SerializeField]
        private string outputFilePath;

        [SerializeField]
        private bool logToConsole;

        public TrackStateFeature(StateViewerWindow editorWindow)
        {
            _editorWindow = editorWindow;
#pragma warning disable CS0618
            _ncg = Currency.Legacy(
                "NCG",
                2,
                new Address("0x47D082a115c63E7b58B1532d20E631538eaFADde"));
#pragma warning restore CS0618
            sourceCsv = string.Empty;
            outputFilePath = Path.Combine(Application.streamingAssetsPath, "tracked-output.csv");
        }

        public void OnGUI()
        {
            EditorGUILayout.HelpBox(_headerMsg, MessageType.Info);

            if (_uiState == UIState.Tracking)
            {
                EditorGUILayout.LabelField("Tracking now...");
                return;
            }

            GUILayout.Label("Source CSV", EditorStyles.boldLabel);
            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            sourceCsv = EditorGUILayout.TextArea(sourceCsv, GUILayout.Height(200));
            EditorGUILayout.EndScrollView();
            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

            var trackButtonDisable = string.IsNullOrEmpty(sourceCsv);
            outputFilePath = EditorGUILayout.TextField("Output File Path", outputFilePath);
            if (string.IsNullOrEmpty(outputFilePath))
            {
                trackButtonDisable = !logToConsole;
            }
            else if (Path.HasExtension(outputFilePath))
            {
                if (Path.GetExtension(outputFilePath) != ".csv")
                {
                    trackButtonDisable = true;
                    EditorGUILayout.HelpBox(
                        "Output file path must be a CSV file.",
                        MessageType.Error);
                }
            }
            else
            {
                trackButtonDisable = true;
                EditorGUILayout.HelpBox("Output file path must be a CSV file.", MessageType.Error);
            }

            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

            logToConsole = EditorGUILayout.Toggle("Log To Console", logToConsole);
            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

            var stateProxy = _editorWindow.GetStateProxy(drawHelpBox: true);
            EditorGUI.BeginDisabledGroup(_uiState == UIState.Tracking || trackButtonDisable);
            if (GUILayout.Button("Track"))
            {
                TrackAsync(stateProxy).Forget();
            }

            EditorGUI.EndDisabledGroup();
        }

        private async UniTaskVoid TrackAsync(StateProxy stateProxy)
        {
            _uiState = UIState.Tracking;

            sourceCsv = sourceCsv.Trim();
            using var reader = new StringReader(sourceCsv);
            using var csvReader = new CsvHelper.CsvReader(
                reader,
                new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                });
            var sourceCsvRows = csvReader.GetRecords<SourceCsvRow>();
            var rows = sourceCsvRows as SourceCsvRow[] ?? sourceCsvRows.ToArray();
            if (string.IsNullOrEmpty(outputFilePath))
            {
                if (logToConsole)
                {
                    foreach (var row in rows)
                    {
                        Debug.Log(row.ToString());
                    }
                }

                return;
            }

            using var writer = new StreamWriter(outputFilePath);
            using var csvWriter = new CsvHelper.CsvWriter(
                writer,
                new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                });
            csvWriter.WriteHeader<OutputCsvRow>();
            await csvWriter.NextRecordAsync();
            foreach (var sourceCsvRow in rows)
            {
                if (logToConsole)
                {
                    Debug.Log(sourceCsvRow.ToString());
                }

                if (string.IsNullOrEmpty(outputFilePath))
                {
                    continue;
                }

                var prevBlockHash = BlockHash.FromString(sourceCsvRow.PrevBlockHash);
                var signerAddr = new Address(sourceCsvRow.SignerAddr);
                var stakeAddr = StakeState.DeriveAddress(signerAddr);
                try
                {
                    var (_, stakeBalance) = stateProxy is null
                        ? ((Address?)null, (FungibleAssetValue?)null)
                        : await stateProxy.GetBalanceAsync(
                            prevBlockHash,
                            stakeAddr,
                            _ncg);
                    var (_, _, stakeStateValue) = stateProxy is null
                        ? ((Address?)null, (Address?)null, (IValue)null)
                        : await stateProxy.GetStateAsync(
                            prevBlockHash,
                            ReservedAddresses.LegacyAccount,
                            stakeAddr);
                    var rowToWrite = ToOutputCsvRow(
                        sourceCsvRow,
                        stakeAddr,
                        stakeBalance,
                        stakeAddr,
                        stakeStateValue);
                    csvWriter.WriteRecord(rowToWrite);
                    await csvWriter.NextRecordAsync();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    _uiState = UIState.Idle;
                    break;
                }
            }

            _uiState = UIState.Idle;
        }

        private static OutputCsvRow ToOutputCsvRow(
            SourceCsvRow sourceCsvRow,
            Address stakeBalanceAddr,
            FungibleAssetValue? stakeBalance,
            Address stakeStateAddr,
            IValue stakeStateValue)
        {
            if (stakeStateValue is not Dictionary dict)
            {
                return new OutputCsvRow
                {
                    Index = sourceCsvRow.Index,
                    BlockIndex = sourceCsvRow.BlockIndex,
                    BlockHash = sourceCsvRow.BlockHash,
                    TxId = sourceCsvRow.TxId,
                    SignerAddr = sourceCsvRow.SignerAddr,
                    PrevBlockIndex = sourceCsvRow.PrevBlockIndex,
                    PrevBlockHash = sourceCsvRow.PrevBlockHash,
                    StakeBalanceAddr = stakeBalanceAddr.ToString(),
                    PrevStakeBalance = stakeBalance?.GetQuantityString() ?? "0",
                    StakeStateAddr = stakeStateAddr.ToString(),
                };
            }

            var stakeState = new StakeState(dict);
#pragma warning disable CS0618
            stakeState.CalculateAccumulatedItemRewardsV2(
                sourceCsvRow.BlockIndex,
                out var v4ItemV1Step,
                out var v4ItemV2Step);
            stakeState.CalculateAccumulatedItemRewardsV3(
                sourceCsvRow.BlockIndex,
                out var v5ItemV1Step,
                out var v5ItemV2Step);
            stakeState.CalculateAccumulatedRuneRewardsV2(
                sourceCsvRow.BlockIndex,
                out var v4RuneV1Step,
                out var v4RuneV2Step);
            stakeState.CalculateAccumulatedRuneRewardsV3(
                sourceCsvRow.BlockIndex,
                out var v5RuneV1Step,
                out var v5RuneV2Step);
            stakeState.CalculateAccumulatedCurrencyRewardsV1(
                sourceCsvRow.BlockIndex,
                out var v4CurrencyV1Step,
                out var v4CurrencyV2Step);
            stakeState.CalculateAccumulatedCurrencyRewardsV2(
                sourceCsvRow.BlockIndex,
                out var v5CurrencyV1Step,
                out var v5CurrencyV2Step);
#pragma warning restore CS0618
            return new OutputCsvRow
            {
                Index = sourceCsvRow.Index,
                BlockIndex = sourceCsvRow.BlockIndex,
                BlockHash = sourceCsvRow.BlockHash,
                TxId = sourceCsvRow.TxId,
                SignerAddr = sourceCsvRow.SignerAddr,
                PrevBlockIndex = sourceCsvRow.PrevBlockIndex,
                PrevBlockHash = sourceCsvRow.PrevBlockHash,
                StakeBalanceAddr = stakeBalanceAddr.ToString(),
                PrevStakeBalance = stakeBalance?.GetQuantityString() ?? "",
                StakeStateAddr = stakeStateAddr.ToString(),
                PrevStakeStartedBlockIndex = stakeState.StartedBlockIndex,
                PrevStakeReceivedBlockIndex = stakeState.ReceivedBlockIndex,
                V4ItemRewardV1Step = v4ItemV1Step,
                V4ItemRewardV2Step = v4ItemV2Step,
                V5ItemRewardV1Step = v5ItemV1Step,
                V5ItemRewardV2Step = v5ItemV2Step,
                V4RuneRewardV1Step = v4RuneV1Step,
                V4RuneRewardV2Step = v4RuneV2Step,
                V5RuneRewardV1Step = v5RuneV1Step,
                V5RuneRewardV2Step = v5RuneV2Step,
                V4CurrencyRewardV1Step = v4CurrencyV1Step,
                V4CurrencyRewardV2Step = v4CurrencyV2Step,
                V5CurrencyRewardV1Step = v5CurrencyV1Step,
                V5CurrencyRewardV2Step = v5CurrencyV2Step,
            };
        }
    }
}
