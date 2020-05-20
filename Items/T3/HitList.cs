﻿using RoR2;
using UnityEngine;
using System.Collections.ObjectModel;
using UnityEngine.Networking;
using RoR2.Orbs;
using TILER2;
using static TILER2.MiscUtil;
using R2API;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System;
using System.Linq;
using System.Collections.Generic;

namespace ThinkInvisible.ClassicItems {
    public class HitList : Item<HitList> {
        public override string displayName => "The Hit List";
		public override ItemTier itemTier => ItemTier.Tier3;
		public override ReadOnlyCollection<ItemTag> itemTags => new ReadOnlyCollection<ItemTag>(new[]{ItemTag.Damage});
        
        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Time per batch of marked enemies.",AutoItemConfigFlags.None,0f,float.MaxValue)]
        public float cooldown {get;private set;} = 10f;
        
        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken | AutoUpdateEventFlags.InvalidateStats)]
        [AutoItemConfig("Additive bonus to base damage per marked enemy killed.",AutoItemConfigFlags.PreventNetMismatch,0f,float.MaxValue)]
        public float procDamage {get;private set;} = 0.5f;
        
        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken | AutoUpdateEventFlags.InvalidateStats)]
        [AutoItemConfig("Maximum damage bonus from The Hit List.",AutoItemConfigFlags.PreventNetMismatch,0f,float.MaxValue)]
        public float maxDamage {get;private set;} = 20f;

        protected override string NewLangName(string langid = null) => displayName;
        protected override string NewLangPickup(string langid = null) => "Killing marked enemies permanently increases damage.";
        protected override string NewLangDesc(string langid = null) => "Every <style=cIsUtility>" + cooldown.ToString("N0") + " seconds</style>, <style=cIsUtility>1</style> <style=cStack>(+1 per stack)</style> random enemy will be <style=cIsUtility>marked</style> for the same duration. Killing <style=cIsUtility>marked</style> enemies gives you <style=cIsDamage>+" + procDamage.ToString("N1") + " permanent base damage</style> <style=cStack>(max. " + maxDamage.ToString("N1") + ")</style>.";
        protected override string NewLangLore(string langid = null) => "A relic of times long past (ClassicItems mod)";

        public ItemIndex hitListTally {get; private set;}
        public BuffIndex markDebuff {get; private set;}
        public BuffIndex tallyBuff {get; private set;}

        public HitList() {
            onAttrib += (tokenIdent, namePrefix) => {
                var markDebuffDef = new CustomBuff(new BuffDef {
                    buffColor = Color.yellow,
                    canStack = false,
                    isDebuff = true,
                    name = "CIHitListDebuff",
                    iconPath = "@ClassicItems:Assets/ClassicItems/icons/hitlist_debuff_icon.png"
                });
                markDebuff = BuffAPI.Add(markDebuffDef);
                
                var tallyBuffDef = new CustomBuff(new BuffDef {
                    buffColor = Color.yellow,
                    canStack = true,
                    isDebuff = false,
                    name = "CIHitListBuff",
                    iconPath = "@ClassicItems:Assets/ClassicItems/icons/hitlist_buff_icon.png"
                });
                tallyBuff = BuffAPI.Add(tallyBuffDef);

                var hitListTallyDef = new CustomItem(new ItemDef {
                    hidden = true,
                    name = "CIINTERNALTally",
                    tier = ItemTier.NoTier,
                    canRemove = false
                }, new ItemDisplayRuleDict(null));
                hitListTally = ItemAPI.Add(hitListTallyDef);
            };
        }

        protected override void LoadBehavior() {
            GlobalEventManager.onCharacterDeathGlobal += Evt_GEMOnCharacterDeathGlobal;
            On.RoR2.Run.FixedUpdate += On_RunFixedUpdate;
            IL.RoR2.CharacterBody.RecalculateStats += IL_CBRecalcStats;
        }

        protected override void UnloadBehavior() {
            GlobalEventManager.onCharacterDeathGlobal -= Evt_GEMOnCharacterDeathGlobal;
            On.RoR2.Run.FixedUpdate -= On_RunFixedUpdate;
            IL.RoR2.CharacterBody.RecalculateStats -= IL_CBRecalcStats;
        }

        private float stopwatch = 0f;
        private void On_RunFixedUpdate(On.RoR2.Run.orig_FixedUpdate orig, Run self) {
            orig(self);
            stopwatch -= Time.fixedDeltaTime;
            if(stopwatch > 0f) return;

            stopwatch = cooldown;
            var alive = AliveList();
            int[] totalVsTeam = {0, 0, 0};
            int totalVsAll = 0;
            foreach(var cm in alive) {
                var icnt = GetCount(cm);
                if(FriendlyFireManager.friendlyFireMode != FriendlyFireManager.FriendlyFireMode.Off) {
                    totalVsAll += icnt;
                }  else {
                    if(cm.teamIndex != TeamIndex.Neutral) totalVsTeam[0] += icnt;
                    if(cm.teamIndex != TeamIndex.Player) totalVsTeam[1] += icnt;
                    if(cm.teamIndex != TeamIndex.Monster) totalVsTeam[2] += icnt;
                }
            }
            if(totalVsAll > 0) {
                for(var i = 0; i < totalVsAll; i++) {
                    if(alive.Count <= 0) break;
                    var next = itemRng.NextElementUniform(alive);
                    if(next.hasBody)
                        next.GetBody().AddTimedBuff(markDebuff, cooldown);
                    else
                        i--;
                    alive.Remove(next);
                }
            } else {
                List<CharacterMaster>[] aliveTeam = {
                    alive.Where(cm => cm.teamIndex == TeamIndex.Neutral).ToList(),
                    alive.Where(cm => cm.teamIndex == TeamIndex.Player).ToList(),
                    alive.Where(cm => cm.teamIndex == TeamIndex.Monster).ToList()
                };
                for(var list = 0; list <= 2; list++) {
                    for(var i = 0; i < totalVsTeam[list]; i++) {                        
                        if(aliveTeam[list].Count <= 0) break;
                        var next = itemRng.NextElementUniform(aliveTeam[list]);
                        if(next.hasBody)
                            next.GetBody().AddTimedBuff(markDebuff, cooldown);
                        else
                            i--;
                        aliveTeam[list].Remove(next);
                    }
                }
            }
        }
        
        private void Evt_GEMOnCharacterDeathGlobal(DamageReport rep) {
            if((rep.victimBody?.HasBuff(markDebuff) ?? false) && GetCount(rep.attackerBody) > 0)
                rep.attackerBody.inventory.GiveItem(hitListTally);
        }

        private void IL_CBRecalcStats(ILContext il) {
            var c = new ILCursor(il);

            bool ILFound = c.TryGotoNext(MoveType.After,
                x=>x.MatchLdarg(0),
                x=>x.MatchLdfld<CharacterBody>("baseDamage"));

            if(ILFound) {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<float,CharacterBody,float>>((baseDamage, cb) => {
                    var ret = baseDamage;
                    var add = Mathf.Clamp(procDamage * (cb.inventory?.GetItemCount(hitListTally) ?? 0), 0f, maxDamage);
                    ret += add;
                    cb.SetBuffCount(tallyBuff, Mathf.FloorToInt(add/procDamage));
                    return ret;
                });
            } else {
                Debug.LogError("ClassicItems: failed to apply The Hit List IL patch (base damage modifier)");
            }
        }
    }
}