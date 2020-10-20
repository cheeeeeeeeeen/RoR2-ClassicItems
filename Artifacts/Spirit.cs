using RoR2;
using TILER2;

namespace Chen.ClassicItems
{
    public class Spirit : Artifact_V2<Spirit>
    {
        public override string displayName => "Artifact of Spirit";

        [AutoConfig("The percentage of which maximum movement speed can be multiplied according to health loss. 1 = 100%. " +
                    "Note that the number here is the movement speed multiplier that can be achieved when the body is dead.",
                    AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float maximumPossibleSpeedMultiplier { get; private set; } = 3f;

        protected override string GetNameString(string langid = null) => displayName;

        protected override string GetDescString(string langid = null) => "Characters run faster at lower health.";

        public Spirit()
        {
            iconResourcePath = "@ChensClassicItems:Assets/ClassicItems/icons/spirit_artifact_on_icon.png";
            iconResourcePathDisabled = "@ChensClassicItems:Assets/ClassicItems/icons/spirit_artifact_off_icon.png";
        }

        public override void Install()
        {
            base.Install();
            On.RoR2.CharacterBody.RecalculateStats += On_CBRecalcStats;
        }

        public override void Uninstall()
        {
            base.Uninstall();
            On.RoR2.CharacterBody.RecalculateStats -= On_CBRecalcStats;
        }

        private void On_CBRecalcStats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            if (!IsActiveAndEnabled()) return;
            HealthComponent hc = self.healthComponent;
            if (!hc || !hc.alive || hc.fullHealth <= 0 || hc.health <= 0 || self.moveSpeed < 0 || hc.health > hc.fullHealth) return;
            self.moveSpeed += self.moveSpeed * maximumPossibleSpeedMultiplier * (1 - (hc.health / hc.fullHealth));
            self.acceleration += self.acceleration * maximumPossibleSpeedMultiplier * (1 - (hc.health / hc.fullHealth));
        }
    }
}