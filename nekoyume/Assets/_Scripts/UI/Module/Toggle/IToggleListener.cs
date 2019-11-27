namespace Nekoyume.UI.Module
{
    public interface IToggleListener
    {
        void OnToggle(IToggleable toggleable);
        void RequestToggledOff(IToggleable toggleable);
    }
}
