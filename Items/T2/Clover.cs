﻿using RoR2;
using System;
using BepInEx.Configuration;
using static ThinkInvisible.ClassicItems.MiscUtil;
using System.Collections.Generic;

namespace ThinkInvisible.ClassicItems {
    public class Clover : ItemBoilerplate<Clover> {
        public override string displayName {get;} = "56 Leaf Clover";

        [AutoItemCfg("Percent chance for a Clover drop to happen at first stack -- as such, multiplicative with Rare/Uncommon chances.", default, 0f, 100f)]
        public float baseChance {get;private set;} = 4f;
        [AutoItemCfg("Percent chance for a Clover drop to happen per extra stack.", default, 0f, 100f)]
        public float stackChance {get;private set;} = 1.5f;
        [AutoItemCfg("Maximum percent chance for a Clover drop on elite kill.", default, 0f, 100f)]
        public float capChance {get;private set;} = 100f;
        
        [AutoItemCfg("Percent chance for a Clover drop to become Tier 2 at first stack (if it hasn't already become Tier 3).", default, 0f, 100f)]
        public float baseUnc {get;private set;} = 1f;
        [AutoItemCfg("Percent chance for a Clover drop to become Tier 2 per extra stack.", default, 0f, 100f)]
        public float stackUnc {get;private set;} = 0.1f;
        [AutoItemCfg("Maximum percent chance for a Clover drop to become Tier 2.", default, 0f, 100f)]
        public float capUnc {get;private set;} = 25f;
        
        [AutoItemCfg("Percent chance for a Clover drop to become Tier 3 at first stack.", default, 0f, 100f)]
        public float baseRare {get;private set;} = 0.01f;
        [AutoItemCfg("Percent chance for a Clover drop to become Tier 3 per extra stack.", default, 0f, 100f)]
        public float stackRare {get;private set;} = 0.001f;
        [AutoItemCfg("Maximum percent chance for a Clover drop to become Tier 3.", default, 0f, 100f)]
        public float capRare {get;private set;} = 1f;

        [AutoItemCfg("Percent chance for a Tier 1 Clover drop to become Equipment instead.", default, 0f, 100f)]
        public float baseEqp {get;private set;} = 5f;

        [AutoItemCfg("If true, all clovers across all living players are counted towards item drops. If false, only the killer's items count.")]
        public bool globalStack {get;private set;} = true;

        public override void SetupConfigInner(ConfigFile cfl) {
            itemAIBDefault = true;
        }
        
        public override void SetupAttributesInner() {
            RegLang(
            	"Elite mobs have a chance to drop items.",
            	"Elites have a <style=cIsUtility>" + Pct(baseChance, 1, 1) + " chance</style> <style=cStack>(+" + Pct(stackChance, 1, 1) + " per stack COMBINED FOR ALL PLAYERS, up to " + Pct(capChance, 1, 1) + ")</style> to <style=cIsUtility>drop items</style> when <style=cIsDamage>killed</style>. <style=cStack>(Further stacks increase uncommon/rare chance up to " +Pct(capUnc,2,1) +" and "+Pct(capRare,3,1)+", respectively.)</style>",
            	"A relic of times long past (ClassicItems mod)");
            _itemTags = new List<ItemTag>{ItemTag.Utility};
            itemTier = ItemTier.Tier2;
        }

        public override void SetupBehaviorInner() {
            On.RoR2.DeathRewards.OnKilledServer += On_DROnKilledServer;
        }

        private void On_DROnKilledServer(On.RoR2.DeathRewards.orig_OnKilledServer orig, DeathRewards self, DamageReport damageReport) {
            orig(self, damageReport);

            if(damageReport == null) return;
            CharacterBody victimBody = damageReport.victimBody;
            if(victimBody == null || victimBody.teamComponent.teamIndex != TeamIndex.Monster || !victimBody.isElite) return;
            int numberOfClovers = 0;
            if(globalStack)
                foreach(CharacterMaster chrm in AliveList()) {
                    numberOfClovers += chrm?.inventory?.GetItemCount(regIndex) ?? 0;
                }
            else
                numberOfClovers += damageReport.attackerMaster?.inventory?.GetItemCount(regIndex) ?? 0;

            if(numberOfClovers == 0) return;

            float rareChance = Math.Min(baseRare + numberOfClovers * stackRare, capRare);
            float uncommonChance = Math.Min(baseUnc + numberOfClovers * stackUnc, capUnc);
            float anyDropChance = Math.Min(baseChance + numberOfClovers * stackChance, capChance);
            //Base drop chance is multiplicative with tier chances -- tier chances are applied to upgrade the dropped item

            if(Util.CheckRoll(anyDropChance)) {
                int tier;
                if(Util.CheckRoll(rareChance)) {
                    tier = 2;
                } else if(Util.CheckRoll(uncommonChance)) {
                    tier = 1;
                } else {
                    if(Util.CheckRoll(baseEqp))
                        tier = 4;
                    else
                        tier = 0;
                }
                SpawnItemFromBody(victimBody, tier);
            }

        }
    }
}
