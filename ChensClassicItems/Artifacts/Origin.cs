#undef DEBUG

using Chen.Helpers.LogHelpers.Collections;
using Chen.Helpers.UnityHelpers;
using R2API;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using TILER2;
using UnityEngine;
using UnityEngine.Networking;
using static Chen.ClassicItems.ClassicItemsPlugin;
using static RoR2.DirectorPlacementRule;
using RoR2Items = RoR2.RoR2Content.Items;

namespace Chen.ClassicItems.Artifacts
{
    /// <summary>
    /// Singleton artifact class powered by TILER2 that implements the Artifact of Origin functionality.
    /// </summary>
    public class Origin : Artifact<Origin>
    {
        /// <summary>
        /// The suffix appended on the Imps spawned by Artifact of Origin.
        /// Might be useful if one wants to fetch the objects related to these Imps through their name.
        /// </summary>
        public const string originSuffix = "ChensClassicItemsOrigin";

        /// <summary>
        /// The drop table used for determining the Imp Vanguard's drops.
        /// </summary>
        public static PickupDropTable dropTable { get; private set; }

        /// <summary>
        /// The RNG used for this artifact.
        /// </summary>
        public static Xoroshiro128Plus treasureRng { get; private set; } = new Xoroshiro128Plus(0UL);

        /// <summary>
        /// The Spawn Card of the Imp Vanguard.
        /// </summary>
        public static CharacterSpawnCard originOverlordSpawnCard { get; private set; }

        /// <summary>
        /// The Spawn Card of the Imp Soldier.
        /// </summary>
        public static CharacterSpawnCard originImpSpawnCard { get; private set; }

        internal static List<GameObject> bodyObjects = new List<GameObject>();
        internal static List<GameObject> masterObjects = new List<GameObject>();

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override string displayName => "Artifact of Origin";

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Amount of time in minutes for Imps to invade the area.", AutoConfigFlags.None, 0, int.MaxValue)]
        public int spawnInterval { get; private set; } = 10;

        [AutoConfig("Number of Imp Overlords that will spawn for each player.", AutoConfigFlags.None, 0, int.MaxValue)]
        public int impOverlordNumber { get; private set; } = 1;

        [AutoConfig("Number of Imps that will spawn for each player.", AutoConfigFlags.None, 0, int.MaxValue)]
        public int impNumber { get; private set; } = 4;

        [AutoConfig("Number of Rare items the Imp Overlord will have.", AutoConfigFlags.None, 0, int.MaxValue)]
        public int impOverlordRedItems { get; private set; } = 2;

        [AutoConfig("Number of Uncommon items the Imp Overlord will have.", AutoConfigFlags.None, 0, int.MaxValue)]
        public int impOverlordGreenItems { get; private set; } = 4;

        [AutoConfig("Number of Common items the Imp Overlord will have.", AutoConfigFlags.None, 0, int.MaxValue)]
        public int impOverlordWhiteItems { get; private set; } = 8;

        [AutoConfig("Number of Lunar items the Imp Overlord will have.", AutoConfigFlags.None, 0, int.MaxValue)]
        public int impOverlordBlueItems { get; private set; } = 0;

        [AutoConfig("Number of Boss items the Imp Overlord will have.", AutoConfigFlags.None, 0, int.MaxValue)]
        public int impOverlordYellowItems { get; private set; } = 0;

        [AutoConfig("Number of Rare items the Imp will have.", AutoConfigFlags.None, 0, int.MaxValue)]
        public int impRedItems { get; private set; } = 1;

        [AutoConfig("Number of Uncommon items the Imp will have.", AutoConfigFlags.None, 0, int.MaxValue)]
        public int impGreenItems { get; private set; } = 2;

        [AutoConfig("Number of Common items the Imp will have.", AutoConfigFlags.None, 0, int.MaxValue)]
        public int impWhiteItems { get; private set; } = 4;

        [AutoConfig("Number of Lunar items the Imp will have.", AutoConfigFlags.None, 0, int.MaxValue)]
        public int impBlueItems { get; private set; } = 0;

        [AutoConfig("Number of Boss items the Imp will have.", AutoConfigFlags.None, 0, int.MaxValue)]
        public int impYellowItems { get; private set; } = 0;

