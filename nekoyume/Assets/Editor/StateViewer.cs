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

namespace Editor
{
    public class StateViewer : EditorWindow
    {
        [SerializeField] TreeViewState treeViewState;

        private StateView stateView;
        private SearchField searchField;

        private string searchString;

        private StateProxy stateProxy;

        [MenuItem("Tools/Libplanet/View State")]
        private static void ShowWindow()
        {   
            var window = GetWindow<StateViewer>();
            window.titleContent = new GUIContent("State Viewer");
            window.Show();
        }

        private void OnEnable()
        {
            PreventNullElements();
        }

        private void OnGUI()
        {
            Rect rect = new Rect(0, 0, position.width, 20);
            PreventNullElements();

            var result = searchField.OnGUI(rect, searchString);
            searchString = result;

            DoProcess(searchString);

            rect.y += 20;
            rect.height = position.height - 20;

            stateView.OnGUI(rect);
        }

        private void DoProcess(string searchString)
        {
            var current = Event.current;
            if (current.type == EventType.KeyUp && current.keyCode == KeyCode.Return)
            {
                OnConfirm(searchString);
            }
        }

        private void OnConfirm(string searchString)
        {
            if (CheckPlaying())
            {
                RegisterAliases();
                try
                {
                    var state = stateProxy.GetState(searchString);
                    stateView.SetState(state);
                }
                catch (KeyNotFoundException)
                {
                    stateView.SetState((Text)"empty");
                }
            }
        }

        private bool CheckPlaying()
        {
            if (!Application.isPlaying || !Game.instance.IsInitialized)
            {
                Debug.Log("You can use this feature in only play mode.");
                return false;
            }

            return true;
        }

        private void PreventNullElements()
        {
            if (searchField is null)
            {
                searchField = new SearchField();
            }

            if (treeViewState is null)
            {
                treeViewState = new TreeViewState();
            }

            if (stateView is null)
            {
                stateView = new StateView(treeViewState);
            }

            if (stateProxy is null && CheckPlaying())
            {
                stateProxy = new StateProxy(Game.instance.Agent);
            }
        }

        private void RegisterAliases()
        {
            var states = Game.instance.States;
            for (int i = 0; i < 3; ++i)
            {
                if (states.AvatarStates.ContainsKey(i))
                    stateProxy.RegisterAlias($"avatar{i}", states.AvatarStates[i].address);
            }
            stateProxy.RegisterAlias("agent", states.AgentState.address);
            for (int i = 0; i < RankingState.RankingMapCapacity; ++i)
            {
                stateProxy.RegisterAlias("ranking", RankingState.Derive(i));
            }

            stateProxy.RegisterAlias("gameConfig", GameConfigState.Address);
            stateProxy.RegisterAlias("redeemCode", RedeemCodeState.Address);
            if (!(states.CurrentAvatarState is null))
            {
                stateProxy.RegisterAlias("me", states.CurrentAvatarState.address);
            }
        }
    }

    class StateProxy
    {
        private IAgent Agent { get; }
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
