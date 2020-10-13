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
    public class MortarTube : Item<MortarTube>
    {
        public override string displayName => "Mortar Tube";
        public override ItemTier itemTier => ItemTier.Tier1;
        public override ReadOnlyCollection<ItemTag> itemTags => new ReadOnlyCollection<ItemTag>(new[] { ItemTag.Damage });

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Base percent chance of launch a mortar. Affected by proc coefficient.", AutoItemConfigFlags.None, 0f, 100f)]
        public float procChance { get; private set; } = 9f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Added to ProcChance per extra stack of Mortar Tube.", AutoItemConfigFlags.None, 0f, 100f)]
        public float stackChance { get; private set; } = 0f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Maximum allowed ProcChance for Mortar Tube.", AutoItemConfigFlags.None, 0f, 100f)]
        public float capChance { get; private set; } = 100f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Damage coefficient of each missile.", AutoItemConfigFlags.None, 0f, float.MaxValue)]
        public float dmgCoefficient { get; private set; } = 1.7f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Stack amount of Damage coefficient. Linear.", AutoItemConfigFlags.None, 0f, float.MaxValue)]
        public float dmgStack { get; private set; } = 1.7f;

        [AutoItemConfig("Velocity multiplier for the mortar. Lower value means it moves slower.", AutoItemConfigFlags.None, 0f, float.MaxValue)]
        public float velocityMultiplier { get; private set; } = .5f;

        [AutoItemConfig("How heavy the mortar is. Higher means it is heavier. This is not a percentage nor a multiplier.", AutoItemConfigFlags.None, 0f, float.MaxValue)]
        public float gravityAmount { get; private set; } = .5f;

        [AutoItemConfig("Setting to true would launch the mortar at a fixed angle regardless of aim. Setting to false would launch the mortar relative to aim.")]
        public bool fixedAim { get; private set; } = false;

        [AutoItemConfig("The angle from where the mortar is launched. 1 means completely up. -1 means completely down.", AutoItemConfigFlags.None, float.MinValue, float.MaxValue)]
        public float launchAngle { get; private set; } = .9f;

        [AutoItemConfig("Inaccuracy of the mortar. Higher value means it's more inaccurate.", AutoItemConfigFlags.None, 0f, float.MaxValue)]
        public float inaccuracyRate { get; private set; } = .05f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Amount of mortars launched per stack. It can be set to 0.5 to fire another mortar for every 2nd Mortar Tube gained (excluding the first).",
                        AutoItemConfigFlags.None, 0f, float.MaxValue)]
        public float stackAmount { get; private set; } = 0f;

        protected override string NewLangName(string langid = null) => displayName;

        protected override string NewLangPickup(string langid = null) => $"Chance to launch a mortar.";

        protected override string NewLangDesc(string langid = null)
        {
            string desc = $"<style=cIsDamage>{Pct(procChance, 0, 1)}</style>";
            if (stackChance > 0f) desc += $" <style=cStack>(+{Pct(stackChance, 0, 1)} per stack, up to {Pct(capChance, 0, 1)})</style>";
            desc += $" chance to launch a mortar that deals <style=cIsDamage>{Pct(dmgCoefficient, 0)}</style>";
            if (dmgStack > 0f) desc += $" <style=cStack>(+{Pct(dmgStack, 0)} per stack)</style>";
            desc += ". Affected by proc coefficient. The mortar deals an AoE damage.";
            if (stackAmount > 0) desc += " More mortars may be launched upon stacking.";
            return desc;
        }

        protected override string NewLangLore(string langid = null) =>
            "\"A very primitive weapon, all manual labor. Put the explosive down the end, then fire.\"\n\n" +
            "\"That sounds highly dangerous. I would not recommend it. There are far more advanced weapons that we can use.\"\n\n" +
            "\"What a waste. It could have been good for artillery support.\"\n\n" +
            "\"We don't exactly need artillery support in this case. We only need to survive and complete our mission.\"\n\n" +
            "\"If you say so... You can still hit your enemies with it, at least.\"\n\n" +
            "\"I'll take it.\"\n\n" +
            "A Mortar Tube, huh? I never knew they actually existed. Is it really a relic of history now? I wonder if it really works. It did look simple... and old.";

        private static GameObject mortarPrefab;

        public MortarTube()
        {
            GameObject paladinRocket = Resources.Load<GameObject>("prefabs/projectiles/PaladinRocket");
            mortarPrefab = paladinRocket.InstantiateClone("MortarProjectile");
            mortarPrefab.AddComponent<MortarGravity>();

            onBehav += () =>
            {
                if (Compat_ItemStats.enabled)
                {
                    Compat_ItemStats.CreateItemStatDef(regItem.ItemDef,
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
                    Compat_BetterUI.AddEffect(regIndex, procChance, stackChance, Compat_BetterUI.ChanceFormatter, Compat_BetterUI.LinearStacking,
                        (value, extraStackValue, procCoefficient) =>
                        {
                            return Mathf.CeilToInt((capChance - value * procCoefficient) / (extraStackValue * procCoefficient)) + 1;
                        });
                }
            };
        }

        protected override void LoadBehavior()
        {
            On.RoR2.GlobalEventManager.OnHitEnemy += On_GEMOnHitEnemy;
        }

        protected override void UnloadBehavior()
        {
            On.RoR2.GlobalEventManager.OnHitEnemy -= On_GEMOnHitEnemy;
        }

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
    }

    public class MortarGravity : MonoBehaviour
    {
        private ProjectileSimple projSimp;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by UnityEngine")]
        private void Awake()
        {
            projSimp = gameObject.GetComponent<ProjectileSimple>();
            if (!projSimp) return;
            projSimp.velocity *= MortarTube.instance.velocityMultiplier;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by UnityEngine")]
        private void FixedUpdate()
        {
            if (!projSimp) return;
            projSimp.rigidbody.velocity -= new Vector3(0, MortarTube.instance.gravityAmount, 0);
        }
    }
}