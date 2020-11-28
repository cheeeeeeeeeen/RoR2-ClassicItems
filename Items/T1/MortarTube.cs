using R2API;
using RoR2;
using RoR2.Projectile;
using System.Collections.ObjectModel;
using TILER2;
using UnityEngine;
using UnityEngine.Networking;
using static TILER2.MiscUtil;

namespace Chen.ClassicItems
{
    /// <summary>
    /// Singleton item class powered by TILER2 that implements Mortar Tube functionality.
    /// </summary>
    public class MortarTube : Item_V2<MortarTube>
    {
        /// <summary>
        /// Contains the mortar projectile prefab. Must invoke SetupMortarProjectile() for it to be initialized.
        /// </summary>
        public static GameObject mortarPrefab { get; private set; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override string displayName => "Mortar Tube";
        public override ItemTier itemTier => ItemTier.Tier1;
        public override ReadOnlyCollection<ItemTag> itemTags => new ReadOnlyCollection<ItemTag>(new[] { ItemTag.Damage });

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Base percent chance of launch a mortar. Affected by proc coefficient.", AutoConfigFlags.None, 0f, 100f)]
        public float procChance { get; private set; } = 9f;

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Added to ProcChance per extra stack of Mortar Tube.", AutoConfigFlags.None, 0f, 100f)]
        public float stackChance { get; private set; } = 0f;

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Maximum allowed ProcChance for Mortar Tube.", AutoConfigFlags.None, 0f, 100f)]
        public float capChance { get; private set; } = 100f;

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Damage coefficient of each missile.", AutoConfigFlags.None, 0f, float.MaxValue)]
        public float dmgCoefficient { get; private set; } = 1.7f;

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Stack amount of Damage coefficient. Linear.", AutoConfigFlags.None, 0f, float.MaxValue)]
        public float dmgStack { get; private set; } = 1.7f;

        [AutoConfig("Velocity multiplier for the mortar. Lower value means it moves slower.", AutoConfigFlags.None, 0f, float.MaxValue)]
        public float velocityMultiplier { get; private set; } = .5f;

        [AutoConfig("How heavy the mortar is. Higher means it is heavier. This is not a percentage nor a multiplier.", AutoConfigFlags.None, 0f, float.MaxValue)]
        public float gravityAmount { get; private set; } = .5f;

        [AutoConfig("Setting to true would launch the mortar at a fixed angle regardless of aim. Setting to false would launch the mortar relative to aim.")]
        public bool fixedAim { get; private set; } = false;

        [AutoConfig("The angle from where the mortar is launched. 1 means completely up. -1 means completely down.", AutoConfigFlags.None, float.MinValue, float.MaxValue)]
        public float launchAngle { get; private set; } = .9f;

        [AutoConfig("Inaccuracy of the mortar. Higher value means it's more inaccurate.", AutoConfigFlags.None, 0f, float.MaxValue)]
        public float inaccuracyRate { get; private set; } = .05f;

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Amount of mortars launched per stack. It can be set to 0.5 to fire another mortar for every 2nd Mortar Tube gained (excluding the first).",
                    AutoConfigFlags.None, 0f, float.MaxValue)]
        public float stackAmount { get; private set; } = 0f;

        protected override string GetNameString(string langID = null) => displayName;

        protected override string GetPickupString(string langID = null) => "Chance to launch a mortar.";

        protected override string GetDescString(string langID = null)
        {
            string desc = $"<style=cIsDamage>{Pct(procChance, 0, 1)}</style>";
            if (stackChance > 0f) desc += $" <style=cStack>(+{Pct(stackChance, 0, 1)} per stack, up to {Pct(capChance, 0, 1)})</style>";
            desc += $" chance to launch a mortar that deals <style=cIsDamage>{Pct(dmgCoefficient, 0)}</style>";
            if (dmgStack > 0f) desc += $" <style=cStack>(+{Pct(dmgStack, 0)} per stack)</style>";
            desc += ". Affected by proc coefficient. The mortar deals an AoE damage.";
            if (stackAmount > 0) desc += " More mortars may be launched upon stacking.";
            return desc;
        }

        protected override string GetLoreString(string langID = null) =>
            "\"A very primitive weapon, all manual labor. Put the explosive down the end, then fire.\"\n\n" +
            "\"That sounds highly dangerous. I would not recommend it. There are far more advanced weapons that we can use.\"\n\n" +
            "\"What a waste. It could have been good for artillery support.\"\n\n" +
            "\"We don't exactly need artillery support in this case. We only need to survive and complete our mission.\"\n\n" +
            "\"If you say so... You can still hit your enemies with it, at least.\"\n\n" +
            "\"I'll take it.\"\n\n" +
            "A Mortar Tube, huh? I never knew they actually existed. Is it really a relic of history now? I wonder if it really works. It did look simple... and old.";

