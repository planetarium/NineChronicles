#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Bencodex;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Lib9c;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Libplanet.Types.Blocks;
using Nekoyume.Blockchain;
using StateViewer.Runtime;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Boolean = Bencodex.Types.Boolean;

namespace StateViewer.Editor.Features
{
    [Serializable]
    public class StateAndBalanceFeature : IStateViewerFeature
    {
        public enum SourceFrom
        {
            Base64EncodedBencodexValue,
            HexEncodedBencodexValue,
            GetStateFromPlayModeAgent,
            // GetStateFromRPCServer,
        }

        private static readonly Codec Codec = new();

        public static readonly IValue[] TestValues =
        {
            Null.Value,
            new Binary("test", Encoding.UTF8),
            new Boolean(true),
            new Integer(100),
            new Text("test"),
            new List(
                (Text)"element at index 0",
                new List(
                    (Text)"element at index 0",
                    (Text)"element at index 1"),
                Dictionary.Empty
                    .SetItem("key1", 1)
                    .SetItem("key2", 2)),
            Dictionary.Empty
                .SetItem("key1", 1)
                .SetItem("key2", new List(
                    (Text)"element at index 0",
                    (Text)"element at index 1"))
                .SetItem("key3", Dictionary.Empty
                    .SetItem("key1", 1)
                    .SetItem("key2", 2)),
            new Address("0x0123456789012345678901234567890123456789").Bencoded,
        };

        private readonly StateViewerWindow _editorWindow;

        [SerializeField]
        private SourceFrom sourceFrom;

        [SerializeField]
        private string encodedBencodexValue;

        private SearchField _addrSearchField;

        [SerializeField]
        private string searchingAddrStr;

        [SerializeField]
        private string blockHashOrIndex;

        [SerializeField]
        private bool useTestValues;

        [SerializeField]
        private StateTreeView stateTreeView;

        [SerializeField]
        private Vector2 stateTreeViewScrollPosition;

        [SerializeField]
        private string ncgValue;

        [SerializeField]
        private string crystalValue;

        private bool _loadingSomething;

        public StateAndBalanceFeature(StateViewerWindow editorWindow)
        {
            _editorWindow = editorWindow;
            sourceFrom = SourceFrom.GetStateFromPlayModeAgent;
            encodedBencodexValue = string.Empty;
            searchingAddrStr = string.Empty;
            blockHashOrIndex = string.Empty;
            stateTreeViewScrollPosition = Vector2.zero;
            ncgValue = string.Empty;
            crystalValue = string.Empty;

            stateTreeView = new StateTreeView(
                tableSheets: editorWindow.GetTableSheets()!,
                contentKind: ContentKind.None);
            _addrSearchField = new SearchField();
            _addrSearchField.downOrUpArrowKeyPressed +=
                stateTreeView.SetFocusAndEnsureSelectedItem;
            ClearAll();
        }

        private void ClearAll()
        {
            encodedBencodexValue = string.Empty;
            searchingAddrStr = string.Empty;
            blockHashOrIndex = string.Empty;
            stateTreeView.ClearData();
            stateTreeViewScrollPosition = Vector2.zero;
            ncgValue = string.Empty;
            crystalValue = string.Empty;
        }

        public void OnGUI()
        {
            GUILayout.Label("State", EditorStyles.boldLabel);
            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
            DrawInputs();
            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
            DrawStateTreeView();
            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
            DrawSaveButton();
            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
            DrawHorizontalLine();
            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
            DrawBalances();
        }

        private static Rect GetRect(
            float? minWidth = null,
            int? minLineCount = null,
            float? maxHeight = null)
        {
            var minHeight = minLineCount.HasValue
                ? EditorGUIUtility.singleLineHeight * minLineCount.Value +
                  EditorGUIUtility.standardVerticalSpacing * (minLineCount.Value - 1)
                : EditorGUIUtility.singleLineHeight;
            maxHeight ??= minHeight;

            return GUILayoutUtility.GetRect(
                minWidth ?? 10f,
                10f,
                minHeight,
                maxHeight.Value,
                GUILayout.ExpandWidth(true));
        }

        private static void DrawHorizontalLine()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }

