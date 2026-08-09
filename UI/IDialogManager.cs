namespace EngineX.UI
{
    public interface IDialogManager
    {
        IDialog Show(string dialogName);
        void Close(string dialogName);
    }
}