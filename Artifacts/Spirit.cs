using RoR2;
using TILER2;

public class Spirit : Artifact<Spirit>
{
    public override string displayName => "Artifact of Spirit";

    [AutoItemConfig("The percentage of which maximum movement speed can be multiplied according to health loss. 1 = 100%. " +
                    "Note that the number here is the movement speed multiplier that can be achieved when the body is dead.",
                    AutoItemConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
    public float maximumPossibleSpeedMultiplier { get; private set; } = 3f;

    protected override string NewLangName(string langid = null) => displayName;

    protected override string NewLangDesc(string langid = null) => "Characters run faster at lower health.";

    public Spirit()
    {
        iconPathName = "@ChensClassicItems:Assets/ClassicItems/icons/spirit_artifact_on_icon.png";
        iconPathNameDisabled = "@ChensClassicItems:Assets/ClassicItems/icons/spirit_artifact_off_icon.png";
    }

    protected override void LoadBehavior()
    {
        On.RoR2.CharacterBody.RecalculateStats += On_CBRecalcStats;
    }

    protected override void UnloadBehavior()
    {
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