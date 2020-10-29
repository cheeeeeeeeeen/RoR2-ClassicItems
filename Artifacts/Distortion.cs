using EntityStates;
using R2API;
using RoR2;
using RoR2.Skills;
using TILER2;
using UnityEngine;

namespace Chen.ClassicItems
{
    public class Distortion : Artifact_V2<Distortion>
    {
        public override string displayName => "Artifact of Distortion";

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("The time when skill lockdown shifts in seconds.", AutoConfigFlags.PreventNetMismatch, 0, int.MaxValue)]
        public int intervalBetweenLocks { get; private set; } = 60;

        protected override string GetNameString(string langid = null) => displayName;

        protected override string GetDescString(string langid = null) => $"Lock a random active or passive skill every {intervalBetweenLocks} seconds.";

        public static SkillDef distortSkill { get; private set; }
        public static Xoroshiro128Plus distortionRng { get; private set; } = new Xoroshiro128Plus(0UL);

        public Distortion()
        {
            iconResourcePath = "@ChensClassicItems:Assets/ClassicItems/icons/distortion_artifact_on_icon.png";
            iconResourcePathDisabled = "@ChensClassicItems:Assets/ClassicItems/Icons/distortion_artifact_off_icon.png";
        }

        public override void SetupBehavior()
        {
            LanguageAPI.Add("ALL_DISTORTION_LOCKED_NAME", "Distorted");
            LanguageAPI.Add("ALL_DISTORTION_LOCKED_DESCRIPTION", "You forgot how to perform this skill.");

            distortSkill = ScriptableObject.CreateInstance<SkillDef>();
            distortSkill.activationState = new SerializableEntityStateType(typeof(BaseState));
            distortSkill.activationStateMachineName = "Weapon";
            distortSkill.baseMaxStock = 0;
            distortSkill.baseRechargeInterval = 0f;
            distortSkill.canceledFromSprinting = false;
            distortSkill.fullRestockOnAssign = false;
            distortSkill.interruptPriority = InterruptPriority.Any;
            distortSkill.isBullets = true;
            distortSkill.isCombatSkill = false;
            distortSkill.mustKeyPress = false;
            distortSkill.noSprint = false;
            distortSkill.rechargeStock = 0;
            distortSkill.requiredStock = 0;
            distortSkill.stockToConsume = 0;
            distortSkill.icon = Resources.Load<Sprite>("@ChensClassicItems:Assets/ClassicItems/icons/distortion_skill_icon.png");
            distortSkill.skillDescriptionToken = "ALL_DISTORTION_LOCKED_DESCRIPTION";
            distortSkill.skillName = "ALL_DISTORTION_LOCKED_NAME";
            distortSkill.skillNameToken = "ALL_DISTORTION_LOCKED_NAME";
            LoadoutAPI.AddSkillDef(distortSkill);
        }

        public override void Install()
        {
            base.Install();
            CharacterBody.onBodyStartGlobal += CharacterBody_onBodyStartGlobal;
            CameraRigController.onCameraTargetChanged += CameraRigController_onCameraTargetChanged;
        }

        public override void Uninstall()
        {
            base.Uninstall();
            CharacterBody.onBodyStartGlobal -= CharacterBody_onBodyStartGlobal;
            CameraRigController.onCameraTargetChanged -= CameraRigController_onCameraTargetChanged;
        }

        private void CharacterBody_onBodyStartGlobal(CharacterBody obj)
        {
            if (IsActiveAndEnabled() && obj.isPlayerControlled)
            {
                DistortionManager.GetOrAddComponent(obj);
            }
        }

        private void CameraRigController_onCameraTargetChanged(CameraRigController arg1, GameObject arg2)
        {
            CharacterBody body = arg2.GetComponent<CharacterBody>();
            if (arg1.viewer != Util.LookUpBodyNetworkUser(body))
            {
                DistortionManager manager = body.GetComponent<DistortionManager>();
                if (manager) manager.RemoveOnSpectate();
            }
        }
    }

    public class DistortionManager : MonoBehaviour
    {
        public GenericSkill[] genericSkills;
        private bool init = true;
        private CharacterBody body;
        private float timer = -1;
        private int lockedSkillIndex = -1;
        public SkillDef oldSkillDef;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by UnityEngine")]
        private void FixedUpdate()
        {
            if (init)
            {
                if (AssignAndCheckBody())
                {
                    genericSkills = body.GetComponentsInChildren<GenericSkill>();
                    init = false;
                }
            }
            else
            {
                if (timer < 0)
                {
                    timer = 0f;
                    LockRandomSkill();
                }
                if (timer > 60 * Distortion.instance.intervalBetweenLocks)
                {
                    timer = 0f;
                    UnlockSkill();
                    LockRandomSkill();
                }
                else timer += Time.fixedDeltaTime;
                if (oldSkillDef.skillNameToken == genericSkills[lockedSkillIndex].skillDef.skillNameToken)
                {
                    LockSkill();
                }
            }
        }

        private bool AssignAndCheckBody()
        {
            body = gameObject.GetComponent<CharacterBody>();
            if (!body)
            {
                ClassicItemsPlugin._logger.LogWarning("DistortionManager.FixedUpdate: Body is not found.");
                return false;
            }
            return true;
        }

        private int LockRandomSkill()
        {
            if (genericSkills.Length > 1)
            {
                lockedSkillIndex = Distortion.distortionRng.RangeInt(0, genericSkills.Length);
                oldSkillDef = genericSkills[lockedSkillIndex].skillDef;
                LockSkill();
                return lockedSkillIndex;
            }
            return -1;
        }

        private void LockSkill()
        {
            genericSkills[lockedSkillIndex].AssignSkill(Distortion.distortSkill);
            genericSkills[lockedSkillIndex].stock = 0;
        }

        private bool UnlockSkill()
        {
            if (lockedSkillIndex >= 0)
            {
                genericSkills[lockedSkillIndex].AssignSkill(oldSkillDef);
                genericSkills[lockedSkillIndex].RecalculateMaxStock();
                return true;
            }
            return false;
        }

        public void RemoveOnSpectate()
        {
            UnlockSkill();
            Destroy(this);
        }

        public static DistortionManager GetOrAddComponent(CharacterBody body)
        {
            return GetOrAddComponent(body.gameObject);
        }

        public static DistortionManager GetOrAddComponent(GameObject bodyObject)
        {
            return bodyObject.GetComponent<DistortionManager>() ?? bodyObject.AddComponent<DistortionManager>();
        }
    }
}