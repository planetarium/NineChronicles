#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Bencodex;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Lib9c;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Helper;
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

        private SourceFrom _sourceFrom;

        private string _encodedBencodexValue;

        private SearchField _addrSearchField;
        private string _searchingAddrStr;

        [SerializeField]
        private bool useTestValues;

        private bool _loadingSomething;

        [SerializeField]
        private MultiColumnHeaderState stateTreeHeaderState;

        // SerializeField is used to ensure the view state is written to the window
        // layout file. This means that the state survives restarting Unity as long as the window
        // is not closed. If the attribute is omitted then the state is still serialized/deserialized.
        [SerializeField]
        private TreeViewState stateTreeViewState;

        private MultiColumnHeader _stateTreeHeader;
        private StateTreeView _stateTreeView;
        private Vector2 _stateTreeViewScrollPosition;

        private string _ncgValue;
        private string _crystalValue;

        // FIXME: TableSheets should be get from a state if needed for each time.
        private TableSheets _tableSheets;

        public StateAndBalanceFeature(StateViewerWindow editorWindow)
        {
            _editorWindow = editorWindow;
            _sourceFrom = SourceFrom.GetStateFromPlayModeAgent;
            _encodedBencodexValue = string.Empty;
            _searchingAddrStr = string.Empty;
            _stateTreeViewScrollPosition = Vector2.zero;
            _ncgValue = string.Empty;
            _crystalValue = string.Empty;
            _tableSheets = TableSheetsHelper.MakeTableSheets();

            stateTreeViewState ??= new TreeViewState();
            var indexOrKeyColumn = new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent("Index/Key"),
                headerTextAlignment = TextAlignment.Center,
                canSort = false,
                width = 100,
                minWidth = 100,
                autoResize = true,
                allowToggleVisibility = false,
            };
            var aliasColumn = new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent("Alias"),
                headerTextAlignment = TextAlignment.Center,
                canSort = false,
                width = 150,
                minWidth = 150,
                autoResize = true,
                allowToggleVisibility = true,
            };
            var valueKindColumn = new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent("ValueKind"),
                headerTextAlignment = TextAlignment.Center,
                canSort = false,
                width = 100,
                minWidth = 100,
                maxWidth = 100,
                autoResize = true,
                allowToggleVisibility = false,
            };
            var valueColumn = new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent("Value"),
                headerTextAlignment = TextAlignment.Center,
                canSort = false,
                width = 300,
                minWidth = 100,
                autoResize = true,
                allowToggleVisibility = false,
            };
            var addColumn = new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent("Add"),
                headerTextAlignment = TextAlignment.Center,
                canSort = false,
                width = 100,
                minWidth = 100,
                maxWidth = 100,
                autoResize = true,
                allowToggleVisibility = false,
            };
            var removeColumn = new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent("Remove"),
                headerTextAlignment = TextAlignment.Center,
                canSort = false,
                width = 100,
                minWidth = 100,
                maxWidth = 100,
                autoResize = true,
                allowToggleVisibility = false,
            };
            stateTreeHeaderState = new MultiColumnHeaderState(new[]
            {
                indexOrKeyColumn,
                aliasColumn,
                valueKindColumn,
                valueColumn,
                addColumn,
                removeColumn,
            });
            _stateTreeHeader = new MultiColumnHeader(stateTreeHeaderState);
            _stateTreeHeader.ResizeToFit();
            _stateTreeView = new StateTreeView(
                stateTreeViewState,
                _stateTreeHeader,
                _tableSheets);
            _addrSearchField = new SearchField();
            _addrSearchField.downOrUpArrowKeyPressed +=
                _stateTreeView.SetFocusAndEnsureSelectedItem;
            ClearAll();
        }

        private void ClearAll()
        {
            _encodedBencodexValue = string.Empty;
            _searchingAddrStr = string.Empty;
            _stateTreeView.ClearData();
            _stateTreeViewScrollPosition = Vector2.zero;
            _ncgValue = string.Empty;
            _crystalValue = string.Empty;
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
            var sourceFrom = (SourceFrom)EditorGUILayout.EnumPopup(
                "Source From",
                _sourceFrom);
            if (sourceFrom != _sourceFrom)
            {
                _sourceFrom = sourceFrom;
                ClearAll();
            }

            switch (_sourceFrom)
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
            _encodedBencodexValue = EditorGUILayout.TextField(
                "Encoded Bencodex Value",
                _encodedBencodexValue);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUI.BeginDisabledGroup(
                string.IsNullOrEmpty(_encodedBencodexValue));
            if (GUILayout.Button("Decode"))
            {
                var binary = _sourceFrom switch
                {
                    SourceFrom.Base64EncodedBencodexValue =>
                        Binary.FromBase64(_encodedBencodexValue),
                    SourceFrom.HexEncodedBencodexValue =>
                        Binary.FromHex(_encodedBencodexValue),
                    SourceFrom.GetStateFromPlayModeAgent or _ =>
                        throw new ArgumentOutOfRangeException(),
                };
                var value = Codec.Decode(binary);
                _stateTreeView.SetData(null, value);
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
                if (stateProxy is { })
                {
                    DrawAddrSearchField();
                }
            }
            else
            {
                DrawTestValues();
            }

            _stateTreeView.ContentKind = (ContentKind)EditorGUILayout.EnumPopup(
                "Content Kind",
                _stateTreeView.ContentKind);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUI.BeginDisabledGroup(
                stateProxy is null ||
                string.IsNullOrEmpty(_searchingAddrStr) ||
                _loadingSomething ||
                !StateViewerWindow.IsSavable);
            if (GUILayout.Button("Get State Async") &&
                stateProxy is { })
            {
                GetStateAndUpdateStateTreeViewAsync(
                    stateProxy,
                    _searchingAddrStr).Forget();
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
            _searchingAddrStr = _addrSearchField.OnGUI(rect, _searchingAddrStr);
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
                    _stateTreeView.SetData(default, testValue);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawStateTreeView()
        {
            _stateTreeViewScrollPosition = GUILayout.BeginScrollView(
                _stateTreeViewScrollPosition,
                false,
                true);
            _stateTreeView.OnGUI(GetRect(
                minLineCount: 5,
                maxHeight: _editorWindow.position.height));
            GUILayout.EndScrollView();
        }

        private void DrawSaveButton()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUI.BeginDisabledGroup(_stateTreeView.Address is null ||
                                         !StateViewerWindow.IsSavable);
            if (GUILayout.Button("Save", GUILayout.MaxWidth(50f)))
            {
                var (addr, value) = _stateTreeView.Serialize();
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
            _ncgValue = EditorGUILayout.TextField("NCG", _ncgValue);
            var ncg = _editorWindow.GetNCG();
            EditorGUI.BeginDisabledGroup(!StateViewerWindow.IsSavable || ncg is null);
            if (GUILayout.Button("Save", GUILayout.MaxWidth(50f)) &&
                ncg is { })
            {
                var balanceList = new List<(Address addr, FungibleAssetValue fav)>
                {
                    (
                        new Address(_searchingAddrStr),
                        FungibleAssetValue.Parse(ncg.Value, _ncgValue)
                    ),
                };
                ActionManager.Instance?.ManipulateState(null, balanceList);
            }

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            // CRYSTAL
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(!StateViewerWindow.IsSavable);
            _crystalValue = EditorGUILayout.TextField("CRYSTAL", _crystalValue);
            if (GUILayout.Button("Save", GUILayout.MaxWidth(50f)))
            {
                var balanceList = new List<(Address addr, FungibleAssetValue fav)>
                {
                    (new Address(_searchingAddrStr),
                        FungibleAssetValue.Parse(Currencies.Crystal, _crystalValue)),
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
                var (addr, value) = await stateProxy.GetStateAsync(addrStr);
                _stateTreeView.SetData(addr, value);

                await UniTask.Run(() =>
                {
                    var (_, ncgFav) = stateProxy.GetBalance(addr, ncg.Value);
                    _ncgValue = $"{ncgFav.MajorUnit}.{ncgFav.MinorUnit}";
                    var (_, crystalFav) = stateProxy.GetBalance(addr, Currencies.Crystal);
                    _crystalValue = $"{crystalFav.MajorUnit}.{crystalFav.MinorUnit}";
                });
            }
            catch (KeyNotFoundException)
            {
                ClearAll();
            }

            _loadingSomething = false;
            _stateTreeView.SetFocusAndEnsureSelectedItem();
        }
    }
}
