#undef DEBUG

using RoR2;
using RoR2.Artifacts;
using System.Collections.Generic;
using System.Linq;
using TILER2;
using UnityEngine;

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

        protected override string GetNameString(string langid = null) => displayName;

        protected override string GetDescString(string langid = null) => $"Imps will invade to destroy you every {spawnInterval} minutes.";

        public static SpawnCard overlordSpawnCard { get; private set; }
        public static SpawnCard impSpawnCard { get; private set; }
        public static Material originImpOverlordMaterial { get; private set; }
        public static Material originImpMaterial { get; private set; }
        public static PickupDropTable dropTable { get; private set; }
        public static string originSuffix { get; private set; } = "(Origin)";
        public static Xoroshiro128Plus treasureRng { get; private set; } = new Xoroshiro128Plus(0UL);

        public Origin()
        {
            iconResourcePath = "@ChensClassicItems:Assets/ClassicItems/icons/origin_artifact_on_icon.png";
            iconResourcePathDisabled = "@ChensClassicItems:Assets/ClassicItems/Icons/origin_artifact_off_icon.png";
        }

        public override void SetupBehavior()
        {
            base.SetupBehavior();
            dropTable = Resources.Load<PickupDropTable>("DropTables/dtPearls");
            overlordSpawnCard = Resources.Load<SpawnCard>("spawncards/characterspawncards/cscImpBoss");
            impSpawnCard = Resources.Load<SpawnCard>("spawncards/characterspawncards/cscImp");
            originImpOverlordMaterial = Resources.Load<Material>("@ChensClassicItems:Assets/ClassicItems/Imp/matImpBossOrigin.mat");
            originImpMaterial = Resources.Load<Material>("@ChensClassicItems:Assets/ClassicItems/Imp/matImpOrigin.mat");
        }

        public override void Install()
        {
            base.Install();
            Run.onRunStartGlobal += Run_onRunStartGlobal;
            GlobalEventManager.onCharacterDeathGlobal += GlobalEventManager_onCharacterDeathGlobal;
        }

        public override void Uninstall()
        {
            base.Uninstall();
            Run.onRunStartGlobal -= Run_onRunStartGlobal;
            GlobalEventManager.onCharacterDeathGlobal -= GlobalEventManager_onCharacterDeathGlobal;
        }

        private void Run_onRunStartGlobal(Run obj)
        {
            if (IsActiveAndEnabled())
            {
                OriginManager.GetOrAddComponent(obj);
            }
        }

        private void GlobalEventManager_onCharacterDeathGlobal(DamageReport obj)
        {
            if (!IsActiveAndEnabled() || obj.victimTeamIndex == obj.attackerTeamIndex || !obj.victimMaster) return;
            if (!obj.victimMaster.name.Contains(originSuffix) || !obj.victimMaster.name.Contains("ImpBoss")) return;
            PickupIndex pickupIndex = dropTable.GenerateDrop(treasureRng);
            if (pickupIndex != PickupIndex.none)
            {
                PickupDropletController.CreatePickupDroplet(pickupIndex, obj.victimBody.corePosition, Vector3.up * 20f);
            }
        }
    }

    public class OriginManager : MonoBehaviour
    {
        private Run run;
        private int previousInvasionCycle = 0;
        private Origin origin = Origin.instance;
        private ItemIndex[] redList;
        private ItemIndex[] greenList;
        private ItemIndex[] whiteList;
        private ItemIndex[] blueList;
        private ItemIndex[] yellowList;

        private readonly ItemIndex[] bannedItems = new ItemIndex[]
        {
            ItemIndex.GoldOnHit, ItemIndex.LunarTrinket, ItemIndex.FocusConvergence, ItemIndex.MonstersOnShrineUse,
            ItemIndex.TitanGoldDuringTP, ItemIndex.SprintWisp, ItemIndex.ArtifactKey, ItemIndex.SiphonOnLowHealth, ItemIndex.ScrapYellow,
            ItemIndex.AutoCastEquipment
        };

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
            }
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
                    if (body) SpawnImpArmy(body, Origin.overlordSpawnCard, Origin.impSpawnCard, rng);
                }
            }
        }

        private void SpawnImpArmy(CharacterBody body, SpawnCard leader, SpawnCard soldier, Xoroshiro128Plus rng)
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
                GameObject leaderMasterObject = DirectorCore.instance.TrySpawnObject(directorSpawnRequest);
                CharacterMaster leaderMaster = leaderMasterObject.GetComponent<CharacterMaster>();
                if (!leaderMaster) return;
                GiveImpItems(leaderMaster, true);
                CharacterBody leaderBody = leaderMaster.GetBody();
                if (!leaderBody) return;
                PostProcess(leaderMaster, leaderBody, $"Imp Overlord {Origin.originSuffix}", Origin.originImpOverlordMaterial, 2);
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
                GameObject soldierMasterObject = DirectorCore.instance.TrySpawnObject(directorSpawnRequest);
                CharacterMaster soldierMaster = soldierMasterObject.GetComponent<CharacterMaster>();
                if (!soldierMaster) return;
                GiveImpItems(soldierMaster, false);
                CharacterBody soldierBody = soldierMaster.GetBody();
                if (!soldierBody) return;
                PostProcess(soldierMaster, soldierBody, $"Imp {Origin.originSuffix}", Origin.originImpMaterial, 0);
            }
        }

        private void GiveImpItems(CharacterMaster master, bool isLeader)
        {
            Inventory inv = master.inventory;
            int redCount = isLeader ? origin.impOverlordRedItems : origin.impRedItems;
            int greenCount = isLeader ? origin.impOverlordGreenItems : origin.impGreenItems;
            int whiteCount = isLeader ? origin.impOverlordWhiteItems : origin.impWhiteItems;
            int blueCount = isLeader ? origin.impOverlordBlueItems : origin.impBlueItems;
            int yellowCount = isLeader ? origin.impOverlordYellowItems : origin.impYellowItems;
            for (int i = 0; i < redCount; i++) inv.GiveItem(DecideRandomItem(redList));
            for (int i = 0; i < greenCount; i++) inv.GiveItem(DecideRandomItem(greenList));
            for (int i = 0; i < whiteCount; i++) inv.GiveItem(DecideRandomItem(whiteList));
            for (int i = 0; i < blueCount; i++) inv.GiveItem(DecideRandomItem(blueList));
            for (int i = 0; i < yellowCount; i++) inv.GiveItem(DecideRandomItem(yellowList));
            float hpBoost = run.difficultyCoefficient;
            if (isLeader) hpBoost *= origin.impOverlordHpMultiplier;
            else hpBoost *= origin.impHpMultiplier;
            inv.GiveItem(ItemIndex.BoostHp, Mathf.RoundToInt(hpBoost));
        }

        private ItemIndex DecideRandomItem(ItemIndex[] itemList)
        {
            ItemIndex index = itemList[run.spawnRng.RangeInt(0, itemList.Length)];
            return index;
        }

        private void PostProcess(CharacterMaster master, CharacterBody body, string displayName, Material material, int renderInfoIndex)
        {
            master.gameObject.name += Origin.originSuffix;
            body.gameObject.name += Origin.originSuffix;
            ModelLocator modelLocator = body.gameObject.GetComponent<ModelLocator>();
            CharacterModel model = modelLocator.modelTransform.gameObject.GetComponent<CharacterModel>();
            model.baseRendererInfos[renderInfoIndex].defaultMaterial = material;
            body.baseNameToken = displayName;
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

        public static OriginManager GetOrAddComponent(Run run)
        {
            return GetOrAddComponent(run.gameObject);
        }

        public static OriginManager GetOrAddComponent(GameObject runObject)
        {
            return runObject.GetComponent<OriginManager>() ?? runObject.AddComponent<OriginManager>();
        }
    }
}