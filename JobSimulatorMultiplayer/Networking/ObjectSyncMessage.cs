using UnityEngine;
using OwlchemyVR;
using System.Collections.Generic;
using System;
using MelonLoader;

namespace JobSimulatorMultiplayer.Networking
{
    public class ObjectSyncMessage : INetworkMessage
    {
        //public int ID;
        //public Vector3 position;
        //public Quaternion rotation;

        public Dictionary<int, Tuple<Vector3, Quaternion>> objectsToSync = new Dictionary<int, Tuple<Vector3, Quaternion>>();

        public ObjectSyncMessage()
        { }

        public ObjectSyncMessage(P2PMessage msg)
        {
            for(int i=0; i < objectsToSync.Count; i++)
            {
                objectsToSync.Add(msg.ReadByte(), Tuple.Create(msg.ReadVector3(), msg.ReadCompressedQuaternion()));
            }
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.ObjectSync);
            msg.WriteSyncDict(objectsToSync);
            return msg;
        }
    }
}