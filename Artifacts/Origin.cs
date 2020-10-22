#undef DISABLE
#if DISABLE
using RoR2;
using RoR2.Skills;
using TILER2;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace Chen.ClassicItems
{
    public class Origin : Artifact_V2<Origin>
    {
        public override string displayName => "Artifact of Origin";

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Amount of time in minutes for Imps to invade the area.", AutoConfigFlags.PreventNetMismatch, 0, int.MaxValue)]
        public int spawnInterval { get; private set; } = 9;

        protected override string GetNameString(string langid = null) => displayName;

        protected override string GetDescString(string langid = null) => $"Imps will be sent to invade to destroy you every {spawnInterval} minutes.";

        public Origin()
        {
            iconResourcePath = "@ChensClassicItems:Assets/ClassicItems/icons/spirit_artifact_on_icon.png";
            iconResourcePathDisabled = "@ChensClassicItems:Assets/ClassicItems/Icons/spirit_artifact_off_icon.png";
        }

        public override void Install()
        {
            base.Install();
            On.RoR2.CharacterMaster.SpawnBody += CharacterMaster_SpawnBody;
            Run.onRunStartGlobal += Run_onRunStartGlobal;
        }

        public override void Uninstall()
        {
            base.Uninstall();
            On.RoR2.CharacterMaster.SpawnBody -= CharacterMaster_SpawnBody;
        }

        private CharacterBody CharacterMaster_SpawnBody(On.RoR2.CharacterMaster.orig_SpawnBody orig, CharacterMaster self, GameObject bodyPrefab, Vector3 position, Quaternion rotation)
        {
            CharacterBody body = orig(self, bodyPrefab, position, rotation);
            if (IsActiveAndEnabled() && body && body.isPlayerControlled)
            {
                DistortionManager.GetOrAddComponent(body);
                DistortionQueue queue = DistortionQueue.GetOrAddComponent(body.master);
                NetworkIdentity identity = body.gameObject.GetComponent<NetworkIdentity>();
                if (queue && identity)
                {
                    queue.netIds.Add(identity.netId);
                }
            }
            return body;
        }

        private void Run_onRunStartGlobal(Run obj)
        {
            throw new System.NotImplementedException();
        }
    }

    public class OriginManager : MonoBehaviour
    {
        public GenericSkill[] genericSkills;
        private bool init = true;
        private CharacterBody body;
        private int timer = -1;
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
                    timer++;
                    LockRandomSkill();
                }
                if (timer > 60 * Distortion.instance.intervalBetweenLocks)
                {
                    timer = 0;
                    UnlockSkill();
                    LockRandomSkill();
                }
                else timer++;
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
                lockedSkillIndex = Random.Range(0, genericSkills.Length);
                oldSkillDef = genericSkills[lockedSkillIndex].skillDef;
                genericSkills[lockedSkillIndex].AssignSkill(Distortion.distortSkill);
                genericSkills[lockedSkillIndex].stock = 0;
                return lockedSkillIndex;
            }
            return -1;
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
#endif