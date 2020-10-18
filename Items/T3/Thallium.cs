using R2API;
using RoR2;
using System.Collections.ObjectModel;
using TILER2;
using UnityEngine;
using UnityEngine.Networking;
using static TILER2.MiscUtil;
using static TILER2.StatHooks;

namespace Chen.ClassicItems
{
    public class Thallium : Item<Thallium>
    {
        public override string displayName => "Thallium";
        public override ItemTier itemTier => ItemTier.Tier3;
        public override ReadOnlyCollection<ItemTag> itemTags => new ReadOnlyCollection<ItemTag>(new[] { ItemTag.Damage });

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Base percent chance of triggering Thallium poisoning. Affected by proc coefficient.", AutoItemConfigFlags.None, 0f, 100f)]
        public float procChance { get; private set; } = 10f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Added to ProcChance per extra stack of Thallium. Linear.", AutoItemConfigFlags.None, 0f, 100f)]
        public float stackChance { get; private set; } = 0f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Maximum allowed ProcChance for Thallium.", AutoItemConfigFlags.None, 0f, 100f)]
        public float capChance { get; private set; } = 10f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Damage coefficient of the poison per second. Based on the victim's damage. Automatically computed per tick.", AutoItemConfigFlags.None, 0f, float.MaxValue)]
        public float dmgCoefficient { get; private set; } = 1.25f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Stack amount of Damage coefficient. Linear.", AutoItemConfigFlags.None, 0f, float.MaxValue)]
        public float dmgStack { get; private set; } = 0f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Slow multiplier applied by Thallium. Only applied on the base movement speed.", AutoItemConfigFlags.None, 0f, 1f)]
        public float slowMultiplier { get; private set; } = .9f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Duration of the Thallium debuff.", AutoItemConfigFlags.None, 0, int.MaxValue)]
        public int duration { get; private set; } = 4;

        protected override string NewLangName(string langid = null) => displayName;

        protected override string NewLangPickup(string langid = null) => $"Chance to slow and damage enemies over time.";

        protected override string NewLangDesc(string langid = null)
        {
            string desc = $"<style=cIsDamage>{Pct(procChance, 0, 1)}</style>";
            if (stackChance > 0f) desc += $" <style=cStack>(+{Pct(stackChance, 0, 1)} per stack, up to {Pct(capChance, 0, 1)})</style>";
            desc += $" chance to apply Thallium poisoning that deals <style=cIsDamage>{Pct(dmgCoefficient * duration, 0)}</style>";
            if (dmgStack > 0f) desc += $" <style=cStack>(+{Pct(dmgStack * duration, 0)} per stack)</style>";
            desc += $" damage based on the victim's damage over <style=cIsDamage>{duration} seconds</style>. Victims' base movement speed are also reduced by" +
                    $" <style=cIsDamage>{Pct(slowMultiplier, 0)}</style>. Affected by proc coefficient." +
                    $" <style=cDeath>The poison cannot be reapplied when affected, and is not stackable.</style>";
            return desc;
        }

        protected override string NewLangLore(string langid = null) =>
            "\"She shouldn't notice,\" it says.\n\nWell, that was dark. Few words, but contains heavy intent.\n\nWe will now use it for our survival instead.";

        private static BuffIndex poisonBuff;
        private static DotController.DotIndex poisonDot;

        public Thallium()
        {
            onBehav += () =>
            {
                CustomBuff thalliumBuffDef = new CustomBuff(new BuffDef
                {
                    //buffColor = new Color32(66, 28, 82, 255),
                    canStack = false,
                    isDebuff = true,
                    name = "CCIThalliumPoison",
                    iconPath = "@ChensClassicItems:Assets/ClassicItems/Icons/thallium_buff_icon.png"
                });
                poisonBuff = BuffAPI.Add(thalliumBuffDef);

                DotController.DotDef thalliumDotDef = new DotController.DotDef
                {
                    interval = .5f,
                    damageCoefficient = 1,
                    damageColorIndex = DamageColorIndex.DeathMark,
                    associatedBuff = poisonBuff
                };
                poisonDot = DotAPI.RegisterDotDef(thalliumDotDef, (dotController, dotStack) =>
                {
                    CharacterBody attackerBody = dotStack.attackerObject.GetComponent<CharacterBody>();
                    if (attackerBody)
                    {
                        float damageMultiplier = dmgCoefficient + dmgStack * (GetCount(attackerBody) - 1);
                        float poisonDamage = 0f;
                        if (dotController.victimBody) poisonDamage += dotController.victimBody.damage;
                        dotStack.damage = poisonDamage * damageMultiplier;
                    }
                });

                if (Compat_ItemStats.enabled)
                {
                    Compat_ItemStats.CreateItemStatDef(regItem.ItemDef,
                    (
                        (count, inv, master) => { return Mathf.Min(procChance + stackChance * (count - 1), capChance); },
                        (value, inv, master) => { return $"Poison Chance: {Pct(value, 0, 1)}"; }
                    ),
                    (
                        (count, inv, master) => { return dmgCoefficient + (count - 1) * dmgStack; },
                        (value, inv, master) => { return $"Victim damage per second: {Pct(value, 0)}"; }
                    ),
                    (
                        (count, inv, master) => { return duration; },
                        (value, inv, master) => { return $"Poison Duration: {value}"; }
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
            GetStatCoefficients += Thallium_GetStatCoefficients;
        }

        protected override void UnloadBehavior()
        {
            On.RoR2.GlobalEventManager.OnHitEnemy -= On_GEMOnHitEnemy;
            GetStatCoefficients -= Thallium_GetStatCoefficients;
        }

        private void On_GEMOnHitEnemy(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            orig(self, damageInfo, victim);

            if (!NetworkServer.active || !victim || !damageInfo.attacker || damageInfo.procCoefficient <= 0f) return;

            var vicb = victim.GetComponent<CharacterBody>();

            CharacterBody body = damageInfo.attacker.GetComponent<CharacterBody>();
            if (!body || !vicb || !vicb.healthComponent || !vicb.mainHurtBox || vicb.HasBuff(poisonBuff)) return;

            CharacterMaster chrm = body.master;
            if (!chrm) return;

            int icnt = GetCount(body);
            if (icnt == 0) return;

            icnt--;
            float m2Proc = procChance;
            if (icnt > 0) m2Proc += stackChance * icnt;
            if (m2Proc > capChance) m2Proc = capChance;
            if (!Util.CheckRoll(m2Proc * damageInfo.procCoefficient, chrm)) return;

            DotController.InflictDot(victim, damageInfo.attacker, poisonDot, duration);
        }

        private void Thallium_GetStatCoefficients(CharacterBody sender, StatHookEventArgs args)
        {
            if (sender.HasBuff(poisonBuff))
            {
                args.moveSpeedMultAdd -= slowMultiplier;
            }
        }
    }
}