        private void DrawInputs()
        {
            var sourceFromNew = (SourceFrom)EditorGUILayout.EnumPopup(
                "Source From",
                sourceFrom);
            if (sourceFromNew != sourceFrom)
            {
                sourceFrom = sourceFromNew;
                ClearAll();
            }

            switch (sourceFrom)
            {
                case SourceFrom.Base64EncodedBencodexValue:
                case SourceFrom.HexEncodedBencodexValue:
                    DrawInputsForSourceFromEncodedBencodexValue();
                    break;
                case SourceFrom.GetStateFromPlayModeAgent:
                    DrawInputsForSourceFromGetStateFromPlayModeAgent();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void DrawInputsForSourceFromEncodedBencodexValue()
        {
            encodedBencodexValue = EditorGUILayout.TextField(
                "Encoded Bencodex Value",
                encodedBencodexValue);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUI.BeginDisabledGroup(
                string.IsNullOrEmpty(encodedBencodexValue));
            if (GUILayout.Button("Decode"))
            {
                var binary = sourceFrom switch
                {
                    SourceFrom.Base64EncodedBencodexValue =>
                        Binary.FromBase64(encodedBencodexValue),
                    SourceFrom.HexEncodedBencodexValue =>
                        Binary.FromHex(encodedBencodexValue),
                    SourceFrom.GetStateFromPlayModeAgent or _ =>
                        throw new ArgumentOutOfRangeException(),
                };
                var value = Codec.Decode(binary);
                stateTreeView.SetData(null, value);
            }

            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();
        }

        private void DrawInputsForSourceFromGetStateFromPlayModeAgent()
        {
            StateProxy? stateProxy = null;
            if (Application.isPlaying)
            {
                stateProxy = _editorWindow.GetStateProxy(drawHelpBox: true);
                if (stateProxy is not null)
                {
                    DrawAddrSearchField();
                    DrawBlockHashOrIndexField();
                }
            }
            else
            {
                DrawTestValues();
            }

            stateTreeView.SetContentKind((ContentKind)EditorGUILayout.EnumPopup(
                "Content Kind",
                stateTreeView.ContentKind));

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUI.BeginDisabledGroup(
                stateProxy is null ||
                string.IsNullOrEmpty(searchingAddrStr) ||
                _loadingSomething ||
                !StateViewerWindow.IsSavable);
            if (GUILayout.Button("Get State Async") &&
                stateProxy is not null)
            {
                GetStateAndUpdateStateTreeViewAsync(
                    stateProxy,
                    searchingAddrStr).Forget();
            }

            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();
        }

        private void DrawAddrSearchField()
        {
            EditorGUILayout.BeginHorizontal();
            var rect = EditorGUILayout.GetControlRect(
                hasLabel: true,
                height: EditorGUIUtility.singleLineHeight);
            rect = EditorGUI.PrefixLabel(rect, new GUIContent("Address"));
            searchingAddrStr = _addrSearchField.OnGUI(rect, searchingAddrStr);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawBlockHashOrIndexField()
        {
            EditorGUILayout.BeginHorizontal();
            blockHashOrIndex = EditorGUILayout.TextField(
                "Block Hash or Index",
                blockHashOrIndex);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTestValues()
        {
            useTestValues = EditorGUILayout.Toggle("Use Test Values", useTestValues);
            if (!useTestValues)
            {
                return;
            }

            EditorGUILayout.BeginHorizontal();
            for (var i = 0; i < TestValues.Length; i++)
            {
                var testValue = TestValues[i];
                if (GUILayout.Button($"{i}: {testValue.Kind}"))
                {
                    stateTreeView.SetData(default, testValue);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawStateTreeView()
        {
            stateTreeViewScrollPosition = GUILayout.BeginScrollView(
                stateTreeViewScrollPosition,
                false,
                true);
            stateTreeView.OnGUI(GetRect(
                minLineCount: 5,
                maxHeight: _editorWindow.position.height));
            GUILayout.EndScrollView();
        }

        private void DrawSaveButton()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUI.BeginDisabledGroup(stateTreeView.Address is null ||
                                         !StateViewerWindow.IsSavable);
            if (GUILayout.Button("Save", GUILayout.MaxWidth(50f)))
            {
                var (addr, value) = stateTreeView.Serialize();
                if (addr is null)
                {
                    Debug.LogWarning("Address is null.");
                    return;
                }

                var stateList = new List<(Address addr, IValue value)>
                {
                    (addr.Value, value),
                };
                ActionManager.Instance?.ManipulateState(stateList, null);
            }

            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();
        }

        private void DrawBalances()
        {
            GUILayout.Label("Balances", EditorStyles.boldLabel);

            // NCG
            EditorGUILayout.BeginHorizontal();
            ncgValue = EditorGUILayout.TextField("NCG", ncgValue);
            var ncg = _editorWindow.GetNCG();
            EditorGUI.BeginDisabledGroup(!StateViewerWindow.IsSavable || ncg is null);
            if (GUILayout.Button("Save", GUILayout.MaxWidth(50f)) &&
                ncg is { })
            {
                var balanceList = new List<(Address addr, FungibleAssetValue fav)>
                {
                    (
                        new Address(searchingAddrStr),
                        FungibleAssetValue.Parse(ncg.Value, ncgValue)
                    ),
                };
                ActionManager.Instance?.ManipulateState(null, balanceList);
            }

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            // CRYSTAL
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(!StateViewerWindow.IsSavable);
            crystalValue = EditorGUILayout.TextField("CRYSTAL", crystalValue);
            if (GUILayout.Button("Save", GUILayout.MaxWidth(50f)))
            {
                var balanceList = new List<(Address addr, FungibleAssetValue fav)>
                {
                    (new Address(searchingAddrStr),
                        FungibleAssetValue.Parse(Currencies.Crystal, crystalValue)),
                };
                ActionManager.Instance?.ManipulateState(null, balanceList);
            }

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }

        private async UniTaskVoid GetStateAndUpdateStateTreeViewAsync(
            StateProxy stateProxy,
            string addrStr)
        {
            if (string.IsNullOrEmpty(addrStr))
            {
                ClearAll();
                return;
            }

            var ncg = _editorWindow.GetNCG();
            if (!StateViewerWindow.IsSavable ||
                ncg is null)
            {
                return;
            }

            _loadingSomething = true;
            try
            {
                Address? addr;
                IValue? state;
                FungibleAssetValue? ncgFav;
                FungibleAssetValue? crystalFav;
                if (string.IsNullOrEmpty(blockHashOrIndex))
                {
                    (addr, state) = await stateProxy.GetStateAsync(addrStr);
                    (_, ncgFav) = stateProxy.GetBalance(addr!.Value, ncg.Value);
                    (_, crystalFav) = stateProxy.GetBalance(addr.Value, Currencies.Crystal);
                }
                else if (long.TryParse(blockHashOrIndex, out var blockIndex))
                {
                    (addr, state) = await stateProxy.GetStateAsync(addrStr, blockIndex);
                    (_, ncgFav) = await stateProxy.GetBalanceAsync(
                        addr!.Value,
                        ncg.Value,
                        blockIndex);
                    (_, crystalFav) = await stateProxy.GetBalanceAsync(
                        addr.Value,
                        Currencies.Crystal,
                        blockIndex);
                }
                else
                {
                    var blockHash = BlockHash.FromString(blockHashOrIndex);
                    (addr, state) = await stateProxy.GetStateAsync(addrStr, blockHash);
                    (_, ncgFav) = await stateProxy.GetBalanceAsync(
                        addr!.Value,
                        ncg.Value,
                        blockHash);
                    (_, crystalFav) = await stateProxy.GetBalanceAsync(
                        addr.Value,
                        Currencies.Crystal,
                        blockHash);
                }

                stateTreeView.SetData(addr, state);
                ncgValue = $"{ncgFav.Value.MajorUnit}.{ncgFav.Value.MinorUnit}";
                crystalValue = $"{crystalFav.Value.MajorUnit}.{crystalFav.Value.MinorUnit}";
            }
            catch (KeyNotFoundException)
            {
                ClearAll();
            }

            _loadingSomething = false;
            stateTreeView.SetFocusAndEnsureSelectedItem();
        }
    }
}
