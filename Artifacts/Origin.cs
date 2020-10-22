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
        public int impOverlordRedItems { get; private set; } = 4;

        [AutoConfig("Number of Uncommon items the Imp Overlord will have.", AutoConfigFlags.None, 0, int.MaxValue)]
        public int impOverlordGreenItems { get; private set; } = 8;

        [AutoConfig("Number of Common items the Imp Overlord will have.", AutoConfigFlags.None, 0, int.MaxValue)]
        public int impOverlordWhiteItems { get; private set; } = 12;

        [AutoConfig("Number of Lunar items the Imp Overlord will have.", AutoConfigFlags.None, 0, int.MaxValue)]
        public int impOverlordBlueItems { get; private set; } = 0;

        [AutoConfig("Number of Boss items the Imp Overlord will have.", AutoConfigFlags.None, 0, int.MaxValue)]
        public int impOverlordYellowItems { get; private set; } = 1;

        [AutoConfig("Number of Rare items the Imp will have.", AutoConfigFlags.None, 0, int.MaxValue)]
        public int impRedItems { get; private set; } = 2;

        [AutoConfig("Number of Uncommon items the Imp will have.", AutoConfigFlags.None, 0, int.MaxValue)]
        public int impGreenItems { get; private set; } = 4;

        [AutoConfig("Number of Common items the Imp will have.", AutoConfigFlags.None, 0, int.MaxValue)]
        public int impWhiteItems { get; private set; } = 6;

        [AutoConfig("Number of Lunar items the Imp will have.", AutoConfigFlags.None, 0, int.MaxValue)]
        public int impBlueItems { get; private set; } = 0;

        [AutoConfig("Number of Boss items the Imp will have.", AutoConfigFlags.None, 0, int.MaxValue)]
        public int impYellowItems { get; private set; } = 0;

        protected override string GetNameString(string langid = null) => displayName;

        protected override string GetDescString(string langid = null) => $"Imps will invade to destroy you every {spawnInterval} minutes.";

        public Origin()
        {
            iconResourcePath = "@ChensClassicItems:Assets/ClassicItems/icons/origin_artifact_on_icon.png";
            iconResourcePathDisabled = "@ChensClassicItems:Assets/ClassicItems/Icons/origin_artifact_off_icon.png";
        }

        public override void Install()
        {
            base.Install();
            Run.onRunStartGlobal += Run_onRunStartGlobal;
        }

        public override void Uninstall()
        {
            base.Uninstall();
            Run.onRunStartGlobal -= Run_onRunStartGlobal;
        }

        private void Run_onRunStartGlobal(Run obj)
        {
            if (IsActiveAndEnabled())
            {
                OriginManager.GetOrAddComponent(obj);
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
            return Mathf.FloorToInt(run.GetRunStopwatch() / (origin.spawnInterval * 60));
        }

        private void PerformInvasion(Xoroshiro128Plus rng)
        {
            SpawnCard overlordSpawnCard = Resources.Load<SpawnCard>("spawncards/characterspawncards/cscImpBoss");
            SpawnCard impSpawnCard = Resources.Load<SpawnCard>("spawncards/characterspawncards/cscImp");
            if (!overlordSpawnCard || !impSpawnCard)
            {
                Log.Warning("OriginManager.PerformInvasion: There were missing Spawn Cards!");
                return;
            }
            for (int i = CharacterMaster.readOnlyInstancesList.Count - 1; i >= 0; i--)
            {
                CharacterMaster master = CharacterMaster.readOnlyInstancesList[i];
                if (master.teamIndex == TeamIndex.Player && master.playerCharacterMasterController)
                {
                    CharacterBody body = master.GetBody();
                    if (body) SpawnImpArmy(body, overlordSpawnCard, impSpawnCard, rng);
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
                if (leaderMasterObject) GiveImpItems(leaderMasterObject, true);
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
                if (soldierMasterObject) GiveImpItems(soldierMasterObject, false, i);
            }
        }

        private void GiveImpItems(GameObject masterObject, bool isLeader, int numbering = -1)
        {
            CharacterMaster master = masterObject.GetComponent<CharacterMaster>();
            if (!master) return;
            Inventory inv = master.inventory;
            string name = isLeader ? "Imp Overlord" : "Imp";
            if (numbering >= 0) name += $" {numbering + 1}";
            int redCount = isLeader ? origin.impOverlordRedItems : origin.impRedItems;
            int greenCount = isLeader ? origin.impOverlordGreenItems : origin.impGreenItems;
            int whiteCount = isLeader ? origin.impOverlordWhiteItems : origin.impWhiteItems;
            int blueCount = isLeader ? origin.impOverlordBlueItems : origin.impBlueItems;
            int yellowCount = isLeader ? origin.impOverlordYellowItems : origin.impYellowItems;
            for (int i = 0; i < redCount; i++) inv.GiveItem(DecideRandomItem(redList, name));
            for (int i = 0; i < greenCount; i++) inv.GiveItem(DecideRandomItem(greenList, name));
            for (int i = 0; i < whiteCount; i++) inv.GiveItem(DecideRandomItem(whiteList, name));
            for (int i = 0; i < blueCount; i++) inv.GiveItem(DecideRandomItem(blueList, name));
            for (int i = 0; i < yellowCount; i++) inv.GiveItem(DecideRandomItem(yellowList, name));
            inv.GiveRandomEquipment();
            float hpBoost = run.difficultyCoefficient;
            if (isLeader) hpBoost *= 3f;
            else hpBoost *= 12f;
            inv.GiveItem(ItemIndex.BoostHp, (int)hpBoost);
        }

        private ItemIndex DecideRandomItem(ItemIndex[] itemList, string name = null)
        {
            ItemIndex index = itemList[run.spawnRng.RangeInt(0, itemList.Length)];
            if (name != null) Log.Message($"OriginManager: Given {name} a {ItemCatalog.GetItemDef(index).name}.");
            return index;
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