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
        private GameObjectType bodyOrMaster;
        private NetworkInstanceId ownerId;
        private short numbering;

        public SpawnOptionsForClients()
        {
        }

        public SpawnOptionsForClients(GameObjectType bodyOrMaster, NetworkInstanceId ownerId, short numbering)
        {
            this.bodyOrMaster = bodyOrMaster;
            this.ownerId = ownerId;
            this.numbering = numbering;
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write((byte)bodyOrMaster);
            writer.Write(ownerId);
            writer.Write(numbering);
        }

        public void Deserialize(NetworkReader reader)
        {
            bodyOrMaster = (GameObjectType)reader.ReadByte();
            ownerId = reader.ReadNetworkId();
            numbering = reader.ReadInt16();
        }

        public void OnReceived()
        {
            if (NetworkServer.active)
            {
                ClassicItemsPlugin._logger.LogMessage("SpawnOptionsForClients: Host got this request. Skip.");
                return;
            }
            ClassicItemsPlugin._logger.LogMessage($"SpawnOptionsForClients: Received a request to spawn options from server. ownerId = {ownerId}, numbering = {numbering}");
            GameObject ownerObject = Util.FindNetworkObject(ownerId);
            if (!ownerObject)
            {
                ClassicItemsPlugin._logger.LogWarning("SpawnOptionsForClients: ownerObject is null.");
                return;
            }
            switch (bodyOrMaster)
            {
                case GameObjectType.Body:
                    TrySpawnOption(ownerObject.GetComponent<CharacterBody>());
                    break;

                case GameObjectType.Master:
                    CharacterMaster ownerMaster = ownerObject.GetComponent<CharacterMaster>();
                    if (!ownerMaster)
                    {
                        ClassicItemsPlugin._logger.LogWarning("SpawnOptionsForClients: ownerMaster is null.");
                        return;
                    }
                    TrySpawnOption(ownerMaster.GetBody());
                    break;
            }
        }

        private void TrySpawnOption(CharacterBody ownerBody)
        {
            if (!ownerBody)
            {
                ClassicItemsPlugin._logger.LogWarning("SpawnOptionsForClients: ownerBody is null.");
                return;
            }
            OptionMasterTracker.SpawnOption(ownerBody.gameObject, numbering);
            ClassicItemsPlugin._logger.LogMessage("SpawnOptionsForClients: Option is good to go.");
        }

        public enum GameObjectType : byte
        {
            Master,
            Body
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
                ClassicItemsPlugin._logger.LogMessage($"SyncFlamethrowerEffectForClients: Host received the request. Skip.");
                return;
            }
            GameObject bodyObject = Util.FindNetworkObject(ownerBodyId);
            if (!bodyObject)
            {
                ClassicItemsPlugin._logger.LogWarning($"SyncFlamethrowerEffectForClients: bodyObject is null.");
                return;
            }
            OptionTracker tracker = bodyObject.GetComponent<OptionTracker>();
            if (!tracker)
            {
                ClassicItemsPlugin._logger.LogWarning($"SyncFlamethrowerEffectForClients: tracker is null.");
                return;
            }
            GameObject option = tracker.existingOptions[optionNumbering - 1];
            OptionBehavior behavior = option.GetComponent<OptionBehavior>();
            if (!behavior)
            {
                ClassicItemsPlugin._logger.LogWarning($"SyncFlamethrowerEffectForClients: behavior is null.");
                return;
            }
            switch (messageType)
            {
                case MessageType.Create:
                    if (GradiusOption.instance.flamethrowerSoundCopy) Util.PlaySound(MageWeapon.Flamethrower.startAttackSoundString, option);
                    behavior.flamethrower = Object.Instantiate(ClassicItemsPlugin.flamethrowerEffectPrefab, option.transform);
                    behavior.flamethrower.GetComponent<ScaleParticleSystemDuration>().newDuration = duration;
                    break;

                case MessageType.Destroy:
                    if (behavior.flamethrower) EntityState.Destroy(behavior.flamethrower);
                    break;

                case MessageType.Redirect:
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