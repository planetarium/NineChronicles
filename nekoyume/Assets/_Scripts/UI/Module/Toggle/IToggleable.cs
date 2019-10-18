using UniRx;

namespace Nekoyume.UI.Module
{
    public interface IToggleable
    {
        bool IsToggledOn { get; }
        int GetInstanceID();
        void RegisterToggleListener(IToggleListener toggleListener);
        void SetToggledOn();
        void SetToggledOff();
    }

    public interface IToggleListener
    {
        void OnToggled(IToggleable toggleable);
    }
}
