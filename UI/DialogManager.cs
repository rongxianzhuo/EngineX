using System.Collections.Generic;

namespace EngineX.UI
{
    public static class DialogManager
    {
        private static IDialogManager _impl;
        private static Dictionary<string, IDialog> _cache;

        public static void Register(IDialogManager impl)
        {
            Reset();
            _impl = impl;
            _cache = new Dictionary<string, IDialog>();
        }

        public static IDialog Get(string name)
        {
            if (_cache.TryGetValue(name, out IDialog dialog))
            {
                return dialog;
            }

            if (_impl == null)
            {
                return null;
            }

            dialog = _impl.Load(name);
            if (dialog != null)
            {
                _cache[name] = dialog;
            }
            return dialog;
        }

        public static void Reset()
        {
            if (_cache == null)
            {
                return;
            }

            foreach (IDialog dialog in _cache.Values)
            {
                dialog?.Dispose();
            }
            _cache.Clear();
        }
    }
}