#undef DEBUG

using R2API;
using RoR2;
using RoR2.Artifacts;
using System.Collections.Generic;
using System.Linq;
using TILER2;
using UnityEngine;
using UnityEngine.Networking;

namespace Chen.ClassicItems
{
    public class Origin : Artifact_V2<Origin>
    {
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
        public float intervalBetweenImps { get; private set; } = .25f;

        protected override string GetNameString(string langid = null) => displayName;

        protected override string GetDescString(string langid = null) => $"Imps will invade to destroy you every {spawnInterval} minutes.";

        public static PickupDropTable dropTable { get; private set; }
        public static string originSuffix { get; private set; } = "(Origin)";
        public static Xoroshiro128Plus treasureRng { get; private set; } = new Xoroshiro128Plus(0UL);
        public static CharacterSpawnCard originOverlordSpawnCard { get; private set; }
        public static CharacterSpawnCard originImpSpawnCard { get; private set; }

        public Origin()
        {
            iconResourcePath = "@ChensClassicItems:Assets/ClassicItems/icons/origin_artifact_on_icon.png";
            iconResourcePathDisabled = "@ChensClassicItems:Assets/ClassicItems/Icons/origin_artifact_off_icon.png";
        }

        public override void SetupBehavior()
        {
            base.SetupBehavior();
            EnemyItemDisplaysCompatibility.Setup();
            dropTable = Resources.Load<PickupDropTable>("DropTables/dtPearls");
            originOverlordSpawnCard =
                ImpOriginSetup(Resources.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscImpBoss"),
                               Resources.Load<Material>("@ChensClassicItems:Assets/ClassicItems/Imp/matImpBossOrigin.mat"),
                               Resources.Load<Texture>("@ChensClassicItems:Assets/ClassicItems/Imp/ImpBossBodyOrigin.png"),
                               "Imp Vanguard", "Reclaimer", 2);
            originImpSpawnCard =
                ImpOriginSetup(Resources.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscImp"),
                               Resources.Load<Material>("@ChensClassicItems:Assets/ClassicItems/Imp/matImpOrigin.mat"),
                               Resources.Load<Texture>("@ChensClassicItems:Assets/ClassicItems/Imp/ImpBodyOrigin.png"),
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

        private CharacterSpawnCard ImpOriginSetup(CharacterSpawnCard origCsc, Material material, Texture icon, string name, string subtitle, int renderInfoIndex)
        {
            GameObject masterObject = origCsc.prefab;
            masterObject = masterObject.InstantiateClone(masterObject.name + originSuffix);
            MasterCatalog.getAdditionalEntries += (list) => { list.Add(masterObject); };
            CharacterMaster master = masterObject.GetComponent<CharacterMaster>();
            GameObject bodyObject = master.bodyPrefab;
            bodyObject = bodyObject.InstantiateClone(bodyObject.name + originSuffix);
            BodyCatalog.getAdditionalEntries += (list) => { list.Add(bodyObject); };
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
            if (IsActiveAndEnabled())
            {
                OriginManager.GetOrAddComponent(obj);
            }
        }

        private void CharacterBody_onBodyStartGlobal(CharacterBody obj)
        {
            if (!obj.name.Contains(originSuffix) || !obj.master) return;
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
#if DEBUG
            Log.Debug($"Done. ({master.name} - {master.GetInstanceID()})");
#endif
            float hpBoost = Run.instance.difficultyCoefficient;
            if (isLeader) hpBoost *= impOverlordHpMultiplier;
            else hpBoost *= impHpMultiplier;
            inv.GiveItem(ItemIndex.BoostHp, Mathf.RoundToInt(hpBoost));
        }

        private ItemIndex DecideRandomItem(ItemIndex[] itemList)
        {
            ItemIndex index = itemList[Run.instance.spawnRng.RangeInt(0, itemList.Length)];
#if DEBUG
            Log.Debug($"Given {index}.");
#endif
            return index;
        }
    }

    public class OriginManager : MonoBehaviour
    {
        public static ItemIndex[] redList;
        public static ItemIndex[] greenList;
        public static ItemIndex[] whiteList;
        public static ItemIndex[] blueList;
        public static ItemIndex[] yellowList;

        private Run run;
        private int previousInvasionCycle = 0;
        private Origin origin = Origin.instance;
        private float intervalTimer = 0f;

        private readonly ItemIndex[] bannedItems = new ItemIndex[]
        {
            ItemIndex.GoldOnHit, ItemIndex.LunarTrinket, ItemIndex.FocusConvergence, ItemIndex.MonstersOnShrineUse,
            ItemIndex.TitanGoldDuringTP, ItemIndex.SprintWisp, ItemIndex.ArtifactKey, ItemIndex.SiphonOnLowHealth, ItemIndex.ScrapYellow,
            ItemIndex.AutoCastEquipment
        };

        private readonly List<KeyValuePair<DirectorSpawnRequest, bool>> spawnQueue = new List<KeyValuePair<DirectorSpawnRequest, bool>>();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by UnityEngine")]
        private void Awake()
        {
            run = Run.instance;
            origin = Origin.instance;
            if (MonsterTeamGainsItemsArtifactManager.availableTier1Items.Length <= 0) MonsterTeamGainsItemsArtifactManager.GenerateAvailableItemsSet();
            redList = MonsterTeamGainsItemsArtifactManager.availableTier3Items;
            greenList = MonsterTeamGainsItemsArtifactManager.availableTier2Items;
            whiteList = MonsterTeamGainsItemsArtifactManager.availableTier1Items;
            blueList = GenerateAvailableItems(run.availableLunarDropList);
            yellowList = GenerateAvailableItems(run.availableBossDropList);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by UnityEngine")]
        private void FixedUpdate()
        {
            if (gameObject)
            {
                int currentInvasionCycle = GetCurrentInvasionCycle();
                if (previousInvasionCycle < currentInvasionCycle)
                {
                    previousInvasionCycle = currentInvasionCycle;
                    PerformInvasion(new Xoroshiro128Plus(run.seed + (ulong)currentInvasionCycle));
                }
                if (spawnQueue.Count > 0)
                {
                    intervalTimer += Time.fixedDeltaTime;
                    if (intervalTimer >= origin.intervalBetweenImps)
                    {
                        GameObject masterObject = DirectorCore.instance.TrySpawnObject(spawnQueue[0].Key);
                        if (masterObject && spawnQueue[0].Value) GivePearlDrop(masterObject);
                        spawnQueue.RemoveAt(0);
                        intervalTimer = 0f;
                    }
                }
                else if (intervalTimer != 0f) intervalTimer = 0f;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by UnityEngine")]
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

        private void PerformInvasion(Xoroshiro128Plus rng)
        {
            for (int i = CharacterMaster.readOnlyInstancesList.Count - 1; i >= 0; i--)
            {
                CharacterMaster master = CharacterMaster.readOnlyInstancesList[i];
                if (master.teamIndex == TeamIndex.Player && master.playerCharacterMasterController)
                {
                    CharacterBody body = master.GetBody();
                    if (body) SpawnImpArmy(body, Origin.originOverlordSpawnCard, Origin.originImpSpawnCard, rng);
                }
            }
        }

        private void SpawnImpArmy(CharacterBody body, CharacterSpawnCard leader, SpawnCard soldier, Xoroshiro128Plus rng)
        {
            Transform spawnOnTarget = body.coreTransform;
            for (int i = 0; i < origin.impOverlordNumber; i++)
            {
                DirectorCore.MonsterSpawnDistance input = DirectorCore.MonsterSpawnDistance.Close;
                DirectorPlacementRule directorPlacementRule = new DirectorPlacementRule
                {
                    spawnOnTarget = spawnOnTarget,
                    placementMode = DirectorPlacementRule.PlacementMode.NearestNode
                };
                DirectorCore.GetMonsterSpawnDistance(input, out directorPlacementRule.minDistance, out directorPlacementRule.maxDistance);
                DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(leader, directorPlacementRule, rng)
                {
                    teamIndexOverride = TeamIndex.Monster,
                    ignoreTeamMemberLimit = true
                };
                spawnQueue.Add(new KeyValuePair<DirectorSpawnRequest, bool>(directorSpawnRequest, i == 0));
            }
            for (int i = 0; i < origin.impNumber; i++)
            {
                DirectorCore.MonsterSpawnDistance input = DirectorCore.MonsterSpawnDistance.Standard;
                DirectorPlacementRule directorPlacementRule = new DirectorPlacementRule
                {
                    spawnOnTarget = spawnOnTarget,
                    placementMode = DirectorPlacementRule.PlacementMode.Approximate
                };
                DirectorCore.GetMonsterSpawnDistance(input, out directorPlacementRule.minDistance, out directorPlacementRule.maxDistance);
                DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(soldier, directorPlacementRule, rng)
                {
                    teamIndexOverride = TeamIndex.Monster,
                    ignoreTeamMemberLimit = true
                };
                spawnQueue.Add(new KeyValuePair<DirectorSpawnRequest, bool>(directorSpawnRequest, false));
            }
        }

        private ItemIndex[] GenerateAvailableItems(List<PickupIndex> list)
        {
            List<ItemIndex> indices = new List<ItemIndex>();
            foreach (PickupIndex pickup in list)
            {
                if (pickup.pickupDef == null) continue;
                ItemIndex index = pickup.pickupDef.itemIndex;
                ItemDef itemDef = ItemCatalog.GetItemDef(index);
                if (itemDef == null || itemDef.ContainsTag(ItemTag.AIBlacklist)) continue;
                if (!bannedItems.Contains(index)) indices.Add(index);
            }
            return indices.ToArray();
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

        public static OriginManager GetOrAddComponent(Run run)
        {
            return GetOrAddComponent(run.gameObject);
        }

        public static OriginManager GetOrAddComponent(GameObject runObject)
        {
            return runObject.GetComponent<OriginManager>() ?? runObject.AddComponent<OriginManager>();
        }
    }

    public class OriginDrop : MonoBehaviour
    {
        private CharacterBody body;
        private HealthComponent healthComponent;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by UnityEngine")]
        private void Awake()
        {
            body = gameObject.GetComponent<CharacterBody>();
            healthComponent = gameObject.GetComponent<HealthComponent>();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by UnityEngine")]
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