        [AutoConfig("Multiplier applied to Imp Overlords' health. Affected by difficulty coefficient. " +
                    "1 leaves them unmodified, but note that difficulty coefficient is still multiplied.",
                    AutoConfigFlags.None, 0f, float.MaxValue)]
        public float impOverlordHpMultiplier { get; private set; } = 1f;

        [AutoConfig("Multiplier applied to Imps' health. Affected by difficulty coefficient. " +
                    "1 leaves them unmodified, but note that difficulty coefficient is still multiplied.",
                    AutoConfigFlags.None, 0f, float.MaxValue)]
        public float impHpMultiplier { get; private set; } = 3f;

        [AutoConfig("Amount of time in seconds for each Imp to spawn apart from each other which adds a delay in between them " +
                    "instead of spawning them all at once to avoid frame drops. 0 will spawn them all almost instantly without delay.",
                    AutoConfigFlags.None, 0f, float.MaxValue)]
        public float intervalBetweenImps { get; private set; } = .3f;

        [AutoConfig("Type of spawning of the Imp Overlord. 0 = On the player, 1 = Randomly near the player." +
                    "Setting the spawn type to 0 sometimes causes the player to be catapulted off the map.",
                    AutoConfigFlags.None, 0, 1)]
        public int impOverlordSpawnArea { get; private set; } = 1;

        [AutoConfig("The mode of the Imp invasion. 0 = 1 batch of Origin Imps per player, 1 = 1 batch of Origin Imps in the map.",
                    AutoConfigFlags.None, 0, 1)]
        public int impInvasionBatchMode { get; private set; } = 0;

        [AutoConfig("Used for logging items that can be given to Imps. Makes it easier to report bugs related to items being received by Origin Imps.")]
        public bool logOriginItemList { get; private set; } = true;

        protected override string GetNameString(string langid = null) => displayName;

        protected override string GetDescString(string langid = null) => $"Imps will invade to destroy you every {spawnInterval} minutes.";

        public Origin()
        {
            iconResource = assetBundle.LoadAsset<Sprite>("Assets/ClassicItems/icons/origin_artifact_on_icon.png");
            iconResourceDisabled = assetBundle.LoadAsset<Sprite>("Assets/ClassicItems/Icons/origin_artifact_off_icon.png");
        }

        public override void SetupBehavior()
        {
            base.SetupBehavior();
            if (Compatibility.EnemyItemDisplays.enabled) Compatibility.EnemyItemDisplays.Setup();
            dropTable = Resources.Load<PickupDropTable>("DropTables/dtPearls");
            originOverlordSpawnCard =
                ImpOriginSetup(Resources.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscImpBoss"),
                               assetBundle.LoadAsset<Material>("Assets/ClassicItems/Imp/matImpBossOrigin.mat"),
                               assetBundle.LoadAsset<Texture>("Assets/ClassicItems/Imp/ImpBossBodyOrigin.png"),
                               "Imp Vanguard", "Reclaimer", 2);
            originImpSpawnCard =
                ImpOriginSetup(Resources.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscImp"),
                               assetBundle.LoadAsset<Material>("Assets/ClassicItems/Imp/matImpOrigin.mat"),
                               assetBundle.LoadAsset<Texture>("Assets/ClassicItems/Imp/ImpBodyOrigin.png"),
                               "Imp Soldier", "Defender", 0);
        }

        public override void Install()
        {
            base.Install();
            Run.onRunStartGlobal += Run_onRunStartGlobal;
            CharacterBody.onBodyStartGlobal += CharacterBody_onBodyStartGlobal;
        }

