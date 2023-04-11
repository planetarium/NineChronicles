#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Bencodex;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using StateViewer.Runtime;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Boolean = Bencodex.Types.Boolean;

namespace StateViewer.Editor
{
    public class StateViewerWindow : EditorWindow
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

        [SerializeField]
        private bool initialized;

        [SerializeField]
        private bool useTestValues;

        private SourceFrom _sourceFrom;

        private string _encodedBencodexValue;

        private SearchField _searchField;
        private string _searchString;
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

        private Currency _ncg;
        private Currency _crystal;
        private string _ncgValue;
        private string _crystalValue;

        private StateProxy _stateProxy;

        // FIXME: TableSheets should be get from a state if needed for each time.
        private TableSheets _tableSheets;

        private static bool IsSavable => Application.isPlaying &&
                                         Game.instance.IsInitialized;

        [MenuItem("Tools/Lib9c/State Viewer")]
        private static void ShowWindow() =>
            GetWindow<StateViewerWindow>("State Viewer", true).Show();

        private void OnEnable()
        {
            minSize = new Vector2(800f, 400f);
            _sourceFrom = SourceFrom.GetStateFromPlayModeAgent;
            _encodedBencodexValue = string.Empty;
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
            _searchField = new SearchField();
            _searchField.downOrUpArrowKeyPressed += _stateTreeView.SetFocusAndEnsureSelectedItem;
            ClearAll();
            initialized = true;
        }

        private void OnGUI()
        {
            if (!initialized)
            {
                return;
            }

            if (Application.isPlaying)
            {
                if (Game.instance.Agent is null ||
                    Game.instance.States.AgentState is null)
                {
                    _stateProxy = null;
                    EditorGUILayout.HelpBox(
                        "Please wait until the Agent is initialized.",
                        MessageType.Info);
                }
                else if (_stateProxy is null)
                {
                    InitializeStateProxy();
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "This feature is only available in play mode.\n" +
                    "Use the test values below if you want to test the State Viewer" +
                    " in non-play mode.",
                    MessageType.Warning);
            }

            DrawAll();
        }

        private void ClearAll()
        {
            _encodedBencodexValue = string.Empty;
            _searchString = string.Empty;
            _stateTreeView.ClearData();
            _stateTreeViewScrollPosition = Vector2.zero;
            _ncgValue = string.Empty;
            _crystalValue = string.Empty;
        }

        private void DrawAll()
        {
            if (!Application.isPlaying)
            {
                GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
                DrawTestValues();
            }

            DrawHorizontalLine();
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

            return GUILayoutUtility.GetRect(
                minWidth ?? 10f,
                10f,
                minHeight,
                maxHeight ?? EditorGUIUtility.singleLineHeight,
                GUILayout.ExpandWidth(true));
        }

        private static void DrawHorizontalLine()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
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
            EditorGUILayout.BeginHorizontal();
            var rect = EditorGUILayout.GetControlRect(
                hasLabel: true,
                height: EditorGUIUtility.singleLineHeight);
            rect = EditorGUI.PrefixLabel(rect, new GUIContent("Address"));
            _searchString = _searchField.OnGUI(rect, _searchString);
            EditorGUILayout.EndHorizontal();

            _stateTreeView.ContentKind = (ContentKind)EditorGUILayout.EnumPopup(
                "Content Kind",
                _stateTreeView.ContentKind);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUI.BeginDisabledGroup(
                string.IsNullOrEmpty(_searchString) ||
                _loadingSomething ||
                !IsSavable);
            if (GUILayout.Button("Get State Async"))
            {
                GetStateAndUpdateStateTreeViewAsync(_searchString).Forget();
            }

            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();
        }

        private void DrawStateTreeView()
        {
            _stateTreeViewScrollPosition =
                GUILayout.BeginScrollView(_stateTreeViewScrollPosition);
            _stateTreeView.OnGUI(GetRect(minLineCount: 5, maxHeight: position.height));
            GUILayout.EndScrollView();
        }

        private void DrawSaveButton()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUI.BeginDisabledGroup(_stateTreeView.Address is null || !IsSavable);
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
            EditorGUI.BeginDisabledGroup(!IsSavable);
            if (GUILayout.Button("Save", GUILayout.MaxWidth(50f)))
            {
                var balanceList = new List<(Address addr, FungibleAssetValue fav)>
                {
                    (new Address(_searchString), FungibleAssetValue.Parse(_ncg, _ncgValue)),
                };
                ActionManager.Instance?.ManipulateState(null, balanceList);
            }

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            // CRYSTAL
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(!IsSavable);
            _crystalValue = EditorGUILayout.TextField("CRYSTAL", _crystalValue);
            if (GUILayout.Button("Save", GUILayout.MaxWidth(50f)))
            {
                var balanceList = new List<(Address addr, FungibleAssetValue fav)>
                {
                    (new Address(_searchString), FungibleAssetValue.Parse(_crystal, _crystalValue)),
                };
                ActionManager.Instance?.ManipulateState(null, balanceList);
            }

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }

        private async UniTaskVoid GetStateAndUpdateStateTreeViewAsync(string searchString)
        {
            if (string.IsNullOrEmpty(searchString))
            {
                ClearAll();
                return;
            }

            if (!Application.isPlaying ||
                !Game.instance.IsInitialized)
            {
                return;
            }

            _loadingSomething = true;
            try
            {
                var (addr, value) = await _stateProxy.GetStateAsync(searchString);
                _stateTreeView.SetData(addr, value);

                await UniTask.Run(() =>
                {
                    var (_, ncg) = _stateProxy.GetBalance(addr, _ncg);
                    _ncgValue = $"{ncg.MajorUnit}.{ncg.MinorUnit}";
                    var (_, crystal) = _stateProxy.GetBalance(addr, _crystal);
                    _crystalValue = $"{crystal.MajorUnit}.{crystal.MinorUnit}";
                });
            }
            catch (KeyNotFoundException)
            {
                ClearAll();
            }

            _loadingSomething = false;
            _stateTreeView.SetFocusAndEnsureSelectedItem();
        }

        private void InitializeStateProxy()
        {
            _stateProxy = new StateProxy(Game.instance.Agent);
            var states = Game.instance.States;
            for (var i = 0; i < 3; ++i)
            {
                if (states.AvatarStates.ContainsKey(i))
                {
                    _stateProxy.RegisterAlias($"avatar{i}", states.AvatarStates[i].address);
                }
            }

            _stateProxy.RegisterAlias("agent", states.AgentState.address);
            for (var i = 0; i < RankingState.RankingMapCapacity; ++i)
            {
                _stateProxy.RegisterAlias("ranking", RankingState.Derive(i));
            }

            _stateProxy.RegisterAlias("gameConfig", GameConfigState.Address);
            _stateProxy.RegisterAlias("redeemCode", RedeemCodeState.Address);
            if (!(states.CurrentAvatarState is null))
            {
                _stateProxy.RegisterAlias("me", states.CurrentAvatarState.address);
            }

            _ncg = states.GoldBalanceState.Gold.Currency;
            _crystal = CrystalCalculator.CRYSTAL;
        }
    }
}
