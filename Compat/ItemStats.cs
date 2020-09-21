// Unmodified code from ThinkInvis.ClassicItems

using RoR2;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Chen.ClassicItems
{
    internal static class Compat_ItemStats
    {
        internal delegate float ItemStatFormulaWrapper(float itemCount, Inventory ctxInventory, CharacterMaster ctxMaster);

        internal delegate string ItemStatFormatterWrapper(float itemCount, Inventory ctxInventory, CharacterMaster ctxMaster);

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        internal static void CreateItemStatDef(ItemDef idef, params (ItemStatFormulaWrapper, ItemStatFormatterWrapper)[] wrappers)
        {
            List<ItemStats.Stat.ItemStat> stats = new List<ItemStats.Stat.ItemStat>();

            foreach (var pair in wrappers)
            {
                stats.Add(new ItemStats.Stat.ItemStat(
                    (itemCount, ctx) => { return pair.Item1.Invoke(itemCount, ctx.Inventory, ctx.Master); },
                    (itemCount, ctx) => { return pair.Item2.Invoke(itemCount, ctx.Inventory, ctx.Master); }));
            }

            var isd = new ItemStats.ItemStatDef
            {
                Stats = stats
            };

            ItemStats.ItemStatsMod.AddCustomItemStatDef(idef.itemIndex, isd);
        }

        private static bool? _enabled;

        internal static bool enabled
        {
            get
            {
                if (_enabled == null) _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("dev.ontrigger.itemstats");
                return (bool)_enabled;
            }
        }
    }
}