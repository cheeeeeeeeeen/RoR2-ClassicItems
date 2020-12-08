using EnemyItemDisplays;
using static Chen.ClassicItems.ClassicItemsPlugin;

namespace Chen.ClassicItems
{
    internal static class EnemyItemDisplaysCompatibility
    {
        private static bool? _enabled;

        public static bool enabled
        {
            get
            {
                if (_enabled == null)
                {
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(EnemyItemDisplaysPlugin.MODUID);
                }
                return (bool)_enabled;
            }
        }

        public static void Setup()
        {
            Log.Debug("EnemyItemDisplays mod is found. There is actually nothing to do here except to let it load first.");
        }
    }
}