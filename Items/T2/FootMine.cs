﻿using EntityStates;
using EntityStates.Engi.EngiWeapon;
using EntityStates.Engi.Mine;
using R2API;
using RoR2;
using RoR2.Projectile;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TILER2;
using UnityEngine;
using UnityEngine.Networking;
using static TILER2.MiscUtil;

namespace Chen.ClassicItems
{
    public class FootMine : Item<FootMine>
    {
        public override string displayName => "Dead Man's Foot";
        public override ItemTier itemTier => ItemTier.Tier2;
        public override ReadOnlyCollection<ItemTag> itemTags => new ReadOnlyCollection<ItemTag>(new[] { ItemTag.Damage });

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Fraction of max health required as damage taken to drop a poison mine.", AutoItemConfigFlags.None, 0f, 1f)]
        public float healthThreshold { get; private set; } = 0.1f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Base poison damage coefficient dealt to affected enemies.", AutoItemConfigFlags.None, 0f, float.MaxValue)]
        public float baseDmg { get; private set; } = 1.5f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Stack amount for the poison damage coefficient.", AutoItemConfigFlags.None, 0f, float.MaxValue)]
        public float stackDmg { get; private set; } = 0f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Number of poison ticks.", AutoItemConfigFlags.None, 0, int.MaxValue)]
        public int baseTicks { get; private set; } = 4;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Stack increase of poison ticks.", AutoItemConfigFlags.None, 0, int.MaxValue)]
        public int stackTicks { get; private set; } = 1;

        [AutoItemConfig("If true, damage to shield and barrier (from e.g. Personal Shield Generator, Topaz Brooch) will not count towards triggering Dead Man's Foot.")]
        public bool requireHealth { get; private set; } = true;

        protected override string NewLangName(string langid = null) => displayName;

        protected override string NewLangPickup(string langid = null) => "Drop a poison mine when taking heavy damage.";

        protected override string NewLangDesc(string langid = null)
        {
            string desc = $"<style=cDeath>When hit for more than {Pct(healthThreshold)} max health</style>, drop a poison mine with <style=cIsDamage>{Pct(baseDmg)}</style>";
            if (stackDmg > 0f) desc += $" <style=cStack>(+{Pct(stackDmg)} per stack)</style>";
            desc += $" damage per second. Poison lasts for <style=cIsDamage>{baseTicks - 1}</style>";
            if (stackTicks > 0) desc += $" <style=cStack>(+{stackTicks} per stack)</style>";
            desc += "<style=cIsDamage> seconds</style>. Poison is <style=cIsUtility>stackable</style>. <style=cDeath>The mine will be destroyed shortly after the owner dies.</style>";
            return desc;
        }

        protected override string NewLangLore(string langid = null) =>
            "I feel like my allies are losing it. Truly, this place is hell to begin with. We are always on the brink of our deaths. " +
            "I can't even believe I find myself writing this journal as all hell is about to break loose any second.\n\n" +
            "As I look towards a friend who appears to be going insane, he is holding a... foot. A foot!?\n\n" +
            "I ask, \"Why are you carrying something disgusting and is that Clark's!?\n\n" +
            "He looked at me while shrugging." +
            "\"He's dead,\" he told me while sighing, as if he expected it, \"Curiosity killed the cat. Approached one of those hideous bugs, infested him, then he exploded.\"" +
            "I stayed silent, confused as to what I should really feel at the moment: disgust or sadness? I don't know." +
            "\"His foot can be a good trap. We need everything in order to survive.\"";

        private static GameObject minePrefab;
        private static GameObject mineGhostPrefab;
        private static BuffIndex poisonBuff;
        private static DotController.DotIndex poisonDot;

        public FootMine()
        {
            GameObject engiMinePrefab = Resources.Load<GameObject>("prefabs/projectiles/EngiMine");
            minePrefab = engiMinePrefab.InstantiateClone("FootMine");
            Object.Destroy(minePrefab.GetComponent<ProjectileDeployToOwner>());

            GameObject engiMineGhostPrefab = Resources.Load<GameObject>("prefabs/projectileghosts/EngiMineGhost");
            mineGhostPrefab = engiMineGhostPrefab.InstantiateClone("FootMineGhost", false);
            SkinnedMeshRenderer mesh = mineGhostPrefab.GetComponentInChildren<SkinnedMeshRenderer>();
            mesh.material.color = Color.green;
            minePrefab.GetComponent<ProjectileController>().ghostPrefab = mineGhostPrefab;

            CustomBuff poisonBuffDef = new CustomBuff(new BuffDef
            {
                //buffColor = new Color32(1, 121, 91, 255),
                canStack = true,
                isDebuff = true,
                name = "CCIFootPoison",
                iconPath = "@ChensClassicItems:Assets/ClassicItems/icons/footmine_buff_icon.png"
            });
            poisonBuff = BuffAPI.Add(poisonBuffDef);

            DotController.DotDef poisonDotDef = new DotController.DotDef
            {
                interval = 1,
                damageCoefficient = 1,
                damageColorIndex = DamageColorIndex.Poison,
                associatedBuff = poisonBuff
            };
            poisonDot = DotAPI.RegisterDotDef(poisonDotDef);

            onBehav += () =>
            {
                if (Compat_ItemStats.enabled)
                {
                    Compat_ItemStats.CreateItemStatDef(regItem.ItemDef,
                    (
                        (count, inv, master) => { return baseDmg + (count - 1) * stackDmg; },
                        (value, inv, master) => { return $"Poison Damage/Second: {Pct(value, 1)}"; }
                    ),
                    (
                        (count, inv, master) => { return baseTicks + (count - 1) * stackTicks; },
                        (value, inv, master) => { return $"Duration: {value} seconds"; }
                    ));
                }
            };
        }

