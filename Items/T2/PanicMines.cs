using EntityStates.Engi.EngiWeapon;
using EntityStates.Engi.Mine;
using RoR2;
using RoR2.Projectile;
using System.Collections.ObjectModel;
using TILER2;
using UnityEngine;
using static TILER2.MiscUtil;

namespace Chen.ClassicItems
{
    public class PanicMines : Item<PanicMines>
    {
        public override string displayName => "Panic Mines";
        public override ItemTier itemTier => ItemTier.Tier2;
        public override ReadOnlyCollection<ItemTag> itemTags => new ReadOnlyCollection<ItemTag>(new[] { ItemTag.Damage });

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Fraction of max health required as damage taken to drop a mine.", AutoItemConfigFlags.None, 0f, 1f)]
        public float healthThreshold { get; private set; } = 0.2f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Base AoE damage coefficient of the panic mine.", AutoItemConfigFlags.None, 0f, float.MaxValue)]
        public float baseDmg { get; private set; } = 5f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Stack increase of the AoE damage coefficient.", AutoItemConfigFlags.None, 0f, float.MaxValue)]
        public float stackDmg { get; private set; } = 0f;

        [AutoItemConfig("If true, damage to shield and barrier (from e.g. Personal Shield Generator, Topaz Brooch) will not count towards triggering Panic Mines.")]
        public bool requireHealth { get; private set; } = true;

        protected override string NewLangName(string langid = null) => displayName;

        protected override string NewLangPickup(string langid = null) => "Drop mines when taking heavy damage.";

        protected override string NewLangDesc(string langid = null)
        {
            string desc = "<style=cDeath>When hit";
            if (healthThreshold > 0f) desc += $" for more than {Pct(healthThreshold)} of max health</style>";
            desc += $", drop <style=cIsDamage>1</style> mine <style=cStack>(+1 per stack)</style> with <style=cIsDamage>{Pct(baseDmg)}</style>";
            if (stackDmg > 0f) desc += " <style=cStack>(+" + Pct(stackDmg) + " per stack)</style>";
            desc += " damage.";
            return desc;
        }

        protected override string NewLangLore(string langid = null) =>
            "\"Must be strapped onto vehicles, NOT personnel! After taking heavy fire, the automatic dispenser should drop down and arm a mine to make a hasty retreat (Or blow enemies sky-high who are dumb enough to follow.)" +
            " Includes smart-fire, but leave the blast radius regardless. The laws of physics don't pick sides. Very high yield for how small it is." +
            " If you want to use it offensively, then... well, just get shot. A lot. Preferably by small arms fire, or you'll die trying to have the mines drop.\"\n\n" +
            "\"Seriously?\" I said to myself upon reading the details that is attached to what it looked like an odd proximity mine.\n\n" +
            "But if there is one thing about surviving in this damnable place, then I should trust that this equipment will prove itself useful.";

        public PanicMines()
        {
            onBehav += () =>
            {
                if (Compat_ItemStats.enabled)
                {
                    Compat_ItemStats.CreateItemStatDef(regItem.ItemDef,
                        ((count, inv, master) =>
                        {
                            return baseDmg + (count - 1) * stackDmg;
                        },
                        (value, inv, master) => { return $"Mine Damage: {Pct(value, 1)}"; }
                    ));
                }
            };
        }

        protected override void LoadBehavior()
        {
            On.RoR2.HealthComponent.TakeDamage += On_HCTakeDamage;
            On.EntityStates.Engi.Mine.MineArmingWeak.FixedUpdate += On_ESMineArmingWeak;
        }

        protected override void UnloadBehavior()
        {
            On.RoR2.HealthComponent.TakeDamage -= On_HCTakeDamage;
            On.EntityStates.Engi.Mine.MineArmingWeak.FixedUpdate -= On_ESMineArmingWeak;
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
            GameObject minePrefab = ClassicItemsPlugin.panicMinePrefab;

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