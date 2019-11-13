namespace Nekoyume.UI.Module
{
    public interface ISwitchable
    {
        bool IsSwitchedOn { get; }
        void Switch();
        void SetSwitchOn();
        void SetSwitchOff();
    }
}
