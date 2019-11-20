namespace Nekoyume.UI.Module
{
    public interface IToggleable
    {
        string Name { get; }
        bool IsToggledOn { get; }
        int GetInstanceID();
        void SetToggleListener(IToggleListener toggleListener);
        void SetToggledOn();
        void SetToggledOff();
    }
}
