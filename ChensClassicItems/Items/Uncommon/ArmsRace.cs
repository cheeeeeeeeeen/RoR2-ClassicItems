using Chen.ClassicItems.Items.Common;
using EntityStates.Drone.DroneWeapon;
using EntityStates.Squid.SquidWeapon;
using RoR2;
using RoR2.Projectile;
using System.Collections.ObjectModel;
using TILER2;
using UnityEngine;
using static TILER2.MiscUtil;
using MageWeapon = EntityStates.Mage.Weapon;

namespace Chen.ClassicItems.Items.Uncommon
{
    /// <summary>
    /// Singleton item class powered by TILER2 that implements Arms Race functionality.
    /// </summary>
    public class ArmsRace : Item_V2<ArmsRace>
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override string displayName => "Arms Race";
        public override ItemTier itemTier => ItemTier.Tier2;
        public override ReadOnlyCollection<ItemTag> itemTags => new ReadOnlyCollection<ItemTag>(new[] { ItemTag.Damage });

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("The chance for drones to launch a mortar. Stacks multiplicatively.", AutoConfigFlags.None, 0f, 100f)]
        public float mortarProcChance { get; private set; } = 5f;

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("The chance for drones to launch a missile. Stacks multiplicatively.", AutoConfigFlags.None, 0f, 100f)]
        public float missileProcChance { get; private set; } = 4f;

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Mortar Damage Coefficient.", AutoConfigFlags.None, 0f, float.MaxValue)]
        public float mortarDamage { get; private set; } = 1.5f;

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Stacking value of Mortar Damage Coefficient. Linear.", AutoConfigFlags.None, 0f, float.MaxValue)]
        public float mortarStackDamage { get; private set; } = 0f;

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Missile Damage Coefficient.", AutoConfigFlags.None, 0f, float.MaxValue)]
        public float missileDamage { get; private set; } = 1.5f;

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Stacking value of Missile Damage Coefficient. Linear.", AutoConfigFlags.None, 0f, float.MaxValue)]
        public float missileStackDamage { get; private set; } = 0f;

        [AutoConfig("Determines whether Squid Polyp will work with Arms Race.")]
        public bool allowSquidPolyps { get; private set; } = false;

        public override bool itemIsAIBlacklisted { get; protected set; } = true;

        protected override string GetNameString(string langid = null) => displayName;

        protected override string GetPickupString(string langid = null) => "Drones are equipped with explosive weaponry.";

        protected override string GetDescString(string langid = null)
        {
            string desc = $"Owned Drones and Turrets have a <style=cIsDamage>{Pct(mortarProcChance, 0, 1)}</style>";
            desc += $" <style=cStack>(+{Pct(mortarProcChance, 0, 1)} per stack, multiplicative)</style> chance";
            desc += $" to launch a mortar and a <style=cIsDamage>{Pct(missileProcChance, 0, 1)}</style>";
            desc += $" <style=cStack>(+{Pct(missileProcChance, 0, 1)} per stack, multiplicative)</style> chance";
            desc += $" to fire a missile for <style=cIsDamage>{Pct(mortarDamage, 0)}</style>";
            if (mortarStackDamage > 0) desc += $" <style=cStack>(+{Pct(mortarStackDamage, 0)} per stack)</style>";
            desc += $" and <style=cIsDamage>{Pct(missileDamage, 0)}</style>";
            if (missileStackDamage > 0) desc += $" <style=cStack>(+{Pct(missileStackDamage, 0)} per stack)</style>";
            desc += " respectively.";
            return desc;
        }

        protected override string GetLoreString(string langid = null) =>
            "\"Psst. Hey. It's me again. You guessed it right: I'm whispering over text once again. It's a habit of mine.\"\n\n" +
            "\"Here are the upgraded drone parts. Since KS-I slotted drones are the trend, I assumed that the drones you own are of the same type. " +
            "Hence, the drone parts being compatible to KS-I only.\"\n\n" +
            "\"Hear me out, there's more. I upgraded the A.I. in it. The A.I. should be able to hide the weapons in these containers. " +
            "You still need to strap it to the drone, though, but hey, it would not be too obvious that the drone is weaponized. It also looks less lamer.\"\n\n" +
            "\"Keep it up. We will not falter in this arms race, although it looks dark for us. Think positive.\"";

        public override void SetupBehavior()
        {
            base.SetupBehavior();

            MortarTube.SetupMortarProjectile();

            if (Compat_ItemStats.enabled)
            {
                Compat_ItemStats.CreateItemStatDef(itemDef,
                (
                    (count, inv, master) => { return ProcComputation(mortarProcChance, (int)count); },
                    (value, inv, master) => { return $"Mortar Firing Chance: {Pct(value, 0, 1)}"; }
                ),
                (
                    (count, inv, master) => { return ProcComputation(missileProcChance, (int)count); },
                    (value, inv, master) => { return $"Missile Firing Chance: {Pct(value, 0, 1)}"; }
                ),
                (
                    (count, inv, master) => { return mortarDamage + (count - 1) * mortarStackDamage; },
                    (value, inv, master) => { return $"Mortar Damage: {Pct(value, 0)}"; }
                ),
                (
                    (count, inv, master) => { return missileDamage + (count - 1) * missileStackDamage; },
                    (value, inv, master) => { return $"Missile Damage: {Pct(value, 0)}"; }
                ));
            }
        }

        public override void Install()
        {
            base.Install();
            On.EntityStates.Drone.DroneWeapon.FireGatling.OnEnter += FireGatling_OnEnter;
            On.EntityStates.Drone.DroneWeapon.FireTurret.OnEnter += FireTurret_OnEnter;
            On.EntityStates.Drone.DroneWeapon.FireMegaTurret.FireBullet += FireMegaTurret_FireBullet;
            On.EntityStates.Drone.DroneWeapon.FireMissileBarrage.FireMissile += FireMissileBarrage_FireMissile;
            On.EntityStates.Drone.DroneWeapon.FireTwinRocket.FireProjectile += FireTwinRocket_FireProjectile;
            On.EntityStates.Mage.Weapon.Flamethrower.FireGauntlet += Flamethrower_FireGauntlet;
            On.EntityStates.Squid.SquidWeapon.FireSpine.FireOrbArrow += FireSpine_FireOrbArrow;
        }

        public override void Uninstall()
        {
            base.Uninstall();
            On.EntityStates.Drone.DroneWeapon.FireGatling.OnEnter -= FireGatling_OnEnter;
            On.EntityStates.Drone.DroneWeapon.FireTurret.OnEnter -= FireTurret_OnEnter;
            On.EntityStates.Drone.DroneWeapon.FireMegaTurret.FireBullet -= FireMegaTurret_FireBullet;
            On.EntityStates.Drone.DroneWeapon.FireMissileBarrage.FireMissile -= FireMissileBarrage_FireMissile;
            On.EntityStates.Drone.DroneWeapon.FireTwinRocket.FireProjectile -= FireTwinRocket_FireProjectile;
            On.EntityStates.Mage.Weapon.Flamethrower.FireGauntlet -= Flamethrower_FireGauntlet;
            On.EntityStates.Squid.SquidWeapon.FireSpine.FireOrbArrow -= FireSpine_FireOrbArrow;
        }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        private void Flamethrower_FireGauntlet(On.EntityStates.Mage.Weapon.Flamethrower.orig_FireGauntlet orig, MageWeapon.Flamethrower self, string muzzleString)
        {
            orig(self, muzzleString);
            if (self.characterBody.name.Contains("FlameDrone") && self.characterBody.master.name.Contains("FlameDrone"))
            {
                TriggerArtillery(self.characterBody, self.tickDamageCoefficient * self.damageStat, self.isCrit);
            }
        }

        private void FireSpine_FireOrbArrow(On.EntityStates.Squid.SquidWeapon.FireSpine.orig_FireOrbArrow orig, FireSpine self)
        {
            orig(self);
            if (!allowSquidPolyps) return;
            TriggerArtillery(self.characterBody, self.damageStat * FireSpine.damageCoefficient, Util.CheckRoll(self.critStat, self.characterBody.master));
        }

        private void FireGatling_OnEnter(On.EntityStates.Drone.DroneWeapon.FireGatling.orig_OnEnter orig, FireGatling self)
        {
            orig(self);
            TriggerArtillery(self.characterBody, FireGatling.damageCoefficient * self.damageStat, Util.CheckRoll(self.critStat, self.characterBody.master));
        }

        private void FireTurret_OnEnter(On.EntityStates.Drone.DroneWeapon.FireTurret.orig_OnEnter orig, FireTurret self)
        {
            orig(self);
            TriggerArtillery(self.characterBody, FireTurret.damageCoefficient * self.damageStat, Util.CheckRoll(self.critStat, self.characterBody.master));
        }

        private void FireMegaTurret_FireBullet(On.EntityStates.Drone.DroneWeapon.FireMegaTurret.orig_FireBullet orig, FireMegaTurret self, string muzzleString)
        {
            orig(self, muzzleString);
            TriggerArtillery(self.characterBody, FireMegaTurret.damageCoefficient * self.damageStat, Util.CheckRoll(self.critStat, self.characterBody.master));
        }

        private void FireMissileBarrage_FireMissile(On.EntityStates.Drone.DroneWeapon.FireMissileBarrage.orig_FireMissile orig, FireMissileBarrage self, string targetMuzzle)
        {
            orig(self, targetMuzzle);
            TriggerArtillery(self.characterBody, self.damageStat * FireMissileBarrage.damageCoefficient, Util.CheckRoll(self.critStat, self.characterBody.master));
        }

        private void FireTwinRocket_FireProjectile(On.EntityStates.Drone.DroneWeapon.FireTwinRocket.orig_FireProjectile orig, FireTwinRocket self, string muzzleString)
        {
            orig(self, muzzleString);
            TriggerArtillery(self.characterBody, self.damageStat * FireTwinRocket.damageCoefficient, Util.CheckRoll(self.critStat, self.characterBody.master));
        }

        private float ProcComputation(float procChance, int stack)
        {
            return (1f - Mathf.Pow(1 - procChance / 100f, stack)) * 100f;
        }

        private void ProcMissile(CharacterBody attackerBody, ProcChainMask procChainMask, float damage, bool crit, int stack)
        {
            GameObject gameObject = attackerBody.gameObject;
            InputBankTest component = gameObject.GetComponent<InputBankTest>();
            Vector3 position = component ? component.aimOrigin : gameObject.transform.position;
            float dmgCoef = missileDamage + (missileStackDamage * (stack - 1));
            damage *= dmgCoef;
            ProcChainMask procChainMask2 = procChainMask;
            procChainMask2.AddProc(ProcType.Missile);
            FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
            {
                projectilePrefab = GlobalEventManager.instance.missilePrefab,
                position = position,
                rotation = Util.QuaternionSafeLookRotation(Vector3.up),
                procChainMask = procChainMask2,
                owner = gameObject,
                damage = damage,
                crit = crit,
                force = 200f,
                damageColorIndex = DamageColorIndex.Item
            };
            ProjectileManager.instance.FireProjectile(fireProjectileInfo);
        }

        private void LaunchMortar(CharacterBody attackerBody, ProcChainMask procChainMask, float damage, bool crit, int stack)
        {
            GameObject gameObject = attackerBody.gameObject;
            InputBankTest component = gameObject.GetComponent<InputBankTest>();
            Vector3 position = component ? component.aimOrigin : gameObject.transform.position;
            Vector3 direction;
            if (MortarTube.instance.fixedAim) direction = gameObject.transform.forward;
            else direction = component ? component.aimDirection : gameObject.transform.forward;
            direction = direction.normalized + new Vector3(0f, MortarTube.instance.launchAngle, 0f);
            float inaccuracyRate = MortarTube.instance.inaccuracyRate;
            direction += new Vector3(Random.Range(-inaccuracyRate, inaccuracyRate),
                                     Random.Range(-inaccuracyRate, inaccuracyRate),
                                     Random.Range(-inaccuracyRate, inaccuracyRate));
            float dmgCoef = mortarDamage + (mortarStackDamage * (stack - 1));
            damage *= dmgCoef;
            ProcChainMask procChainMask2 = procChainMask;
            procChainMask2.AddProc(ProcType.Missile);
            FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
            {
                projectilePrefab = MortarTube.mortarPrefab,
                position = position,
                procChainMask = procChainMask2,
                owner = gameObject,
                damage = damage,
                crit = crit,
                force = 500f,
                damageColorIndex = DamageColorIndex.Item,
                speedOverride = -1f,
                damageTypeOverride = DamageType.AOE,
                rotation = Util.QuaternionSafeLookRotation(direction)
            };
            ProjectileManager.instance.FireProjectile(fireProjectileInfo);
        }

        /// <summary>
        /// Used to trigger Arms Race effect in launching artillery.
        /// </summary>
        /// <param name="body">The drone's body</param>
        /// <param name="damage">Damage to be computed against the coefficients of Arms Race projectiles</param>
        /// <param name="crit">Determines if this should be a critical hit</param>
        /// <param name="procChainMask">The proc chain mask</param>
        public void TriggerArtillery(CharacterBody body, float damage, bool crit, ProcChainMask procChainMask = default)
        {
            if (damage <= 0 || !body.master || !body.master.minionOwnership || !body.master.minionOwnership.ownerMaster
                || procChainMask.HasProc(ProcType.Missile)) return;
            int itemCount = GetCount(body.master.minionOwnership.ownerMaster);
            if (itemCount <= 0) return;
            if (Util.CheckRoll(ProcComputation(mortarProcChance, itemCount), body.master))
            {
                LaunchMortar(body, procChainMask, damage, crit, itemCount);
            }
            if (Util.CheckRoll(ProcComputation(missileProcChance, itemCount), body.master))
            {
                ProcMissile(body, procChainMask, damage, crit, itemCount);
            }
        }
    }
}