        protected override void LoadBehavior()
        {
            On.RoR2.HealthComponent.TakeDamage += On_HCTakeDamage;
            On.EntityStates.Engi.Mine.MineArmingWeak.FixedUpdate += On_ESMineArmingWeak;
            On.EntityStates.Engi.Mine.Detonate.Explode += On_ESDetonate;
        }

        protected override void UnloadBehavior()
        {
            On.RoR2.HealthComponent.TakeDamage -= On_HCTakeDamage;
            On.EntityStates.Engi.Mine.MineArmingWeak.FixedUpdate -= On_ESMineArmingWeak;
            On.EntityStates.Engi.Mine.Detonate.Explode -= On_ESDetonate;
        }

        private void On_HCTakeDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo di)
        {
            var oldHealth = self.health;
            var oldCH = self.combinedHealth;
            CharacterBody vBody = self.body;
            GameObject vGameObject = self.gameObject;

            orig(self, di);

            int icnt = GetCount(vBody);
            if (icnt < 1
                || (requireHealth && (oldHealth - self.health) / self.fullHealth < healthThreshold)
                || (!requireHealth && (oldCH - self.combinedHealth) / self.fullCombinedHealth < healthThreshold))
                return;

            Vector3 corePos = Util.GetCorePosition(vBody);

            Util.PlaySound(FireMines.throwMineSoundString, vGameObject);
            ProjectileManager.instance.FireProjectile(minePrefab, corePos, MineDropDirection(),
                                                      vGameObject, icnt, 50f, false,
                                                      DamageColorIndex.Item, null, -1f);
        }

        private void On_ESMineArmingWeak(On.EntityStates.Engi.Mine.MineArmingWeak.orig_FixedUpdate orig, MineArmingWeak self)
        {
            if (self.outer.name != "FootMine(Clone)") orig(self);
            else if (NetworkServer.active)
            {
                if (!self.projectileController.owner)
                {
                    if (Detonate.explosionEffectPrefab)
                    {
                        EffectManager.SpawnEffect(Detonate.explosionEffectPrefab, new EffectData
                        {
                            origin = self.transform.position,
                            rotation = self.transform.rotation,
                            scale = Detonate.blastRadius * 0.3f
                        }, true);
                    }
                    EntityState.Destroy(self.gameObject);
                }
            }
        }

        private void On_ESDetonate(On.EntityStates.Engi.Mine.Detonate.orig_Explode orig, Detonate self)
        {
            if (self.outer.name != "FootMine(Clone)") orig(self);
            else if (NetworkServer.active)
            {
                List<TeamComponent> teamMembers = new List<TeamComponent>();
                TeamFilter teamFilter = self.GetComponent<TeamFilter>();
                float blastRadius = Detonate.blastRadius * 1.2f;
                float sqrad = blastRadius * blastRadius;
                bool isFF = FriendlyFireManager.friendlyFireMode != FriendlyFireManager.FriendlyFireMode.Off;
                GameObject owner = self.projectileController.owner;
                int icnt = (int)self.GetComponent<ProjectileDamage>().damage; // this is actually the stack number

                if (isFF || teamFilter.teamIndex != TeamIndex.Monster) teamMembers.AddRange(TeamComponent.GetTeamMembers(TeamIndex.Monster));
                if (isFF || teamFilter.teamIndex != TeamIndex.Neutral) teamMembers.AddRange(TeamComponent.GetTeamMembers(TeamIndex.Neutral));
                if (isFF || teamFilter.teamIndex != TeamIndex.Player) teamMembers.AddRange(TeamComponent.GetTeamMembers(TeamIndex.Player));
                if (owner) teamMembers.Remove(owner.GetComponent<TeamComponent>());

                foreach (TeamComponent tcpt in teamMembers)
                {
                    if ((tcpt.transform.position - self.transform.position).sqrMagnitude <= sqrad)
                    {
                        if (tcpt.body && tcpt.body.mainHurtBox && tcpt.body.isActiveAndEnabled)
                        {
                            DotController.InflictDot(tcpt.gameObject, owner, poisonDot, baseTicks + stackTicks * (icnt - 1), baseDmg + stackDmg * (icnt - 1));
                        }
                    }
                }
                EntityState.Destroy(self.gameObject);
            }
        }

        private Quaternion MineDropDirection()
        {
            return Util.QuaternionSafeLookRotation(
                new Vector3(Random.Range(-1f, 1f),
                            -1f,
                            Random.Range(-1f, 1f))
            );
        }
    }
}