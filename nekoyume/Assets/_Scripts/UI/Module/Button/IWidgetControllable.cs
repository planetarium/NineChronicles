namespace Nekoyume.UI.Module
{
    public interface IWidgetControllable
    {
        bool HasWidget { get; }
        void SetWidgetType<T>() where T : Widget;
        void ShowWidget();
        void HideWidget();
    }
}
