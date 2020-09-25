using EntityStates.Engi.EngiWeapon;
using EntityStates.Engi.Mine;
using RoR2;
using RoR2.Projectile;
using TILER2;
using UnityEngine;
using static TILER2.MiscUtil;

namespace Chen.ClassicItems
{
    public class InstantMinefield : Equipment<InstantMinefield>
    {
        public override string displayName => "Instant Minefield";

        public override float eqpCooldown { get; protected set; } = 45f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Number of mines to drop on use.", AutoItemConfigFlags.None, 0, int.MaxValue)]
        public int mineNumber { get; private set; } = 6;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Damage of each mine.", AutoItemConfigFlags.None, 0f, float.MaxValue)]
        public float mineDamage { get; private set; } = 4f;

        protected override string NewLangName(string langid = null) => displayName;

        protected override string NewLangPickup(string langid = null)
        {
            string desc = "Drop";
            if (mineNumber != 1) desc += " many mines";
            else desc += " a mine";
            desc += " on use.";
            return desc;
        }

        protected override string NewLangDesc(string langid = null)
        {
            string desc = $"Drop {mineNumber} mine";
            if (mineNumber != 1) desc += "s";
            desc += $", each dealing {Pct(mineDamage)} damage.";
            return desc;
        }

        protected override string NewLangLore(string langid = null) => "A relic of times long past (ChensClassicItems mod)";

        public InstantMinefield()
        {
        }

        protected override void LoadBehavior()
        {
            On.EntityStates.Engi.Mine.MineArmingWeak.FixedUpdate += On_ESMineArmingWeak;
            On.EntityStates.Engi.Mine.BaseMineArmingState.OnEnter += On_ESBaseMineArmingState;
        }

        protected override void UnloadBehavior()
        {
            On.EntityStates.Engi.Mine.MineArmingWeak.FixedUpdate -= On_ESMineArmingWeak;
            On.EntityStates.Engi.Mine.BaseMineArmingState.OnEnter -= On_ESBaseMineArmingState;
        }

        protected override bool OnEquipUseInner(EquipmentSlot slot)
        {
            CharacterBody body = slot.characterBody;
            if (!body) return false;

            GameObject gameObject = body.gameObject;
            Util.PlaySound(FireMines.throwMineSoundString, gameObject);
            DropMines(body, gameObject);
            //if (instance.CheckEmbryoProc(body)) DropMines(body, gameObject);

            return true;
        }

        private void DropMines(CharacterBody userBody, GameObject userGameObject)
        {
            Vector3 corePos = Util.GetCorePosition(userBody);
            GameObject minePrefab = ClassicItemsPlugin.instantMinePrefab;
            for (int n = 0; n < mineNumber; n++)
            {
                ProjectileManager.instance.FireProjectile(minePrefab, corePos, MineDropDirection(),
                                                          userGameObject, userBody.damage * mineDamage,
                                                          400f, Util.CheckRoll(userBody.crit, userBody.master),
                                                          DamageColorIndex.Item, null, -1f);
            }
        }

        private Quaternion MineDropDirection()
        {
            return Util.QuaternionSafeLookRotation(
                new Vector3(Random.Range(-1f, 1f),
                            -0.3f,
                            Random.Range(-1f, 1f))
            );
        }

        private void On_ESMineArmingWeak(On.EntityStates.Engi.Mine.MineArmingWeak.orig_FixedUpdate orig, MineArmingWeak self)
        {
            if (self.outer.name != "InstantMine(Clone)") orig(self);
            else self.outer.SetNextState(new MineArmingFull());
        }

        private void On_ESBaseMineArmingState(On.EntityStates.Engi.Mine.BaseMineArmingState.orig_OnEnter orig, BaseMineArmingState self)
        {
            orig(self);
            if (self.outer.name == "InstantMine(Clone)")
            {
                if (self.forceScale != 1f) self.forceScale = 1f;
                if (self.damageScale != 1f) self.damageScale = 1f;
            }
        }
    }
}