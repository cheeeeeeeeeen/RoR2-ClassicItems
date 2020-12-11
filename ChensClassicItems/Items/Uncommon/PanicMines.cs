using EntityStates;
using EntityStates.Engi.EngiWeapon;
using EntityStates.Engi.Mine;
using R2API;
using RoR2;
using RoR2.Projectile;
using System.Collections.ObjectModel;
using TILER2;
using UnityEngine;
using UnityEngine.Networking;
using static TILER2.MiscUtil;

namespace Chen.ClassicItems.Items.Uncommon
{
    /// <summary>
    /// Singleton item class powered by TILER2 that implements Panic Mines functionality.
    /// </summary>
    public class PanicMines : Item_V2<PanicMines>
    {
        /// <summary>
        /// The mine prefab used to deploy the mines triggered by Panic Mines.
        /// </summary>
        public static GameObject minePrefab { get; private set; }

        /// <summary>
        /// The ghost projectile prefab for the mine prefab of Panic Mines.
        /// </summary>
        public static GameObject mineGhostPrefab { get; private set; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override string displayName => "Panic Mines";
        public override ItemTier itemTier => ItemTier.Tier2;
        public override ReadOnlyCollection<ItemTag> itemTags => new ReadOnlyCollection<ItemTag>(new[] { ItemTag.Damage });

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Fraction of max health required as damage taken to drop a mine.", AutoConfigFlags.None, 0f, 1f)]
        public float healthThreshold { get; private set; } = 0.1f;

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Base AoE damage coefficient of the panic mine.", AutoConfigFlags.None, 0f, float.MaxValue)]
        public float baseDmg { get; private set; } = 5f;

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Stack increase of the AoE damage coefficient.", AutoConfigFlags.None, 0f, float.MaxValue)]
        public float stackDmg { get; private set; } = 0f;

        [AutoConfig("If true, damage to shield and barrier (from e.g. Personal Shield Generator, Topaz Brooch) will not count towards triggering Panic Mines.")]
        public bool requireHealth { get; private set; } = true;

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Determines if the Panic Mine will self-destruct when the owner is lost. This will deal no damage.")]
        public bool selfDestructOnLostOwner { get; private set; } = false;

        protected override string GetNameString(string langid = null) => displayName;

        protected override string GetPickupString(string langid = null) => "Drop mines when taking heavy damage.";

        protected override string GetDescString(string langid = null)
        {
            string desc = "<style=cDeath>When hit";
            if (healthThreshold > 0f) desc += $" for more than {Pct(healthThreshold)} of max health</style>";
            desc += $", drop <style=cIsDamage>1</style> mine <style=cStack>(+1 per stack)</style> with <style=cIsDamage>{Pct(baseDmg)}</style>";
            if (stackDmg > 0f) desc += " <style=cStack>(+" + Pct(stackDmg) + " per stack)</style>";
            desc += " damage.";
            if (selfDestructOnLostOwner) desc += " <style=cDeath>The mine will be destroyed shortly after the owner dies.</style>";
            return desc;
        }

        protected override string GetLoreString(string langid = null) =>
            "\"Must be strapped onto vehicles, NOT personnel! After taking heavy fire, the automatic dispenser should drop down and arm a mine to make a hasty retreat (Or blow enemies sky-high who are dumb enough to follow.)" +
            " Includes smart-fire, but leave the blast radius regardless. The laws of physics don't pick sides. Very high yield for how small it is." +
            " If you want to use it offensively, then... well, just get shot. A lot. Preferably by small arms fire, or you'll die trying to have the mines drop.\"\n\n" +
            "\"Seriously?\" I said to myself upon reading the details that is attached to what it looked like an odd proximity mine.\n\n" +
            "But if there is one thing about surviving in this damnable place, then I should trust that this equipment will prove itself useful.";

        public override void SetupBehavior()
        {
            base.SetupBehavior();
            GameObject engiMinePrefab = Resources.Load<GameObject>("prefabs/projectiles/EngiMine");
            minePrefab = engiMinePrefab.InstantiateClone("PanicMine");
            Object.Destroy(minePrefab.GetComponent<ProjectileDeployToOwner>());

            GameObject engiMineGhostPrefab = Resources.Load<GameObject>("prefabs/projectileghosts/EngiMineGhost");
            mineGhostPrefab = engiMineGhostPrefab.InstantiateClone("PanicMineGhost", false);
            SkinnedMeshRenderer mesh = mineGhostPrefab.GetComponentInChildren<SkinnedMeshRenderer>();
            mesh.material.color = new Color32(255, 168, 0, 255);
            minePrefab.GetComponent<ProjectileController>().ghostPrefab = mineGhostPrefab;

            ProjectileCatalog.getAdditionalEntries += list => list.Add(minePrefab);

            if (Compat_ItemStats.enabled)
            {
                Compat_ItemStats.CreateItemStatDef(itemDef,
                (
                    (count, inv, master) => { return baseDmg + (count - 1) * stackDmg; },
                    (value, inv, master) => { return $"Damage: {Pct(value, 1)}"; }
                ),
                (
                    (count, inv, master) => { return count; },
                    (value, inv, master) => { return $"Mines: {value}"; }
                ));
            }
        }

        public override void Install()
        {
            base.Install();
            On.RoR2.HealthComponent.TakeDamage += On_HCTakeDamage;
            On.EntityStates.Engi.Mine.MineArmingWeak.FixedUpdate += On_ESMineArmingWeak;
        }

        public override void Uninstall()
        {
            base.Uninstall();
            On.RoR2.HealthComponent.TakeDamage -= On_HCTakeDamage;
            On.EntityStates.Engi.Mine.MineArmingWeak.FixedUpdate -= On_ESMineArmingWeak;
        }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

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
            for (int t = 0; t < icnt; t++)
            {
                ProjectileManager.instance.FireProjectile(minePrefab, corePos, MineDropDirection(),
                                                          vGameObject, DamageCalculation(vBody.damage, icnt),
                                                          200f, Util.CheckRoll(vBody.crit, vBody.master),
                                                          DamageColorIndex.Item, null, -1f);
            }
        }

        private void On_ESMineArmingWeak(On.EntityStates.Engi.Mine.MineArmingWeak.orig_FixedUpdate orig, MineArmingWeak self)
        {
            if (self.outer.name != "PanicMine(Clone)") orig(self);
            else
            {
                if (selfDestructOnLostOwner && NetworkServer.active && !self.projectileController.owner)
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
                if (self.blastRadiusScale != 1.2f) self.blastRadiusScale = 1.2f;
                if (self.forceScale != 1f) self.forceScale = 1f;
                if (self.damageScale != 1f) self.damageScale = 1f;
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

        private float DamageCalculation(float characterDamage, int stack)
        {
            return characterDamage * (baseDmg + stackDmg * (stack - 1));
        }
    }
}