namespace EngineX.UI
{
    public interface IUiButton : IUiElement
    {
        bool IsPressed();

        void SetEnabled(bool enabled);
    }
}