using System.Collections.Generic;
using System.Linq;
using UniRx;

namespace Nekoyume.UI.Module
{
    public class ToggleGroup : IToggleListener
    {
        private readonly Dictionary<int, IToggleable> _idAndToggleablePairs = new Dictionary<int, IToggleable>();
        public readonly Subject<IToggleable> OnToggledOn = new Subject<IToggleable>();
        public readonly Subject<IToggleable> OnToggledOff = new Subject<IToggleable>();

        #region IToggleListener

        public void OnToggled(IToggleable toggleable)
        {
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

        #endregion

        public void RegisterToggleable(IToggleable toggleable)
        {
            var id = toggleable.GetInstanceID();
            if (_idAndToggleablePairs.ContainsKey(id))
                return;
            
            RegisterToggleableInternal(toggleable);
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

        public void SetToggledOffAll()
        {
            foreach (var pair in _idAndToggleablePairs.Where(pair => pair.Value.IsToggledOn))
            {
                _idAndToggleablePairs[pair.Key].SetToggledOff();
            }
        }

        private void RegisterToggleableInternal(IToggleable toggleable)
        {
            var id = toggleable.GetInstanceID();
            _idAndToggleablePairs.Add(id, toggleable);
            toggleable.RegisterToggleListener(this);
        }
    }
}
