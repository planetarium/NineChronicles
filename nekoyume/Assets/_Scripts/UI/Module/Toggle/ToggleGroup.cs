using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;

namespace Nekoyume.UI.Module
{
    public class ToggleGroup : IToggleGroup
    {
        private readonly Dictionary<int, IToggleable> _idAndToggleablePairs = new Dictionary<int, IToggleable>();

        public readonly Subject<IToggleable> OnToggledOn = new Subject<IToggleable>();
        public readonly Subject<IToggleable> OnToggledOff = new Subject<IToggleable>();

        public IEnumerable<IToggleable> Toggleables => _idAndToggleablePairs.Values;

        public Func<bool> DisabledFunc = null;

        #region IToggleGroup

        public void OnToggle(IToggleable toggleable)
        {
            var disabled = DisabledFunc?.Invoke();
            if (disabled.HasValue && disabled.Value)
            {
                return;
            }

            var id = toggleable.GetInstanceID();
            foreach (var pair in _idAndToggleablePairs.Where(pair => pair.Key != id && pair.Value.IsToggledOn))
            {
                pair.Value.SetToggledOff();
                OnToggledOff.OnNext(pair.Value);
            }

            if (toggleable.IsToggledOn)
            {
                toggleable.SetToggledOff();
                OnToggledOff.OnNext(toggleable);
            }
            else
            {
                toggleable.SetToggledOn();
                OnToggledOn.OnNext(toggleable);
            }
        }

        public void RequestToggledOff(IToggleable toggleable)
        {
            toggleable.SetToggledOff();
            OnToggledOff.OnNext(toggleable);
        }

        public void RegisterToggleable(IToggleable toggleable)
        {
            var id = toggleable.GetInstanceID();
            if (_idAndToggleablePairs.ContainsKey(id))
                return;

            _idAndToggleablePairs.Add(id, toggleable);
            toggleable.SetToggleListener(this);
        }

        public void SetToggledOn(IToggleable toggleable)
        {
            var id = toggleable.GetInstanceID();
            foreach (var pair in _idAndToggleablePairs.Where(pair => pair.Key != id))
            {
                pair.Value.SetToggledOff();
            }

            toggleable.SetToggledOn();
        }

        public void SetToggledOff(IToggleable toggleable)
        {
            var id = toggleable.GetInstanceID();
            foreach (var pair in _idAndToggleablePairs.Where(pair => pair.Key == id))
            {
                pair.Value.SetToggledOff();
            }
        }

        public void SetToggledOffAll()
        {
            foreach (var pair in _idAndToggleablePairs.Where(pair => pair.Value.IsToggledOn))
            {
                _idAndToggleablePairs[pair.Key].SetToggledOff();
            }
        }

        #endregion
    }
}
