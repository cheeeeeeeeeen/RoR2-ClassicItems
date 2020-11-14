namespace Chen.ClassicItems
{
    public static class EnemyItemDisplaysCompatibility
    {
        private static bool? _enabled;

        public static bool enabled
        {
            get
            {
                if (_enabled == null)
                {
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rob.EnemyItemDisplays");
                }
                return (bool)_enabled;
            }
        }

        public static void Setup()
        {
            if (!enabled) return;
            Log.Message("EnemyItemDisplays mod is found. There is actually nothing to do here except to let it load first.");
        }
    }
}