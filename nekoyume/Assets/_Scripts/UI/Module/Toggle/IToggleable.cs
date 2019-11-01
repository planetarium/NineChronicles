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
}
