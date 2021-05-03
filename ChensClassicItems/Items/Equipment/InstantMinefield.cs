using EntityStates.Engi.EngiWeapon;
using EntityStates.Engi.Mine;
using R2API;
using RoR2;
using RoR2.Projectile;
using ThinkInvisible.ClassicItems;
using TILER2;
using UnityEngine;
using static TILER2.MiscUtil;

namespace Chen.ClassicItems.Items.Equipment
{
    /// <summary>
    /// Singleton equipment class powered by TILER2 that implements Instant Minefield functionality.
    /// </summary>
    public class InstantMinefield : Equipment<InstantMinefield>
    {
        /// <summary>
        /// The mine prefab used to deploy the mines triggered by Instant Minefield.
        /// </summary>
        public static GameObject minePrefab { get; private set; }

        /// <summary>
        /// The ghost projectile prefab for the mine prefab of Instant Minefield.
        /// </summary>
        public static GameObject mineGhostPrefab { get; private set; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override string displayName => "Instant Minefield";

        public override float cooldown { get; protected set; } = 45f;

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Number of mines to drop on use.", AutoConfigFlags.None, 0, int.MaxValue)]
        public int mineNumber { get; private set; } = 6;

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Damage of each mine.", AutoConfigFlags.None, 0f, float.MaxValue)]
        public float mineDamage { get; private set; } = 4f;

        protected override string GetNameString(string langid = null) => displayName;

        protected override string GetPickupString(string langid = null)
        {
            string desc = "Drop";
            if (mineNumber != 1) desc += " many mines";
            else desc += " a mine";
            desc += " on use.";
            return desc;
        }

        protected override string GetDescString(string langid = null)
        {
            string desc = $"Drop <style=cIsDamage>{mineNumber}</style> mine";
            if (mineNumber != 1) desc += "s";
            desc += $", each dealing <style=cIsDamage>{Pct(mineDamage)}</style> damage.";
            return desc;
        }

        protected override string GetLoreString(string langid = null) =>
            "\"Turn the safe switch off, and just simply lay it down... and its uh... smart-fire should prevent it from blowing your own legs off.\"\n\n" +
            "\"No [REDACTED]? I don't feel safe already, and that's just Step 1!\"\n\n" +
            "\"That's what it says here. Hurry up because we don't have long. Those giant insects will be coming anytime soon.\"\n\n" +
            "\"Here goes nothing, then. Turning off the safe. Laying it down now. Let's run when I place it just to be safe.\"\n\n" +
            "\"Go, go, go! Move it!\"\n\n" +
            "\"Seems to be safe as advertised. Look at it go. It deployed mines almost instantly.\"\n\n" +
            "\"Could have used a better name, though. Instant Minefield doesn't exactly sound legit.\"\n\n" +
            "\"End of log.\"";

        public override void SetupBehavior()
        {
            base.SetupBehavior();
            GameObject engiMinePrefab = Resources.Load<GameObject>("prefabs/projectiles/EngiMine");
            minePrefab = engiMinePrefab.InstantiateClone("InstantMine");
            Object.Destroy(minePrefab.GetComponent<ProjectileDeployToOwner>());

            GameObject engiMineGhostPrefab = Resources.Load<GameObject>("prefabs/projectileghosts/EngiMineGhost");
            mineGhostPrefab = engiMineGhostPrefab.InstantiateClone("InstantMineGhost", false);
            SkinnedMeshRenderer mesh = mineGhostPrefab.GetComponentInChildren<SkinnedMeshRenderer>();
            mesh.material.color = new Color32(111, 95, 52, 255);
            minePrefab.GetComponent<ProjectileController>().ghostPrefab = mineGhostPrefab;

            ProjectileAPI.Add(minePrefab);

            //Embryo_V2.instance.Compat_Register(catalogIndex);
        }

        public override void Install()
        {
            base.Install();
            On.EntityStates.Engi.Mine.MineArmingWeak.FixedUpdate += On_ESMineArmingWeak;
            On.EntityStates.Engi.Mine.BaseMineArmingState.OnEnter += On_ESBaseMineArmingState;
        }

        public override void Uninstall()
        {
            base.Uninstall();
            On.EntityStates.Engi.Mine.MineArmingWeak.FixedUpdate -= On_ESMineArmingWeak;
            On.EntityStates.Engi.Mine.BaseMineArmingState.OnEnter -= On_ESBaseMineArmingState;
        }

        protected override bool PerformEquipmentAction(EquipmentSlot slot)
        {
            CharacterBody body = slot.characterBody;
            if (!body) return false;

            GameObject gameObject = body.gameObject;
            Util.PlaySound(FireMines.throwMineSoundString, gameObject);
            DropMines(body, gameObject);
            //if (instance.CheckEmbryoProc(body)) DropMines(body, gameObject, .6f);

            return true;
        }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        private void DropMines(CharacterBody userBody, GameObject userGameObject, float yMult = 1f)
        {
            Vector3 corePos = Util.GetCorePosition(userBody);
            for (int n = 0; n < mineNumber; n++)
            {
                ProjectileManager.instance.FireProjectile(minePrefab, corePos, MineDropDirection(yMult),
                                                          userGameObject, userBody.damage * mineDamage,
                                                          400f, Util.CheckRoll(userBody.crit, userBody.master),
                                                          DamageColorIndex.Item, null, -1f);
            }
        }

        private Quaternion MineDropDirection(float yMultiplier)
        {
            return Util.QuaternionSafeLookRotation(
                new Vector3(Random.Range(-1f, 1f),
                            -0.4f * yMultiplier,
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