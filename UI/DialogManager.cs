

namespace EngineX.UI
{
    public static class DialogManager
    {
        private static IDialogManager _impl;

        public static void Register(IDialogManager impl)
        {
            _impl = impl;
        }

        public static IDialog Show(string name)
        {
            return _impl?.Show(name);
        }

        public static void Unregister()
        {
            _impl = null;
        }
    }
}