#undef DEBUG

using BepInEx;
using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.Projectile;
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
    [R2APISubmoduleDependency(nameof(DotAPI), nameof(ItemAPI), nameof(LanguageAPI), nameof(ResourcesAPI), nameof(PlayerAPI), nameof(PrefabAPI), nameof(BuffAPI), nameof(LoadoutAPI))]
    public class ClassicItemsPlugin : BaseUnityPlugin
    {
        public const string ModVer =
#if DEBUG
                "0." +
#endif
            "0.2.2";

        public const string ModName = "ChensClassicItems";
        public const string ModGuid = "com.Chen.ChensClassicItems";

        private static ConfigFile cfgFile;

        internal static FilingDictionary<ItemBoilerplate> chensItemList = new FilingDictionary<ItemBoilerplate>();

        private static readonly ReadOnlyDictionary<ItemTier, string> modelNameMap = new ReadOnlyDictionary<ItemTier, string>(new Dictionary<ItemTier, string>{
            {ItemTier.Boss, "BossCard"},
            {ItemTier.Lunar, "LunarCard"},
            {ItemTier.Tier1, "CommonCard"},
            {ItemTier.Tier2, "UncommonCard"},
            {ItemTier.Tier3, "RareCard"}
        });

        internal static BepInEx.Logging.ManualLogSource _logger;

        public static GameObject panicMinePrefab;
        public static GameObject footMinePrefab;
        public static BuffIndex footPoisonBuff;
        public static DotController.DotIndex footPoisonDot;
        public static GameObject instantMinePrefab;

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
            Logger.LogWarning("Running test build with debug enabled! If you're seeing this after downloading the mod from Thunderstore, please panic.");
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
            chensItemList = ItemBoilerplate.InitAll("ChensClassicItems");

            Logger.LogDebug("Loading item configs...");
            foreach (ItemBoilerplate x in chensItemList)
            {
                x.ConfigEntryChanged += (sender, args) =>
                {
                    if ((args.flags & (AutoUpdateEventFlags.InvalidateNameToken | (longDesc ? AutoUpdateEventFlags.InvalidateDescToken : AutoUpdateEventFlags.InvalidatePickupToken))) == 0) return;
                    if (x.pickupDef != null)
                    {
                        var ctsf = x.pickupDef.displayPrefab?.transform;
                        if (!ctsf) return;
                        var cfront = ctsf.Find("cardfront");
                        if (!cfront) return;

                        cfront.Find("carddesc").GetComponent<TextMeshPro>().text = Language.GetString(longDesc ? x.descToken : x.pickupToken);
                        cfront.Find("cardname").GetComponent<TextMeshPro>().text = Language.GetString(x.nameToken);
                    }
                    if (x.logbookEntry != null)
                    {
                        x.logbookEntry.modelPrefab = x.pickupDef.displayPrefab;
                    }
                };
                x.SetupConfig(cfgFile);
            }

            Logger.LogDebug("Registering item attributes...");

            int longestName = 0;
            foreach (ItemBoilerplate x in chensItemList)
            {
                string mpnOvr = null;
                if (x is Item item) mpnOvr = "@ChensClassicItems:Assets/ClassicItems/models/" + modelNameMap[item.itemTier] + ".prefab";
                else if (x is Equipment eqp) mpnOvr = "@ChensClassicItems:Assets/ClassicItems/models/" + (eqp.eqpIsLunar ? "LqpCard.prefab" : "EqpCard.prefab");
                var ipnOvr = "@ChensClassicItems:Assets/ClassicItems/icons/" + x.itemCodeName + "_icon.png";

                if (mpnOvr != null)
                {
                    typeof(ItemBoilerplate).GetProperty(nameof(ItemBoilerplate.modelPathName)).SetValue(x, mpnOvr);
                    typeof(ItemBoilerplate).GetProperty(nameof(ItemBoilerplate.iconPathName)).SetValue(x, ipnOvr);
                }

                x.SetupAttributes("CHENSCLASSICITEMS", "CCI");
                if (x.itemCodeName.Length > longestName) longestName = x.itemCodeName.Length;
            }

            Logger.LogMessage("Index dump follows (pairs of name / index):");
            foreach (ItemBoilerplate x in chensItemList)
            {
                if (x is Equipment eqp)
                    Logger.LogMessage("Equipment CCI" + x.itemCodeName.PadRight(longestName) + " / " + ((int)eqp.regIndex).ToString());
                else if (x is Item item)
                    Logger.LogMessage("     Item CCI" + x.itemCodeName.PadRight(longestName) + " / " + ((int)item.regIndex).ToString());
                else if (x is Artifact afct)
                    Logger.LogMessage(" Artifact CCI" + x.itemCodeName.PadRight(longestName) + " / " + ((int)afct.regIndex).ToString());
                else
                    Logger.LogMessage("    Other CCI" + x.itemCodeName.PadRight(longestName) + " / N/A");
            }

            Logger.LogDebug("Tweaking vanilla stuff...");
            Logger.LogDebug("No need. ThinkInvis.ClassicItems has added the needed actions.");

            Logger.LogDebug("Creating new prefabs...");

            Logger.LogDebug("Cloning needed prefabs...");

            GameObject engiMinePrefab = Resources.Load<GameObject>("prefabs/projectiles/EngiMine");

            panicMinePrefab = engiMinePrefab.InstantiateClone("PanicMine");
            Destroy(panicMinePrefab.GetComponent<ProjectileDeployToOwner>());
            footMinePrefab = engiMinePrefab.InstantiateClone("FootMine");
            Destroy(footMinePrefab.GetComponent<ProjectileDeployToOwner>());
            instantMinePrefab = engiMinePrefab.InstantiateClone("InstantMine");
            Destroy(instantMinePrefab.GetComponent<ProjectileDeployToOwner>());

            Logger.LogDebug("Registering buffs...");

            var poisonBuffDef = new CustomBuff(new BuffDef
            {
                buffColor = new Color(1, 121, 91),
                canStack = true,
                isDebuff = true,
                name = "CCIFootPoison",
                iconPath = "@ChensClassicItems:Assets/ClassicItems/icons/footmine_buff_icon.png"
            });
            footPoisonBuff = BuffAPI.Add(poisonBuffDef);

            Logger.LogDebug("Registering DoTs...");

            var poisonDotDef = new DotController.DotDef
            {
                interval = 1,
                damageCoefficient = 1,
                damageColorIndex = DamageColorIndex.Poison,
                associatedBuff = footPoisonBuff
            };
            footPoisonDot = DotAPI.RegisterDotDef(poisonDotDef);

            Logger.LogDebug("Registering item behaviors...");

            foreach (ItemBoilerplate x in chensItemList)
            {
                x.SetupBehavior();
            }

            Logger.LogDebug("Initial setup done!");
        }

        private void Start()
        {
            Logger.LogDebug("Performing late setup:");
            Logger.LogDebug("Nothing to perform here.");
        }
    }
}