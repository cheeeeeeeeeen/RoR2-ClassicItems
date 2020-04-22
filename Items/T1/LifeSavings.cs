﻿using RoR2;
using System;
using UnityEngine;
using BepInEx.Configuration;
using System.Collections.Generic;
using UnityEngine.Networking;
using static ThinkInvisible.ClassicItems.ClassicItemsPlugin.MasterItemList;
using R2API.Utils;

namespace ThinkInvisible.ClassicItems
{
    public class LifeSavings : ItemBoilerplate
    {
        public override string itemCodeName{get;} = "LifeSavings";

        private ConfigEntry<float> cfgGainPerSec;
        private ConfigEntry<int> cfgInvertCount;

        public float gainPerSec {get;private set;}
        public int invertCount {get;private set;}

        protected override void SetupConfigInner(ConfigFile cfl) {
            itemAIBDefault = true;

            cfgGainPerSec = cfl.Bind(new ConfigDefinition("Items." + itemCodeName, "GainPerSec"), 1f, new ConfigDescription(
                "Money to add to players per second per Life Savings stack (without taking into account InvertCount).",
                new AcceptableValueRange<float>(0f,float.MaxValue)));
            cfgInvertCount = cfl.Bind(new ConfigDefinition("Items." + itemCodeName, "InvertCount"), 3, new ConfigDescription(
                "With <InvertCount stacks, number of stacks affects time per interval instead of multiplying money gained.",
                new AcceptableValueRange<int>(0,int.MaxValue)));

            gainPerSec = cfgGainPerSec.Value;
            invertCount = cfgInvertCount.Value;
        }

        protected override void SetupAttributesInner() {
            modelPathName = "savingscard.prefab";
            iconPathName = "lifesavings_icon.png";
            RegLang("Life Savings",
            	"Earn gold over time.",
            	"Generates <style=cIsUtility>$" + gainPerSec + "</style> <style=cStack>(+$" + gainPerSec + " per stack)</style> every second.",
            	"A relic of times long past (ClassicItems mod)");
            _itemTags = new List<ItemTag>{ItemTag.Utility};
            itemTier = ItemTier.Tier1;
        }

        protected override void SetupBehaviorInner() {
            On.RoR2.CharacterBody.OnInventoryChanged += On_CBOnInventoryChanged;
            On.RoR2.SceneExitController.Begin += On_SECBegin;
            On.EntityStates.SpawnTeleporterState.OnExit += On_EntSTSOnExit;
        }
        private void On_EntSTSOnExit(On.EntityStates.SpawnTeleporterState.orig_OnExit orig, EntityStates.SpawnTeleporterState self) {
            orig(self);
            if(!NetworkServer.active) return;
            var cpt = self.outer.commonComponents.characterBody.GetComponent<LifeSavingsComponent>();
            if(cpt) cpt.holdIt = false;
        }

        private void On_SECBegin(On.RoR2.SceneExitController.orig_Begin orig, SceneExitController self) {
            orig(self);
            if(!NetworkServer.active) return;
            foreach(NetworkUser networkUser in NetworkUser.readOnlyInstancesList) {
				if(networkUser.master.hasBody) {
                    var cpt = networkUser.master.GetBody().GetComponent<LifeSavingsComponent>();
                    if(cpt) cpt.holdIt = true;
				}
            }
        }

        private void On_CBOnInventoryChanged(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self) {
            orig(self);
            var cpt = self.GetComponent<LifeSavingsComponent>();
            if(!cpt) self.gameObject.AddComponent<LifeSavingsComponent>();
        }
    }
        
    public class LifeSavingsComponent : NetworkBehaviour {
        private float moneyBuffer = 0f;
        [SyncVar]
        public bool holdIt = true; //https://www.youtube.com/watch?v=vDMwDT6BhhE

        #pragma warning disable IDE0051
        private void FixedUpdate() {
            var body = this.gameObject.GetComponent<CharacterBody>();
            if(body.inventory && body.master) {
                int icnt = lifeSavings.GetCount(body);
                if(icnt > 0)
                    moneyBuffer += Time.fixedDeltaTime * lifeSavings.gainPerSec * ((icnt < lifeSavings.invertCount)?(1f/(float)(lifeSavings.invertCount-icnt+1)):(icnt-lifeSavings.invertCount+1));
                //Disable during pre-teleport money drain so it doesn't softlock
                //Accumulator is emptied into actual money variable whenever a tick passes and it has enough for a change in integer value
                if(moneyBuffer >= 1.0f && !holdIt){
                    if(Compat_ShareSuite.enabled && Compat_ShareSuite.MoneySharing())
                        Compat_ShareSuite.GiveMoney((uint)Math.Floor(moneyBuffer));
                    else
                        body.master.GiveMoney((uint)Math.Floor(moneyBuffer));
                    moneyBuffer %= 1.0f;
                }
            }
        }
    }
}
