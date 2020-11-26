#undef DEBUG

using BepInEx;
using BepInEx.Configuration;
using Chen.Helpers;
using Chen.Helpers.GeneralHelpers;
using Chen.Helpers.LogHelpers;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.Artifacts;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TILER2;
using TMPro;
using UnityEngine.Networking;
using static Chen.Helpers.GeneralHelpers.AssetsManager;
using static TILER2.MiscUtil;
using Path = System.IO.Path;
using ThinkInvisCI = ThinkInvisible.ClassicItems;

namespace Chen.ClassicItems
{
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [BepInDependency(ThinkInvisCI.ClassicItemsPlugin.ModGuid, ThinkInvisCI.ClassicItemsPlugin.ModVer)]
    [BepInDependency(HelperPlugin.ModGuid, HelperPlugin.ModVer)]
    [BepInDependency(EnemyItemDisplays.EnemyItemDisplaysPlugin.MODUID, BepInDependency.DependencyFlags.SoftDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [R2APISubmoduleDependency(nameof(DotAPI), nameof(ResourcesAPI), nameof(PrefabAPI), nameof(BuffAPI),
                              nameof(LoadoutAPI), nameof(LanguageAPI))]
    public class ClassicItemsPlugin : BaseUnityPlugin
    {
        public const string ModVer =
#if DEBUG
            "0." +
#endif
            "2.2.13";

        public const string ModName = "ChensClassicItems";
        public const string ModGuid = "com.Chen.ChensClassicItems";

        public static readonly GlobalConfig globalCfg = new GlobalConfig();
        private static ConfigFile cfgFile;

        internal static FilingDictionary<CatalogBoilerplate> chensItemList = new FilingDictionary<CatalogBoilerplate>();

        private static readonly ReadOnlyDictionary<ItemTier, string> modelNameMap = new ReadOnlyDictionary<ItemTier, string>(new Dictionary<ItemTier, string>{
            {ItemTier.Boss, "BossCard"},
            {ItemTier.Lunar, "LunarCard"},
            {ItemTier.Tier1, "CommonCard"},
            {ItemTier.Tier2, "UncommonCard"},
            {ItemTier.Tier3, "RareCard"}
        });

        internal static Log Log;

        public bool longDesc { get; private set; } = ThinkInvisCI.ClassicItemsPlugin.globalConfig.longDesc;

        public static void ListItems(string start, ItemIndex[] list)
        {
            Log.Message(start);
            foreach (var item in list)
            {
                Log.Message($"-> {ItemCatalog.GetItemDef(item).name}");
            }
        }

#if DEBUG

        private void Update() => DropletGenerator.Update();

#endif

        private void Awake()
        {
            Log = new Log(Logger);

            Log.Debug("Performing plugin setup:");

#if DEBUG
            Log.Warning("Running test build with debug enabled! Report to CHEN if you're seeing this!");
            On.RoR2.Networking.GameNetworkManager.OnClientConnect += (self, user, t) => { };
#endif

            Log.Debug("Loading assets...");
            BundleInfo bundleInfo = new BundleInfo("@ChensClassicItems", "ChensClassicItems.chensclassicitems_assets", BundleType.UnityAssetBundle);
            new AssetsManager(bundleInfo).RegisterAll();

            cfgFile = new ConfigFile(Path.Combine(Paths.ConfigPath, ModGuid + ".cfg"), true);

            Log.Debug("Loading global configs...");
            globalCfg.BindAll(cfgFile, ModName, "Global");

            Log.Debug("Instantiating item classes...");
            chensItemList = T2Module.InitAll<CatalogBoilerplate>(new T2Module.ModInfo
            {
                displayName = "Chen's Classic Items",
                longIdentifier = "ChensClassicItems",
                shortIdentifier = "CCI",
                mainConfigFile = cfgFile
            });

            Log.Debug("Loading item configs...");
            foreach (CatalogBoilerplate x in chensItemList)
            {
                x.SetupConfig();
                x.ConfigEntryChanged += (sender, args) =>
                {
                    if ((args.flags & AutoConfigUpdateActionTypes.InvalidateLanguage) == 0) return;
                    var y = sender as CatalogBoilerplate;
                    if (y.pickupDef != null)
                    {
                        var c = y.pickupDef.displayPrefab;
                        if (!c) return;
                        var ctsf = c.transform;
                        var cfront = ctsf.Find("cardfront");
                        if (!cfront) return;

                        cfront.Find("carddesc").GetComponent<TextMeshPro>().text = Language.GetString(longDesc ? y.descToken : y.pickupToken);
                        cfront.Find("cardname").GetComponent<TextMeshPro>().text = Language.GetString(y.nameToken);
                    }
                    if (y.logbookEntry != null) y.logbookEntry.modelPrefab = y.pickupDef.displayPrefab;
                };
            }

            Log.Debug("Registering item attributes...");
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

            Log.Debug("Registering item behaviors...");
            foreach (CatalogBoilerplate x in chensItemList)
            {
                x.SetupBehavior();
            }

            Log.Debug("Performing early finalization...");
            T2Module.SetupAll_PluginStart(chensItemList);

            if (globalCfg.logEvolutionItemList)
            {
                RunArtifactManager.onArtifactEnabledGlobal += OnEvolutionEnable;
                RunArtifactManager.onArtifactDisabledGlobal += OnEvolutionDisable;
            }

            Log.Debug("Initial setup done!");
        }

        private void OnEvolutionDisable([JetBrains.Annotations.NotNull] RunArtifactManager runArtifactManager,
                                        [JetBrains.Annotations.NotNull] ArtifactDef artifactDef)
        {
            if (NetworkServer.active && artifactDef == RoR2Content.Artifacts.monsterTeamGainsItemsArtifactDef)
            {
                Run.onRunStartGlobal -= EvolutionListItems;
            }
        }

        private void OnEvolutionEnable([JetBrains.Annotations.NotNull] RunArtifactManager runArtifactManager,
                                       [JetBrains.Annotations.NotNull] ArtifactDef artifactDef)
        {
            if (NetworkServer.active && artifactDef == RoR2Content.Artifacts.monsterTeamGainsItemsArtifactDef)
            {
                Run.onRunStartGlobal += EvolutionListItems;
            }
        }

        private void EvolutionListItems(Run run)
        {
            Log.Message("Starting to display items that can be given to enemies by Evolution...");
            ListItems("COMMON:", MonsterTeamGainsItemsArtifactManager.availableTier1Items);
            ListItems("UNCOMMON:", MonsterTeamGainsItemsArtifactManager.availableTier2Items);
            ListItems("RARE:", MonsterTeamGainsItemsArtifactManager.availableTier3Items);
        }

        private void Start()
        {
            Log.Debug("Performing late setup:");
            Log.Debug("Nothing to perform. Early setup was done.");
            CatalogBoilerplate.ConsoleDump(Logger, chensItemList);
        }

        public class GlobalConfig : AutoConfigContainer
        {
            [AutoConfig("Used for logging items that can be given to enemies when Evolution is on. " +
                        "Makes it easier to report bugs related to Evolution and modded items.")]
            public bool logEvolutionItemList { get; private set; } = true;
        }
    }
}