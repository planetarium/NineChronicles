namespace Nekoyume.UI.Module
{
    public interface IToggleGroup : IToggleListener
    {
        void RegisterToggleable(IToggleable toggleable);
        void SetToggledOn(IToggleable toggleable);
        void SetToggledOff(IToggleable toggleable);
        void SetToggledOffAll();
    }
}
