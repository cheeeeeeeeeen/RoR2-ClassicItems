// Unmodified code from ThinkInvis.ClassicItems

using System.Runtime.CompilerServices;

namespace Chen.ClassicItems
{
    internal static class Compat_ShareSuite
    {
        //taken from https://github.com/harbingerofme/DebugToolkit/blob/master/Code/DT-Commands/Money.cs
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        internal static void GiveMoney(uint amount)
        {
            ShareSuite.MoneySharingHooks.AddMoneyExternal((int)amount);
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        internal static bool MoneySharing()
        {
            if (ShareSuite.ShareSuite.MoneyIsShared.Value)
                return true;
            return false;
        }

        private static bool? _enabled;

        internal static bool enabled
        {
            get
            {
                if (_enabled == null) _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.funkfrog_sipondo.sharesuite");
                return (bool)_enabled;
            }
        }
    }
}