        public override void Uninstall()
        {
            base.Uninstall();
            Run.onRunStartGlobal -= Run_onRunStartGlobal;
            CharacterBody.onBodyStartGlobal -= CharacterBody_onBodyStartGlobal;
        }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        private CharacterSpawnCard ImpOriginSetup(CharacterSpawnCard origCsc, Material material, Texture icon, string name, string subtitle, int renderInfoIndex)
        {
            GameObject masterObject = origCsc.prefab;
            masterObject = masterObject.InstantiateClone(masterObject.name + originSuffix, true);
            masterObjects.Add(masterObject);
            CharacterMaster master = masterObject.GetComponent<CharacterMaster>();
            GameObject bodyObject = master.bodyPrefab;
            bodyObject = bodyObject.InstantiateClone(bodyObject.name + originSuffix, true);
            bodyObjects.Add(bodyObject);
            CharacterBody body = bodyObject.GetComponent<CharacterBody>();
            body.baseNameToken += originSuffix;
            body.subtitleNameToken += originSuffix;
            LanguageAPI.Add(body.baseNameToken, name);
            LanguageAPI.Add(body.subtitleNameToken, subtitle);
            body.portraitIcon = icon;
            ModelLocator bodyModelLocator = bodyObject.GetComponent<ModelLocator>();
            GameObject bodyModelTransformObject = bodyModelLocator.modelTransform.gameObject;
            CharacterModel bodyModel = bodyModelTransformObject.GetComponent<CharacterModel>();
            bodyModel.baseRendererInfos[renderInfoIndex].defaultMaterial = material;
            master.bodyPrefab = bodyObject;
            CharacterSpawnCard newCsc = ScriptableObject.CreateInstance<CharacterSpawnCard>();
            newCsc.name = origCsc.name + originSuffix;
            newCsc.prefab = masterObject;
            newCsc.sendOverNetwork = origCsc.sendOverNetwork;
            newCsc.hullSize = origCsc.hullSize;
            newCsc.nodeGraphType = origCsc.nodeGraphType;
            newCsc.requiredFlags = origCsc.requiredFlags;
            newCsc.forbiddenFlags = origCsc.forbiddenFlags;
            newCsc.directorCreditCost = origCsc.directorCreditCost;
            newCsc.occupyPosition = origCsc.occupyPosition;
            newCsc.loadout = origCsc.loadout;
            newCsc.noElites = origCsc.noElites;
            newCsc.forbiddenAsBoss = true;
            return newCsc;
        }

        private void Run_onRunStartGlobal(Run obj)
        {
            if (NetworkServer.active && IsActiveAndEnabled())
            {
                obj.gameObject.GetOrAddComponent<OriginManager>();
                if (logOriginItemList)
                {
                    Log.Message("Listing items that can be given to Origin Imps...");
                    Log.Message("COMMON:");
                    Log.MessageArray(OriginManager.whiteList, ListItemFormat);
                    Log.Message("UNCOMMON:");
                    Log.MessageArray(OriginManager.greenList, ListItemFormat);
                    Log.Message("RARE:");
                    Log.MessageArray(OriginManager.redList, ListItemFormat);
                    Log.Message("BOSS:");
                    Log.MessageArray(OriginManager.yellowList, ListItemFormat);
                    Log.Message("LUNAR:");
                    Log.MessageArray(OriginManager.blueList, ListItemFormat);
                }
            }
        }

        private void CharacterBody_onBodyStartGlobal(CharacterBody obj)
        {
            if (!NetworkServer.active || !obj.name.Contains(originSuffix) || !obj.master) return;
            GiveOriginItems(obj.master, obj.name.Contains("Boss"));
        }

        private void GiveOriginItems(CharacterMaster master, bool isLeader)
        {
            Inventory inv = master.inventory;
            int redCount = isLeader ? impOverlordRedItems : impRedItems;
            int greenCount = isLeader ? impOverlordGreenItems : impGreenItems;
            int whiteCount = isLeader ? impOverlordWhiteItems : impWhiteItems;
            int blueCount = isLeader ? impOverlordBlueItems : impBlueItems;
            int yellowCount = isLeader ? impOverlordYellowItems : impYellowItems;
#if DEBUG
            Log.Debug($"Giving items to {master.name} ({master.GetInstanceID()})");
#endif
            for (int i = 0; i < redCount; i++) inv.GiveItem(DecideRandomItem(OriginManager.redList));
            for (int i = 0; i < greenCount; i++) inv.GiveItem(DecideRandomItem(OriginManager.greenList));
            for (int i = 0; i < whiteCount; i++) inv.GiveItem(DecideRandomItem(OriginManager.whiteList));
            for (int i = 0; i < blueCount; i++) inv.GiveItem(DecideRandomItem(OriginManager.blueList));
            for (int i = 0; i < yellowCount; i++) inv.GiveItem(DecideRandomItem(OriginManager.yellowList));

            float hpBoost = Run.instance.difficultyCoefficient;
            if (isLeader) hpBoost *= impOverlordHpMultiplier;
            else hpBoost *= impHpMultiplier;
            inv.GiveItem(RoR2Items.BoostHp, Mathf.RoundToInt(hpBoost));
        }

