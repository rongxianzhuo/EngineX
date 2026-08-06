using System;

namespace EngineX.UI
{
    public interface IDialog
    {
        T GetChild<T>(string name) where T : IUiElement;
        void Close();
    }
}