#undef DEBUG

using BepInEx;
using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using RoR2;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using TILER2;
using TMPro;
using UnityEngine;
using static TILER2.MiscUtil;
using Path = System.IO.Path;
using ThinkInvisCI = ThinkInvisible.ClassicItems;

namespace Chen.ClassicItems
{
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [BepInDependency(ThinkInvisCI.ClassicItemsPlugin.ModGuid, ThinkInvisCI.ClassicItemsPlugin.ModVer)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [R2APISubmoduleDependency(nameof(DotAPI), nameof(ResourcesAPI), nameof(PrefabAPI), nameof(BuffAPI),
                              nameof(LoadoutAPI), nameof(LanguageAPI))]
    public class ClassicItemsPlugin : BaseUnityPlugin
    {
        public const string ModVer =
#if DEBUG
            "0." +
#endif
            "2.2.1";

        public const string ModName = "ChensClassicItems";
        public const string ModGuid = "com.Chen.ChensClassicItems";

        private static ConfigFile cfgFile;

        internal static FilingDictionary<CatalogBoilerplate> chensItemList = new FilingDictionary<CatalogBoilerplate>();

        private static readonly ReadOnlyDictionary<ItemTier, string> modelNameMap = new ReadOnlyDictionary<ItemTier, string>(new Dictionary<ItemTier, string>{
            {ItemTier.Boss, "BossCard"},
            {ItemTier.Lunar, "LunarCard"},
            {ItemTier.Tier1, "CommonCard"},
            {ItemTier.Tier2, "UncommonCard"},
            {ItemTier.Tier3, "RareCard"}
        });

        internal static BepInEx.Logging.ManualLogSource _logger;

        public bool longDesc { get; private set; } = ThinkInvisCI.ClassicItemsPlugin.globalConfig.longDesc;

#if DEBUG

        public void Update()
        {
            var i3 = Input.GetKeyDown(KeyCode.F3);
            var i4 = Input.GetKeyDown(KeyCode.F4);
            var i5 = Input.GetKeyDown(KeyCode.F5);
            var i6 = Input.GetKeyDown(KeyCode.F6);
            var i7 = Input.GetKeyDown(KeyCode.F7);
            if (i3 || i4 || i5 || i6 || i7)
            {
                var trans = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;

                List<PickupIndex> spawnList;
                if (i3) spawnList = Run.instance.availableTier1DropList;
                else if (i4) spawnList = Run.instance.availableTier2DropList;
                else if (i5) spawnList = Run.instance.availableTier3DropList;
                else if (i6) spawnList = Run.instance.availableEquipmentDropList;
                else spawnList = Run.instance.availableLunarDropList;

                PickupDropletController.CreatePickupDroplet(spawnList[Run.instance.spawnRng.RangeInt(0, spawnList.Count)], trans.position, new Vector3(0f, -5f, 0f));
            }
        }

#endif

        private void Awake()
        {
            _logger = Logger;

            Logger.LogDebug("Performing plugin setup:");

#if DEBUG
            Logger.LogWarning("Running test build with debug enabled! Report to CHEN if you're seeing this!");
            On.RoR2.Networking.GameNetworkManager.OnClientConnect += (self, user, t) => { };
#endif

            Logger.LogDebug("Loading assets...");
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ChensClassicItems.chensclassicitems_assets"))
            {
                var bundle = AssetBundle.LoadFromStream(stream);
                var provider = new AssetBundleResourcesProvider("@ChensClassicItems", bundle);
                ResourcesAPI.AddProvider(provider);
            }

            cfgFile = new ConfigFile(Path.Combine(Paths.ConfigPath, ModGuid + ".cfg"), true);

            Logger.LogDebug("Loading global configs...");
            Logger.LogDebug("Skip. Global configs are based on ThinkInvis.ClassicItems.");

            Logger.LogDebug("Instantiating item classes...");
            chensItemList = T2Module.InitAll<CatalogBoilerplate>(new T2Module.ModInfo
            {
                displayName = "Chen's Classic Items",
                longIdentifier = "ChensClassicItems",
                shortIdentifier = "CCI",
                mainConfigFile = cfgFile
            });

            Logger.LogDebug("Loading item configs...");
            foreach (CatalogBoilerplate x in chensItemList)
            {
                x.SetupConfig();
                x.ConfigEntryChanged += (sender, args) =>
                {
                    if ((args.flags & AutoConfigUpdateActionTypes.InvalidateLanguage) == 0) return;
                    var y = sender as CatalogBoilerplate;
                    if (y.pickupDef != null)
                    {
                        var ctsf = y.pickupDef.displayPrefab?.transform;
                        if (!ctsf) return;
                        var cfront = ctsf.Find("cardfront");
                        if (!cfront) return;

                        cfront.Find("carddesc").GetComponent<TextMeshPro>().text = Language.GetString(longDesc ? y.descToken : y.pickupToken);
                        cfront.Find("cardname").GetComponent<TextMeshPro>().text = Language.GetString(y.nameToken);
                    }
                    if (y.logbookEntry != null) y.logbookEntry.modelPrefab = y.pickupDef.displayPrefab;
                };
            }

            Logger.LogDebug("Registering item attributes...");
            foreach (CatalogBoilerplate x in chensItemList)
            {
                string mpnOvr = null;
                if (x is Item_V2 item) mpnOvr = "@ChensClassicItems:Assets/ClassicItems/models/" + modelNameMap[item.itemTier] + ".prefab";
                else if (x is Equipment_V2 eqp) mpnOvr = "@ChensClassicItems:Assets/ClassicItems/models/" + (eqp.isLunar ? "LqpCard.prefab" : "EqpCard.prefab");
                var ipnOvr = "@ChensClassicItems:Assets/ClassicItems/icons/" + x.name + "_icon.png";

                if (mpnOvr != null)
                {
                    typeof(CatalogBoilerplate).GetProperty(nameof(CatalogBoilerplate.modelResourcePath)).SetValue(x, mpnOvr);
                    typeof(CatalogBoilerplate).GetProperty(nameof(CatalogBoilerplate.iconResourcePath)).SetValue(x, ipnOvr);
                }

                x.SetupAttributes();
            }

            Logger.LogDebug("Registering item behaviors...");
            foreach (CatalogBoilerplate x in chensItemList)
            {
                x.SetupBehavior();
            }

            Logger.LogDebug("Performing early finalization...");
            T2Module.SetupAll_PluginStart(chensItemList);
            CatalogBoilerplate.ConsoleDump(Logger, chensItemList);

            Logger.LogDebug("Initial setup done!");
        }

        private void Start()
        {
            Logger.LogDebug("Performing late setup:");
            Logger.LogDebug("Nothing to perform. Early setup was done.");
        }
    }

    public static class Log
    {
        public static void Debug(object data) => logger.LogDebug(data);

        public static void Error(object data) => logger.LogError(data);

        public static void Info(object data) => logger.LogInfo(data);

        public static void Message(object data) => logger.LogMessage(data);

        public static void Warning(object data) => logger.LogWarning(data);

        public static BepInEx.Logging.ManualLogSource logger => ClassicItemsPlugin._logger;
    }
}