        private ItemDef DecideRandomItem(ItemDef[] itemList)
        {
            ItemDef itemDef = itemList[Run.instance.spawnRng.RangeInt(0, itemList.Length)];
#if DEBUG
            Log.Debug($"Given {itemDef}.");
#endif
            return itemDef;
        }

        internal static bool DebugCheck()
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }

    internal class OriginManager : QueueProcessor<KeyValuePair<DirectorSpawnRequest, bool>>
    {
        public static ItemDef[] redList;
        public static ItemDef[] greenList;
        public static ItemDef[] whiteList;
        public static ItemDef[] blueList;
        public static ItemDef[] yellowList;

        private readonly Run run = Run.instance;
        private readonly Origin origin = Origin.instance;
        private int previousInvasionCycle = 0;
        private GameObject masterObject = null;
        private float? _processInterval = null;

        private readonly ItemDef[] bannedItems = new ItemDef[]
        {
            RoR2Items.GoldOnHit, RoR2Items.LunarTrinket, RoR2Items.FocusConvergence, RoR2Items.MonstersOnShrineUse,
            RoR2Items.TitanGoldDuringTP, RoR2Items.SprintWisp, RoR2Items.ArtifactKey, RoR2Items.SiphonOnLowHealth, RoR2Items.ScrapYellow,
            RoR2Items.AutoCastEquipment, RoR2Items.BonusGoldPackOnKill
        };

        protected override int itemsPerFrame { get; set; } = 1;

        protected override float processInterval
        {
            get
            {
                if (_processInterval == null) _processInterval = origin.intervalBetweenImps;
                return (float)_processInterval;
            }
            set => _processInterval = value;
        }

        protected override bool Process(KeyValuePair<DirectorSpawnRequest, bool> item)
        {
            masterObject = DirectorCore.instance.TrySpawnObject(item.Key);
            return masterObject;
        }

        protected override void OnSuccess(KeyValuePair<DirectorSpawnRequest, bool> item)
        {
            base.OnSuccess(item);
            if (masterObject && item.Value) GivePearlDrop(masterObject);
        }

        protected override void FixedUpdate()
        {
            if (gameObject)
            {
                int currentInvasionCycle = GetCurrentInvasionCycle();
                if (previousInvasionCycle < currentInvasionCycle)
                {
                    previousInvasionCycle = currentInvasionCycle;
                    ProcessInvasionMode(new Xoroshiro128Plus(run.seed + (ulong)currentInvasionCycle));
                }
                base.FixedUpdate();
            }
        }

        private void Awake()
        {
            redList = GenerateAvailableItems(run.availableTier3DropList);
            greenList = GenerateAvailableItems(run.availableTier2DropList);
            whiteList = GenerateAvailableItems(run.availableTier1DropList);
            blueList = GenerateAvailableItems(run.availableLunarDropList);
            yellowList = GenerateAvailableItems(run.availableBossDropList);
        }

        private void OnDestroy()
        {
            redList = null;
            greenList = null;
            whiteList = null;
            blueList = null;
            yellowList = null;
        }

        private int GetCurrentInvasionCycle()
        {
#if DEBUG
            return Mathf.FloorToInt(run.GetRunStopwatch() / 10);
#else
            return Mathf.FloorToInt(run.GetRunStopwatch() / (origin.spawnInterval * 60));
#endif
        }

        private void ProcessInvasionMode(Xoroshiro128Plus rng)
        {
            if (origin.impInvasionBatchMode == 0)
            {
                for (int i = 0; i < PlayerCharacterMasterController.instances.Count; i++)
                {
                    PerformInvasion(i, rng);
                }
            }
            else if (origin.impInvasionBatchMode == 1)
            {
                int playerCount = PlayerCharacterMasterController.instances.Count;
                int index = rng.RangeInt(0, playerCount);
                PerformInvasion(index, rng);
            }
        }

