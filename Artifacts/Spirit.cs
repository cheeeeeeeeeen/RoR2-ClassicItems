using RoR2;
using TILER2;
using UnityEngine;

namespace Chen.ClassicItems
{
    /// <summary>
    /// Singleton artifact class powered by TILER2 that implements Artifact of Spirit functionality.
    /// </summary>
    public class Spirit : Artifact_V2<Spirit>
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public override string displayName => "Artifact of Spirit";

        [AutoConfig("The percentage of which maximum movement speed can be multiplied according to health loss. 1 = 100%. " +
                    "Note that the number here is the movement speed multiplier that can be achieved when the body is dead.",
                    AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float maximumPossibleSpeedMultiplier { get; private set; } = 3f;

        [AutoConfig("Determines the icon style. Any integer = Kirbsuke's, 1 = Aromatic Sunday's. Client only.", AutoConfigFlags.None, 0, int.MaxValue)]
        public int iconStyle { get; private set; } = 0;

        protected override string GetNameString(string langid = null) => displayName;

        protected override string GetDescString(string langid = null) => "Characters run faster at lower health.";

        public override void SetupConfig()
        {
            base.SetupConfig();
            switch (iconStyle)
            {
                case 1:
                    iconResourcePath = "@ChensClassicItems:Assets/ClassicItems/icons/alt_spirit_artifact_on_icon.png";
                    iconResourcePathDisabled = "@ChensClassicItems:Assets/ClassicItems/icons/alt_spirit_artifact_off_icon.png";
                    break;

                default:
                    iconResourcePath = "@ChensClassicItems:Assets/ClassicItems/icons/spirit_artifact_on_icon.png";
                    iconResourcePathDisabled = "@ChensClassicItems:Assets/ClassicItems/icons/spirit_artifact_off_icon.png";
                    break;
            }
        }

        public override void Install()
        {
            base.Install();
            On.RoR2.CharacterBody.RecalculateStats += On_CBRecalcStats;
            CharacterBody.onBodyStartGlobal += CharacterBody_onBodyStartGlobal;
        }

        public override void Uninstall()
        {
            base.Uninstall();
            On.RoR2.CharacterBody.RecalculateStats -= On_CBRecalcStats;
            CharacterBody.onBodyStartGlobal -= CharacterBody_onBodyStartGlobal;
        }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        private void On_CBRecalcStats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            if (!IsActiveAndEnabled()) return;
            HealthComponent hc = self.healthComponent;
            if (!hc || !hc.alive || hc.fullHealth <= 0 || hc.health <= 0 || self.moveSpeed < 0 || hc.health > hc.fullHealth) return;
            self.moveSpeed += self.moveSpeed * maximumPossibleSpeedMultiplier * (1 - (hc.health / hc.fullHealth));
            self.acceleration = self.moveSpeed * (self.baseAcceleration / self.baseMoveSpeed);
        }

        private void CharacterBody_onBodyStartGlobal(CharacterBody obj)
        {
            if (!IsActiveAndEnabled() || obj.isPlayerControlled) return;
            obj.gameObject.AddComponent<SpiritBehavior>();
        }
    }

    internal class SpiritBehavior : MonoBehaviour
    {
        private float previousHealth;
        private CharacterBody body;
        private HealthComponent healthComponent;

        private readonly float threshold = 1f;

        private void Awake()
        {
            body = gameObject.GetComponent<CharacterBody>();
            healthComponent = gameObject.GetComponent<HealthComponent>();
            previousHealth = healthComponent.health;
        }

        private void FixedUpdate()
        {
            if (IsWithinThreshold()) return;
            previousHealth = healthComponent.health;
            body.RecalculateStats();
        }

        private bool IsWithinThreshold()
        {
            return healthComponent.health <= (previousHealth + threshold) &&
                   healthComponent.health >= (previousHealth - threshold);
        }
    }
}