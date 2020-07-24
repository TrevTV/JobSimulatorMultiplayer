using UnityEngine;
using OwlchemyVR;

namespace JobSimulatorMultiplayer.Networking
{
    public class ObjectSyncMessage : INetworkMessage
    {
        public WorldItem worldItem;
        public Vector3 position;
        public Quaternion rotation;

        public ObjectSyncMessage()
        { }

        public ObjectSyncMessage(P2PMessage msg)
        {
            worldItem = msg.ReadByte(); // idk yet
            position = msg.ReadVector3();
            rotation = msg.ReadCompressedQuaternion();
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.ObjectSync);
            msg.WriteByte(worldItem);
            msg.WriteVector3(position);
            msg.WriteCompressedQuaternion(rotation);

            return msg;
        }
    }
}