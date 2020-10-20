using EntityStates;
using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using RoR2.Skills;
using RoR2.UI;
using System.Collections;
using System.Collections.Generic;
using TILER2;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace Chen.ClassicItems
{
    public class Distortion : Artifact_V2<Distortion>
    {
        public override string displayName => "Artifact of Distortion";

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("The time when skill lockdown shifts in seconds.", AutoConfigFlags.PreventNetMismatch, 0, int.MaxValue)]
        public int intervalBetweenLocks { get; private set; } = 60;

        [AutoConfig("The syncing time for Distortion effects towards Clients. There is no need to modify this unless there is a problem. " +
                    "Increase this if clients do not get their skills locked. Setting to 0 may cause problems.",
                    AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float syncSeconds { get; private set; } = .5f;

        protected override string GetNameString(string langid = null) => displayName;

        protected override string GetDescString(string langid = null) => $"Lock a random active or passive skill every {intervalBetweenLocks} seconds.";

        public static SkillDef distortSkill;

        public Distortion()
        {
            iconResourcePath = "@ChensClassicItems:Assets/ClassicItems/icons/distortion_artifact_on_icon.png";
            iconResourcePathDisabled = "@ChensClassicItems:Assets/ClassicItems/Icons/distortion_artifact_off_icon.png";
        }

        public override void SetupBehavior()
        {
            NetworkingAPI.RegisterMessageType<SpawnDistortionComponent>();

            LanguageAPI.Add("ALL_DISTORTION_LOCKED_NAME", "Distorted");
            LanguageAPI.Add("ALL_DISTORTION_LOCKED_DESCRIPTION", "You forgot how to perform this skill.");

            distortSkill = ScriptableObject.CreateInstance<SkillDef>();
            distortSkill.activationState = new SerializableEntityStateType(nameof(Idle));
            distortSkill.activationStateMachineName = "Weapon";
            distortSkill.baseMaxStock = 0;
            distortSkill.baseRechargeInterval = 0f;
            distortSkill.beginSkillCooldownOnSkillEnd = true;
            distortSkill.canceledFromSprinting = false;
            distortSkill.fullRestockOnAssign = true;
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
            On.RoR2.CharacterMaster.SpawnBody += CharacterMaster_SpawnBody;
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
    }

    public class DistortionManager : MonoBehaviour
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
                genericSkills[lockedSkillIndex].RecalculateMaxStock();
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

    public class DistortionQueue : MonoBehaviour
    {
        public List<NetworkInstanceId> netIds { get; private set; } = new List<NetworkInstanceId>();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by UnityEngine")]
        private void FixedUpdate()
        {
            if (!PauseScreenController.paused && NetworkServer.active && NetworkUser.AllParticipatingNetworkUsersReady() && netIds.Count > 0)
            {
                NetworkInstanceId[] copy = new NetworkInstanceId[netIds.Count];
                netIds.CopyTo(copy);
                netIds.Clear();
                for (int i = 0; i < copy.Length; i++)
                {
                    StartCoroutine(SendSignal(copy[i]));
                }
            }
        }

        private IEnumerator SendSignal(NetworkInstanceId netId)
        {
            yield return new WaitForSeconds(Distortion.instance.syncSeconds);
            new SpawnDistortionComponent(netId).Send(NetworkDestination.Clients);
        }

        public static DistortionQueue GetOrAddComponent(CharacterMaster master)
        {
            return GetOrAddComponent(master.gameObject);
        }

        public static DistortionQueue GetOrAddComponent(GameObject masterObject)
        {
            return masterObject.GetComponent<DistortionQueue>() ?? masterObject.AddComponent<DistortionQueue>();
        }
    }

    public class SpawnDistortionComponent : INetMessage
    {
        private NetworkInstanceId ownerBodyId;

        public SpawnDistortionComponent()
        {
        }

        public SpawnDistortionComponent(NetworkInstanceId ownerBodyId)
        {
            this.ownerBodyId = ownerBodyId;
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(ownerBodyId);
        }

        public void Deserialize(NetworkReader reader)
        {
            ownerBodyId = reader.ReadNetworkId();
        }

        public void OnReceived()
        {
            if (NetworkServer.active) return;
            GameObject bodyObject = Util.FindNetworkObject(ownerBodyId);
            if (!bodyObject)
            {
                ClassicItemsPlugin._logger.LogWarning($"SpawnDistortionComponent: bodyObject is null.");
                return;
            }
            CharacterBody body = bodyObject.GetComponent<CharacterBody>();
            if (!body)
            {
                ClassicItemsPlugin._logger.LogWarning($"SpawnDistortionComponent: body is null.");
                return;
            }
            if (!body.isPlayerControlled || !body.hasEffectiveAuthority)
            {
                ClassicItemsPlugin._logger.LogMessage($"SpawnDistortionComponent: You do not control this character. Skip.");
                return;
            }
            DistortionManager.GetOrAddComponent(bodyObject);
        }
    }
}