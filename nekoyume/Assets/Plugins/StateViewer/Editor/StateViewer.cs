using System;
using System.Collections.Generic;
using Bencodex.Types;
using Libplanet;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Model.State;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Event = UnityEngine.Event;

namespace StateViewer.Editor
{
    public class StateViewer : EditorWindow
    {
        // SerializeField is used to ensure the view state is written to the window
        // layout file. This means that the state survives restarting Unity as long as the window
        // is not closed. If the attribute is omitted then the state is still serialized/deserialized.
        [SerializeField]
        private TreeViewState stateViewState;

        [SerializeField]
        private MultiColumnHeaderState multiColumnHeaderState;

        private StateView _stateView;

        private SearchField _searchField;

        private string _searchString;

        private StateProxy _stateProxy;

        [MenuItem("Tools/Lib9c/State Viewer")]
        private static void ShowWindow() =>
            GetWindow<StateViewer>("State Viewer", true).Show();

        private void OnEnable()
        {
            stateViewState ??= new TreeViewState();
            multiColumnHeaderState ??= new MultiColumnHeaderState(
                Array.Empty<MultiColumnHeaderState.Column>());
            _stateView = new StateView(stateViewState);
            _searchField = new SearchField();
        }

        private void OnGUI()
        {
            if (Application.isPlaying)
            {
                if (Game.instance.Agent is null)
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
                // Draw warning message.
                EditorGUILayout.HelpBox(
                    "This feature is only available in Play mode.",
                    MessageType.Warning);
            }

            DrawAll();
        }

        private void DrawAll()
        {
            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
            DrawSearch();
            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
            var rect = GetRect(maxHeight: position.height);
            _stateView.OnGUI(rect);
            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
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

        private void DrawSearch()
        {
            var rect = GetRect();
            _searchString = _searchField.OnGUI(rect, _searchString);

            var current = Event.current;
            if (current.keyCode != KeyCode.Return ||
                current.type != EventType.KeyUp)
            {
                return;
            }

            OnConfirm(_searchString);
        }

        private void OnConfirm(string searchString)
        {
            if (!Application.isPlaying ||
                !Game.instance.IsInitialized)
            {
                return;
            }

            try
            {
                var state = _stateProxy.GetState(searchString);
                _stateView.SetState(state);
            }
            catch (KeyNotFoundException)
            {
                _stateView.SetState((Text)"empty");
            }
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
        }
    }

    internal class StateProxy
    {
        public IAgent Agent { get; }
        private Dictionary<string, Address> Aliases { get; }

        public StateProxy(IAgent agent)
        {
            Agent = agent;
            Aliases = new Dictionary<string, Address>();
        }

        public IValue GetState(string searchString)
        {
            Address address;

            if (searchString.Length == 40)
            {
                address = new Address(searchString);
            }
            else
            {
                address = Aliases[searchString];
            }

            return Agent.GetState(address);
        }

        public void RegisterAlias(string alias, Address address)
        {
            if (!Aliases.ContainsKey(alias))
            {
                Aliases.Add(alias, address);
            }
            else
            {
                Aliases[alias] = address;
            }
        }
    }
}
