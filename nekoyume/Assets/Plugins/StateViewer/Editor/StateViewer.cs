using System.Collections.Generic;
using System.Text;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Event = UnityEngine.Event;

namespace StateViewer.Editor
{
    public class StateViewer : EditorWindow
    {
        [SerializeField]
        private bool initialized;

        [SerializeField]
        private bool drawTestValues;

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

        private SearchField _searchField;
        private string _searchString;

        private Currency _ncg;
        private Currency _crystal;
        private string _ncgValue;
        private string _crystalValue;

        private StateProxy _stateProxy;

        private readonly IValue[] _testValues =
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

        [MenuItem("Tools/Lib9c/State Viewer")]
        private static void ShowWindow() =>
            GetWindow<StateViewer>("State Viewer", true).Show();

        private void OnEnable()
        {
            minSize = new Vector2(800f, 300f);

            stateTreeViewState ??= new TreeViewState();
            var keyOrIndexColumn = new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent("Key/Index"),
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
            var editColumn = new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent("Add/Edit"),
                headerTextAlignment = TextAlignment.Center,
                canSort = false,
                width = 100,
                minWidth = 100,
                maxWidth = 100,
                autoResize = true,
                allowToggleVisibility = false,
            };
            var addRemoveColumn = new MultiColumnHeaderState.Column
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
                keyOrIndexColumn,
                aliasColumn,
                valueKindColumn,
                valueColumn,
                editColumn,
                addRemoveColumn,
            });
            _stateTreeHeader = new MultiColumnHeader(stateTreeHeaderState);
            _stateTreeHeader.ResizeToFit();
            _stateTreeView = new StateTreeView(
                stateTreeViewState,
                _stateTreeHeader);
            _searchField = new SearchField();
            _stateTreeView.SetData(default, Null.Value);
            _searchField.downOrUpArrowKeyPressed += _stateTreeView.SetFocusAndEnsureSelectedItem;
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
            DrawSearchField();
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
            float? maxHeight = null)
        {
            return GUILayoutUtility.GetRect(
                minWidth ?? 1f,
                1f,
                EditorGUIUtility.singleLineHeight,
                maxHeight ?? EditorGUIUtility.singleLineHeight,
                GUILayout.ExpandWidth(true));
        }

        private void DrawHorizontalLine()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }

        private void DrawTestValues()
        {
            drawTestValues = EditorGUILayout.Toggle("Test Values", drawTestValues);
            if (!drawTestValues)
            {
                return;
            }

            EditorGUILayout.BeginHorizontal();
            for (var i = 0; i < _testValues.Length; i++)
            {
                var testValue = _testValues[i];
                if (GUILayout.Button($"{i}: {testValue.Kind}"))
                {
                    _stateTreeView.SetData(default, testValue);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSearchField()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Address");
            _searchString = _searchField.OnGUI(_searchString);
            GUILayout.EndHorizontal();
            if (!_searchField.HasFocus())
            {
                return;
            }

            var current = Event.current;
            if (current.keyCode != KeyCode.Return ||
                current.type != EventType.KeyUp)
            {
                return;
            }

            _stateTreeView.SetFocus();
            OnConfirm(_searchString).Forget();
        }

        private void DrawStateTreeView()
        {
            _stateTreeViewScrollPosition =
                GUILayout.BeginScrollView(_stateTreeViewScrollPosition);
            _stateTreeView.OnGUI(GetRect(maxHeight: position.height));
            GUILayout.EndScrollView();
        }

        private void DrawSaveButton()
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.Space();
            if (GUILayout.Button("Save", GUILayout.MaxWidth(50f)))
            {
                var stateList = new List<(Address addr, IValue value)>
                {
                    _stateTreeView.Serialize(),
                };
                ActionManager.Instance?.ManipulateState(stateList, null);
            }

            GUILayout.EndHorizontal();
        }

        private void DrawBalances()
        {
            GUILayout.Label("Balances", EditorStyles.boldLabel);

            // NCG
            EditorGUILayout.BeginHorizontal();
            _ncgValue = EditorGUILayout.TextField("NCG", _ncgValue);
            if (GUILayout.Button("Save", GUILayout.MaxWidth(50f)))
            {
                var balanceList = new List<(Address addr, FungibleAssetValue fav)>
                {
                    (new Address(_searchString), FungibleAssetValue.Parse(_ncg, _ncgValue)),
                };
                ActionManager.Instance?.ManipulateState(null, balanceList);
            }

            EditorGUILayout.EndHorizontal();

            // CRYSTAL
            EditorGUILayout.BeginHorizontal();
            _crystalValue = EditorGUILayout.TextField("CRYSTAL", _crystalValue);
            if (GUILayout.Button("Save", GUILayout.MaxWidth(50f)))
            {
                var balanceList = new List<(Address addr, FungibleAssetValue fav)>
                {
                    (new Address(_searchString), FungibleAssetValue.Parse(_crystal, _crystalValue)),
                };
                ActionManager.Instance?.ManipulateState(null, balanceList);
            }

            EditorGUILayout.EndHorizontal();
        }

        private async UniTaskVoid OnConfirm(string searchString)
        {
            if (!Application.isPlaying ||
                !Game.instance.IsInitialized)
            {
                return;
            }

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
                _stateTreeView.SetData(default, (Text)"empty");
            }

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
