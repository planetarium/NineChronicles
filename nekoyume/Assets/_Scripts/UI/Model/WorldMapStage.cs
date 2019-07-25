using System;
using Nekoyume.Data.Table;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class WorldMapStage : IDisposable
    {
        public enum State
        {
            Normal, Selected, Cleared, Disabled
        }
        
        public readonly ReactiveProperty<State> state = new ReactiveProperty<State>();
        public readonly ReactiveProperty<int> stage = new ReactiveProperty<int>();
        public readonly ReactiveProperty<bool> hasBoss = new ReactiveProperty<bool>();
        
        public readonly Subject<Module.WorldMapStage> onClick = new Subject<Module.WorldMapStage>();

        public WorldMapStage(State state, int stage, bool hasBoss)
        {
            this.state.Value = state;
            this.stage.Value = stage;
            this.hasBoss.Value = hasBoss;
        }
        
        public void Dispose()
        {
            state.Dispose();
            hasBoss.Dispose();
            stage.Dispose();
            
            onClick.Dispose();
        }
    }
}
