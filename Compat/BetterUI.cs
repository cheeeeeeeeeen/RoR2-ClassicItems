namespace Chen.ClassicItems
{
    public static class Compat_BetterUI
    {
        private static bool? _enabled;

        internal static bool enabled
        {
            get
            {
                if (_enabled == null) _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.xoxfaby.BetterUI");
                return (bool)_enabled;
            }
        }
    }
}