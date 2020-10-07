using RoR2;
using RoR2.Projectile;
using System.Collections.ObjectModel;
using TILER2;
using UnityEngine;
using UnityEngine.Networking;
using static TILER2.MiscUtil;

namespace Chen.ClassicItems
{
    public class Missile2 : Item<Missile2>
    {
        public override string displayName => "AtG Missile Mk. 2";
        public override ItemTier itemTier => ItemTier.Tier3;
        public override ReadOnlyCollection<ItemTag> itemTags => new ReadOnlyCollection<ItemTag>(new[] { ItemTag.Damage });

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Base percent chance of triggering AtG Missile Mk. 2. Affected by proc coefficient.", AutoItemConfigFlags.None, 0f, 100f)]
        public float procChance { get; private set; } = 7f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Added to ProcChance per extra stack of AtG Missile Mk. 2.", AutoItemConfigFlags.None, 0f, 100f)]
        public float stackChance { get; private set; } = 7f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Maximum allowed ProcChance for AtG Missile Mk. 2.", AutoItemConfigFlags.None, 0f, 100f)]
        public float capChance { get; private set; } = 100f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Damage coefficient of each missile.", AutoItemConfigFlags.None, 0f, float.MaxValue)]
        public float dmgCoefficient { get; private set; } = 3f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Stack amount of Damage coefficient. Linear.", AutoItemConfigFlags.None, 0f, float.MaxValue)]
        public float dmgStack { get; private set; } = 0f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken | AutoUpdateEventFlags.InvalidatePickupToken)]
        [AutoItemConfig("Number of missiles per proc.", AutoItemConfigFlags.None, 1, int.MaxValue)]
        public int missileAmount { get; private set; } = 3;

        protected override string NewLangName(string langid = null) => displayName;

        protected override string NewLangPickup(string langid = null) => $"Chance to fire {missileAmount} missiles.";

        protected override string NewLangDesc(string langid = null)
        {
            string desc = $"<style=cIsDamage>{Pct(procChance, 0, 1)}</style>";
            if (stackChance > 0f) desc += $" <style=cStack>(+{Pct(stackChance, 0, 1)} per stack, up to {Pct(capChance, 0, 1)})</style>";
            desc += $" chance to fire <style=cIsDamage>{missileAmount}</style> missiles that deal <style=cIsDamage>{Pct(dmgCoefficient, 0)}</style>";
            if (dmgStack > 0f) desc += $" <style=cStack>(+{Pct(dmgStack, 0)} per stack)</style>";
            desc += " each. Affected by proc coefficient.";
            return desc;
        }

        protected override string NewLangLore(string langid = null) =>
            "\"I do not understand. They're all [REDACTED]. Whatever, use them.\"\n\n" +
            "\"You don't sound so convincing. The [REDACTED] is [REDACTED] [REDACTED]. I mean it. [REDACTED].\"\n\n" +
            "\"Alright, soldier, stop speaking the [REDACTED] language.\"\n\n" +
            "\"... but it IS [REDACTED].\"\n\n" +
            "\"I give up. To your positions.\"";

        public Missile2()
        {
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
                    ));
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