        private void PerformInvasion(int index, Xoroshiro128Plus rng)
        {
            PlayerCharacterMasterController pcmc = PlayerCharacterMasterController.instances[index];
            CharacterMaster master = pcmc.master;
            if (master.teamIndex == TeamIndex.Player)
            {
                CharacterBody body = master.GetBody();
                if (body) QueueImpArmy(body, Origin.originOverlordSpawnCard, Origin.originImpSpawnCard, rng);
            }
        }

        private void QueueImpArmy(CharacterBody body, CharacterSpawnCard leader, SpawnCard soldier, Xoroshiro128Plus rng)
        {
            Transform spawnOnTarget = body.coreTransform;
            for (int i = 0; i < origin.impOverlordNumber; i++)
            {
                DirectorCore.MonsterSpawnDistance input = DirectorCore.MonsterSpawnDistance.Close;
                DirectorPlacementRule directorPlacementRule = new DirectorPlacementRule
                {
                    spawnOnTarget = spawnOnTarget,
                    placementMode = SpawnAreaType()
                };
                DirectorCore.GetMonsterSpawnDistance(input, out directorPlacementRule.minDistance, out directorPlacementRule.maxDistance);
                DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(leader, directorPlacementRule, rng)
                {
                    teamIndexOverride = TeamIndex.Monster,
                    ignoreTeamMemberLimit = true
                };
                Add(new KeyValuePair<DirectorSpawnRequest, bool>(directorSpawnRequest, i == 0));
            }
            for (int i = 0; i < origin.impNumber; i++)
            {
                DirectorCore.MonsterSpawnDistance input = DirectorCore.MonsterSpawnDistance.Standard;
                DirectorPlacementRule directorPlacementRule = new DirectorPlacementRule
                {
                    spawnOnTarget = spawnOnTarget,
                    placementMode = PlacementMode.Approximate
                };
                DirectorCore.GetMonsterSpawnDistance(input, out directorPlacementRule.minDistance, out directorPlacementRule.maxDistance);
                DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(soldier, directorPlacementRule, rng)
                {
                    teamIndexOverride = TeamIndex.Monster,
                    ignoreTeamMemberLimit = true
                };
                Add(new KeyValuePair<DirectorSpawnRequest, bool>(directorSpawnRequest, false));
            }
        }

        private ItemDef[] GenerateAvailableItems(List<PickupIndex> list)
        {
            List<ItemDef> indices = new List<ItemDef>();
            foreach (PickupIndex pickup in list)
            {
                if (pickup.pickupDef != null)
                {
                    ItemIndex index = pickup.pickupDef.itemIndex;
                    ItemDef itemDef = ItemCatalog.GetItemDef(index);
                    if (IsItemAllowed(itemDef)) indices.Add(itemDef);
                }
            }
            return indices.ToArray();
        }

        private bool IsItemAllowed(ItemDef itemDef)
        {
            return itemDef != null
                   && !itemDef.ContainsTag(ItemTag.AIBlacklist)
                   && !itemDef.ContainsTag(ItemTag.EquipmentRelated)
                   && !itemDef.ContainsTag(ItemTag.SprintRelated)
                   && !bannedItems.Contains(itemDef);
        }

        private bool GivePearlDrop(GameObject masterObject)
        {
            CharacterMaster master = masterObject.GetComponent<CharacterMaster>();
            if (master)
            {
                GameObject bodyObject = master.GetBodyObject();
                if (bodyObject)
                {
                    bodyObject.AddComponent<OriginDrop>();
                    return true;
                }
            }
            return false;
        }

        private PlacementMode SpawnAreaType()
        {
            if (origin.impOverlordSpawnArea == 0) return PlacementMode.NearestNode;
            else return PlacementMode.Approximate;
        }
    }

    internal class OriginDrop : MonoBehaviour
    {
        private CharacterBody body;
        private HealthComponent healthComponent;

        private void Awake()
        {
            body = gameObject.GetComponent<CharacterBody>();
            healthComponent = gameObject.GetComponent<HealthComponent>();
        }

        private void FixedUpdate()
        {
            if (NetworkServer.active && !healthComponent.alive)
            {
                PickupIndex pickupIndex = Origin.dropTable.GenerateDrop(Origin.treasureRng);
                if (pickupIndex != PickupIndex.none)
                {
                    PickupDropletController.CreatePickupDroplet(pickupIndex, body.corePosition, Vector3.up * 20f);
                }
                Destroy(this);
            }
        }
    }
}