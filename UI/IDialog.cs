using System;

namespace EngineX.UI
{
    public interface IDialog : IDisposable
    {
        T GetChild<T>(string name) where T : IUiElement;

        IDialog GetChild(string name);

        void SetVisible(bool visible);
    }
}