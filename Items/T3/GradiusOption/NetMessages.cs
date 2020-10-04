using EntityStates;
using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using MageWeapon = EntityStates.Mage.Weapon;

namespace Chen.ClassicItems
{
    public class SpawnOptionsForClients : INetMessage
    {
        private NetworkInstanceId ownerId;
        private short numbering;
        private bool bodyOrMaster;

        public SpawnOptionsForClients()
        {
        }

        public SpawnOptionsForClients(NetworkInstanceId ownerId, short numbering, bool bodyOrMaster)
        {
            this.ownerId = ownerId;
            this.numbering = numbering;
            this.bodyOrMaster = bodyOrMaster;
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(ownerId);
            writer.Write(numbering);
            writer.Write(bodyOrMaster);
        }

        public void Deserialize(NetworkReader reader)
        {
            ownerId = reader.ReadNetworkId();
            numbering = reader.ReadInt16();
            bodyOrMaster = reader.ReadBoolean();
        }

        public void OnReceived()
        {
            if (!NetworkServer.active)
            {
                ClassicItemsPlugin._logger.LogDebug($"SpawnOptionsForClients: Received a request to spawn options from server. ownerId = {ownerId}, numbering = {numbering}");
                GameObject ownerObject = Util.FindNetworkObject(ownerId);
                if (ownerObject)
                {
                    if (bodyOrMaster)
                    {
                        ClassicItemsPlugin._logger.LogDebug("SpawnOptionsForClients: BODY MODE - Getting CharacterBody...");
                        TrySpawnOption(ownerObject.GetComponent<CharacterBody>());
                    }
                    else
                    {
                        ClassicItemsPlugin._logger.LogDebug("SpawnOptionsForClients: MASTER MODE - Getting CharacterMaster...");
                        CharacterMaster ownerMaster = ownerObject.GetComponent<CharacterMaster>();
                        if (ownerMaster)
                        {
                            ClassicItemsPlugin._logger.LogDebug("SpawnOptionsForClients: Getting CharacterBody...");
                            TrySpawnOption(ownerMaster.GetBody());
                        }
                        else ClassicItemsPlugin._logger.LogDebug("SpawnOptionsForClients: ownerMaster is null.");
                    }
                }
                else ClassicItemsPlugin._logger.LogDebug("SpawnOptionsForClients: ownerObject is null.");
            }
            else ClassicItemsPlugin._logger.LogDebug("SpawnOptionsForClients: Host got this request. Skip.");
        }

        private void TrySpawnOption(CharacterBody ownerBody)
        {
            if (ownerBody)
            {
                ClassicItemsPlugin._logger.LogDebug("SpawnOptionsForClients: Preparations complete. Firing SpawnOption method.");
                OptionMasterTracker.SpawnOption(ownerBody.gameObject, numbering);
                ClassicItemsPlugin._logger.LogDebug("SpawnOptionsForClients: Option is good to go.");
            }
            else ClassicItemsPlugin._logger.LogDebug("SpawnOptionsForClients: ownerBody is null.");
        }
    }

    public class SyncFlamethrowerEffectForClients : INetMessage
    {
        private MessageType messageType;
        private NetworkInstanceId ownerBodyId;
        private short optionNumbering;
        private float duration;
        private Vector3 direction;

        public SyncFlamethrowerEffectForClients()
        {
        }

        public SyncFlamethrowerEffectForClients(MessageType messageType, NetworkInstanceId id, short numbering, float duration, Vector3 direction)
        {
            this.messageType = messageType;
            ownerBodyId = id;
            optionNumbering = numbering;
            this.duration = duration;
            this.direction = direction;
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write((byte)messageType);
            writer.Write(ownerBodyId);
            writer.Write(optionNumbering);
            writer.Write(duration);
            writer.Write(direction);
        }

        public void Deserialize(NetworkReader reader)
        {
            messageType = (MessageType)reader.ReadByte();
            ownerBodyId = reader.ReadNetworkId();
            optionNumbering = reader.ReadInt16();
            duration = reader.ReadSingle();
            direction = reader.ReadVector3();
        }

        public void OnReceived()
        {
            if (NetworkServer.active)
            {
                ClassicItemsPlugin._logger.LogDebug($"SyncFlamethrowerEffectForClients: Host received the request. Skip.");
                return;
            }
            GameObject bodyObject = Util.FindNetworkObject(ownerBodyId);
            if (!bodyObject)
            {
                ClassicItemsPlugin._logger.LogDebug($"SyncFlamethrowerEffectForClients: bodyObject is null.");
                return;
            }
            OptionTracker tracker = bodyObject.GetComponent<OptionTracker>();
            if (!tracker)
            {
                ClassicItemsPlugin._logger.LogDebug($"SyncFlamethrowerEffectForClients: tracker is null.");
                return;
            }
            GameObject option = tracker.existingOptions[optionNumbering - 1];
            OptionBehavior behavior = option.GetComponent<OptionBehavior>();
            if (!behavior)
            {
                ClassicItemsPlugin._logger.LogDebug($"SyncFlamethrowerEffectForClients: behavior is null.");
                return;
            }
            switch (messageType)
            {
                case MessageType.Create:
                    ClassicItemsPlugin._logger.LogDebug($"SyncFlamethrowerEffectForClients: CREATE MODE.");
                    if (GradiusOption.instance.flamethrowerSoundCopy) Util.PlaySound(MageWeapon.Flamethrower.startAttackSoundString, option);
                    behavior.flamethrower = Object.Instantiate(ClassicItemsPlugin.flamethrowerEffectPrefab, option.transform);
                    behavior.flamethrower.GetComponent<ScaleParticleSystemDuration>().newDuration = duration;
                    break;
                case MessageType.Destroy:
                    ClassicItemsPlugin._logger.LogDebug($"SyncFlamethrowerEffectForClients: DESTROY MODE.");
                    if (behavior.flamethrower) EntityState.Destroy(behavior.flamethrower);
                    break;
                case MessageType.Redirect:
                    ClassicItemsPlugin._logger.LogDebug($"SyncFlamethrowerEffectForClients: REDIRECT MODE.");
                    if (behavior.flamethrower) behavior.flamethrower.transform.forward = direction;
                    break;
                default:
                    break;
            }
        }

        public enum MessageType : byte
        {
            Create,
            Destroy,
            Redirect
        };
    }
}