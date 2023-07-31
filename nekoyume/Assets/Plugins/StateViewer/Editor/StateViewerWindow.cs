#nullable enable

using Libplanet.Types.Assets;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using StateViewer.Editor.Features;
using StateViewer.Runtime;
using UnityEditor;
using UnityEngine;

namespace StateViewer.Editor
{
    public class StateViewerWindow : EditorWindow
    {
        private class ViewModel
        {
            public readonly StateAndBalanceFeature StateAndBalanceFeature;
            public readonly SetInventoryFeature SetInventoryFeature;
            public readonly TrackStateFeature TrackStateFeature;
            public int SelectedFeatureIndex;

            public ViewModel(StateViewerWindow window)
            {
                StateAndBalanceFeature = new StateAndBalanceFeature(window);
                SetInventoryFeature = new SetInventoryFeature(window);
                TrackStateFeature = new TrackStateFeature(window);
                SelectedFeatureIndex = 0;
            }
        }

        private static readonly string[] FeatureModeNames =
        {
            "State & Balance",
            "Set Inventory",
            "Track State",
        };

        private ViewModel? _viewModel;
        private StateProxy? _stateProxy;
        private Currency? _ncg;

        // FIXME: TableSheets should be get from a state if needed for each time.
        private TableSheets? _tableSheets;

        public static bool IsSavable => Application.isPlaying &&
                                        Game.instance.IsInitialized;

        [MenuItem("Tools/Lib9c/State Viewer")]
        private static void ShowWindow() =>
            GetWindow<StateViewerWindow>("State Viewer", true).Show();

        private void OnEnable()
        {
            minSize = new Vector2(800f, 400f);
            _viewModel = new ViewModel(this)
            {
                SelectedFeatureIndex = 0,
            };
            _stateProxy = null;
        }

        private void OnGUI()
        {
            if (_viewModel is null)
            {
                return;
            }

            _viewModel.SelectedFeatureIndex = GUILayout.Toolbar(
                _viewModel.SelectedFeatureIndex,
                FeatureModeNames);
            switch (_viewModel.SelectedFeatureIndex)
            {
                case 0:
                    _viewModel.StateAndBalanceFeature.OnGUI();
                    break;
                case 1:
                    _viewModel.SetInventoryFeature.OnGUI();
                    break;
                case 2:
                    _viewModel.TrackStateFeature.OnGUI();
                    break;
            }
        }

        public StateProxy? GetStateProxy(bool drawHelpBox)
        {
            if (Application.isPlaying)
            {
                var game = Game.instance;
                if (game.Agent is null ||
                    game.States.AgentState is null)
                {
                    if (drawHelpBox)
                    {
                        EditorGUILayout.HelpBox(
                            "Please wait until the Agent is initialized.",
                            MessageType.Info);
                    }

                    return null;
                }

                if (_stateProxy is null)
                {
                    InitializeStateProxy();
                }

                return _stateProxy;
            }

            if (drawHelpBox)
            {
                EditorGUILayout.HelpBox(
                    "This feature is only available in play mode.\n" +
                    "Use the test values below if you want to test the State Viewer" +
                    " in non-play mode.",
                    MessageType.Warning);
            }

            return null;
        }

        public Currency? GetNCG()
        {
            if (!Application.isPlaying)
            {
                return null;
            }

            return _ncg ??= Game.instance.States?.GoldBalanceState?.Gold.Currency;
        }

        public TableSheets? GetTableSheets()
        {
            return _tableSheets ??= TableSheetsHelper.MakeTableSheets();
        }

        private void InitializeStateProxy()
        {
            var states = Game.instance.States;
            if (states is null)
            {
                return;
            }

            _stateProxy = new StateProxy(Game.instance.Agent);
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
        }
    }
}
