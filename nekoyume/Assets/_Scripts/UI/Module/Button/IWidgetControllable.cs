namespace Nekoyume.UI.Module
{
    public interface IWidgetControllable
    {
        void SetWidgetType<T>() where T : Widget;
        void ShowWidget();
        void HideWidget();
    }
}
