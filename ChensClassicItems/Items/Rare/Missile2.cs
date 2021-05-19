using RoR2;
using RoR2.Projectile;
using System.Collections.ObjectModel;
using TILER2;
using UnityEngine;
using UnityEngine.Networking;
using static TILER2.MiscUtil;

namespace Chen.ClassicItems.Items.Rare
{
    /// <summary>
    /// Singleton item class powered by TILER2 that implements AtG Missile Mk. II functionality.
    /// </summary>
    public class Missile2 : Item<Missile2>
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override string displayName => "AtG Missile Mk. 2";
        public override ItemTier itemTier => ItemTier.Tier3;
        public override ReadOnlyCollection<ItemTag> itemTags => new ReadOnlyCollection<ItemTag>(new[] { ItemTag.Damage });

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Base percent chance of triggering AtG Missile Mk. 2. Affected by proc coefficient.", AutoConfigFlags.None, 0f, 100f)]
        public float procChance { get; private set; } = 7f;

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Added to ProcChance per extra stack of AtG Missile Mk. 2.", AutoConfigFlags.None, 0f, 100f)]
        public float stackChance { get; private set; } = 7f;

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Maximum allowed ProcChance for AtG Missile Mk. 2.", AutoConfigFlags.None, 0f, 100f)]
        public float capChance { get; private set; } = 100f;

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Damage coefficient of each missile.", AutoConfigFlags.None, 0f, float.MaxValue)]
        public float dmgCoefficient { get; private set; } = 3f;

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Stack amount of Damage coefficient. Linear.", AutoConfigFlags.None, 0f, float.MaxValue)]
        public float dmgStack { get; private set; } = 0f;

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Number of missiles per proc.", AutoConfigFlags.None, 1, int.MaxValue)]
        public int missileAmount { get; private set; } = 3;

        protected override string GetNameString(string langid = null) => displayName;

        protected override string GetPickupString(string langid = null) => $"Chance to fire {missileAmount} missiles.";

        protected override string GetDescString(string langid = null)
        {
            string desc = $"<style=cIsDamage>{Pct(procChance, 0, 1)}</style>";
            if (stackChance > 0f) desc += $" <style=cStack>(+{Pct(stackChance, 0, 1)} per stack, up to {Pct(capChance, 0, 1)})</style>";
            desc += $" chance to fire <style=cIsDamage>{missileAmount}</style> missiles that deal <style=cIsDamage>{Pct(dmgCoefficient, 0)}</style>";
            if (dmgStack > 0f) desc += $" <style=cStack>(+{Pct(dmgStack, 0)} per stack)</style>";
            desc += " each. Affected by proc coefficient.";
            return desc;
        }

        protected override string GetLoreString(string langid = null) =>
            "\"I do not understand. They're all [REDACTED]. Whatever, use them.\"\n\n" +
            "\"You don't sound so convincing. The [REDACTED] is [REDACTED] [REDACTED]. I mean it. [REDACTED].\"\n\n" +
            "\"Alright, soldier, stop speaking the [REDACTED] language.\"\n\n" +
            "\"... but it IS [REDACTED].\"\n\n" +
            "\"I give up. To your positions.\"";

        public override void SetupBehavior()
        {
            base.SetupBehavior();
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
                ));
            }
            if (Compat_BetterUI.enabled)
            {
                Compat_BetterUI.AddEffect(itemDef, procChance, stackChance, Compat_BetterUI.ChanceFormatter, Compat_BetterUI.LinearStacking,
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

            for (int t = 0; t < missileAmount; t++)
            {
                ProcMissile(t, body, damageInfo.procChainMask, victim, damageInfo, icnt);
            }
        }

        private void ProcMissile(int mNum, CharacterBody attackerBody, ProcChainMask procChainMask, GameObject victim, DamageInfo damageInfo, int stack)
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
                projectilePrefab = GlobalEventManager.instance.missilePrefab,
                position = position,
                rotation = Util.QuaternionSafeLookRotation(DetermineFacing(mNum)),
                procChainMask = procChainMask2,
                target = victim,
                owner = gameObject,
                damage = damage,
                crit = damageInfo.crit,
                force = 200f,
                damageColorIndex = DamageColorIndex.Item
            };
            ProjectileManager.instance.FireProjectile(fireProjectileInfo);
        }

        private Vector3 DetermineFacing(int missileNumber)
        {
            if (missileNumber % 2 == 0) return new Vector3(Random.Range(-.5f, .5f), Random.Range(1.5f, .5f), 0);
            else return (Vector3.up);
        }
    }
}