        public override void SetupBehavior()
        {
            base.SetupBehavior();

            SetupMortarProjectile();

            if (Compat_ItemStats.enabled)
            {
                Compat_ItemStats.CreateItemStatDef(itemDef,
                (
                    (count, inv, master) => { return Mathf.Min(procChance + stackChance * (count - 1), capChance); },
                    (value, inv, master) => { return $"Firing Chance: {Pct(value, 0, 1)}"; }
                ),
                (
                    (count, inv, master) => { return dmgCoefficient + (count - 1) * dmgStack; },
                    (value, inv, master) => { return $"Damage: {Pct(value, 0)}"; }
                ),
                (
                    (count, inv, master) => { return Mathf.Floor(1 + stackAmount * (count - 1)); },
                    (value, inv, master) => { return $"Mortars: {value}"; }
                ));
            }
            if (Compat_BetterUI.enabled)
            {
                Compat_BetterUI.AddEffect(catalogIndex, procChance, stackChance, Compat_BetterUI.ChanceFormatter, Compat_BetterUI.LinearStacking,
                    (value, extraStackValue, procCoefficient) =>
                    {
                        return Mathf.CeilToInt((capChance - value * procCoefficient) / (extraStackValue * procCoefficient)) + 1;
                    });
            }
        }

        public override void Install()
        {
            base.Install();
            On.RoR2.GlobalEventManager.OnHitEnemy += On_GEMOnHitEnemy;
        }

        public override void Uninstall()
        {
            base.Uninstall();
            On.RoR2.GlobalEventManager.OnHitEnemy -= On_GEMOnHitEnemy;
        }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        private void On_GEMOnHitEnemy(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            orig(self, damageInfo, victim);

            if (!NetworkServer.active || !victim || !damageInfo.attacker || damageInfo.procCoefficient <= 0f || damageInfo.procChainMask.HasProc(ProcType.Missile)) return;

            var vicb = victim.GetComponent<CharacterBody>();

            CharacterBody body = damageInfo.attacker.GetComponent<CharacterBody>();
            if (!body || !vicb || !vicb.healthComponent || !vicb.mainHurtBox) return;

            CharacterMaster chrm = body.master;
            if (!chrm) return;

            int icnt = GetCount(body);
            if (icnt == 0) return;

            icnt--;
            float m2Proc = procChance;
            if (icnt > 0) m2Proc += stackChance * icnt;
            if (m2Proc > capChance) m2Proc = capChance;
            if (!Util.CheckRoll(m2Proc * damageInfo.procCoefficient, chrm)) return;
            LaunchMortar(body, damageInfo.procChainMask, victim, damageInfo, icnt);
        }

        private void LaunchMortar(CharacterBody attackerBody, ProcChainMask procChainMask, GameObject victim, DamageInfo damageInfo, int stack)
        {
            GameObject gameObject = attackerBody.gameObject;
            InputBankTest component = gameObject.GetComponent<InputBankTest>();
            Vector3 position = component ? component.aimOrigin : gameObject.transform.position;

            float dmgCoef = dmgCoefficient + (dmgStack * stack);
            float damage = Util.OnHitProcDamage(damageInfo.damage, attackerBody.damage, dmgCoef);
            ProcChainMask procChainMask2 = procChainMask;
            procChainMask2.AddProc(ProcType.Missile);
            FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
            {
                projectilePrefab = mortarPrefab,
                position = position,
                procChainMask = procChainMask2,
                target = victim,
                owner = gameObject,
                damage = damage,
                crit = damageInfo.crit,
                force = 500f,
                damageColorIndex = DamageColorIndex.Item,
                speedOverride = -1f,
                damageTypeOverride = DamageType.AOE
            };
            int times = (int)(1 + stackAmount * stack);
            for (int t = 0; t < times; t++)
            {
                Vector3 direction;
                if (fixedAim) direction = gameObject.transform.forward;
                else direction = component ? component.aimDirection : gameObject.transform.forward;
                direction = direction.normalized + new Vector3(0f, launchAngle, 0f);
                direction += new Vector3(Random.Range(-inaccuracyRate, inaccuracyRate),
                                         Random.Range(-inaccuracyRate, inaccuracyRate),
                                         Random.Range(-inaccuracyRate, inaccuracyRate));
                fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(direction);
                ProjectileManager.instance.FireProjectile(fireProjectileInfo);
            }
        }

        /// <summary>
        /// Sets up the mortar projectile. Always invoke the method if one needs to borrow the mortar prefab.
        /// </summary>
        public static void SetupMortarProjectile()
        {
            if (mortarPrefab) return;
            GameObject paladinRocket = Resources.Load<GameObject>("prefabs/projectiles/PaladinRocket");
            mortarPrefab = paladinRocket.InstantiateClone("MortarProjectile");
            mortarPrefab.AddComponent<MortarGravity>();
            ProjectileCatalog.getAdditionalEntries += list => list.Add(mortarPrefab);
        }
    }

    internal class MortarGravity : MonoBehaviour
    {
        private ProjectileSimple projSimp;

        private void Awake()
        {
            projSimp = gameObject.GetComponent<ProjectileSimple>();
            if (!projSimp) return;
            projSimp.velocity *= MortarTube.instance.velocityMultiplier;
        }

        private void FixedUpdate()
        {
            if (!projSimp) return;
            projSimp.rigidbody.velocity -= new Vector3(0, MortarTube.instance.gravityAmount, 0);
        }
    }
}