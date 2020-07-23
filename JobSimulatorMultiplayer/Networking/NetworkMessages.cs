using Steamworks;
using UnityEngine;

namespace JobSimulatorMultiplayer.Networking 
{
    public enum MessageType
    {
        Join,
        PlayerName,
        OtherPlayerName,
        PlayerPosition,
        OtherPlayerPosition,
        Disconnect,
        ServerShutdown,
        JoinRejected,
        SceneTransition,
        SetPartyId,
        SetServerSetting,
        IdAllocation,
        IdRequest,
        ObjectSync
    }

    public interface INetworkMessage
    {
        P2PMessage MakeMsg();
    }

    // Server -> clients
    public class OtherPlayerPositionMessage : INetworkMessage
    {
        public Vector3 headPos;
        public Vector3 lHandPos;
        public Vector3 rHandPos;

        public Quaternion headRot;
        public Quaternion lHandRot;
        public Quaternion rHandRot;
        public byte playerId;

        public OtherPlayerPositionMessage(P2PMessage msg)
        {
            playerId = msg.ReadByte();

            headPos = msg.ReadVector3();
            lHandPos = msg.ReadVector3();
            rHandPos = msg.ReadVector3();

            headRot = msg.ReadCompressedQuaternion();
            lHandRot = msg.ReadCompressedQuaternion();
            rHandRot = msg.ReadCompressedQuaternion();
        }

        public OtherPlayerPositionMessage()
        { }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.OtherPlayerPosition);
            msg.WriteByte(playerId);

            msg.WriteVector3(headPos);
            msg.WriteVector3(lHandPos);
            msg.WriteVector3(rHandPos);

            msg.WriteCompressedQuaternion(headRot);
            msg.WriteCompressedQuaternion(lHandRot);
            msg.WriteCompressedQuaternion(rHandRot);
            return msg;
        }
    }

    // Client player -> server
    public class PlayerPositionMessage : INetworkMessage
    {
        public byte playerId;

        public Vector3 headPos;
        public Vector3 lHandPos;
        public Vector3 rHandPos;

        public Quaternion headRot;
        public Quaternion lHandRot;
        public Quaternion rHandRot;

        public PlayerPositionMessage(P2PMessage msg)
        {
            playerId = msg.ReadByte();

            headPos = msg.ReadVector3();
            lHandPos = msg.ReadVector3();
            rHandPos = msg.ReadVector3();

            headRot = msg.ReadCompressedQuaternion();
            lHandRot = msg.ReadCompressedQuaternion();
            rHandRot = msg.ReadCompressedQuaternion();
        }

        public PlayerPositionMessage()
        { }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.PlayerPosition);
            msg.WriteByte(playerId);

            msg.WriteVector3(headPos);
            msg.WriteVector3(lHandPos);
            msg.WriteVector3(rHandPos);

            msg.WriteCompressedQuaternion(headRot);
            msg.WriteCompressedQuaternion(lHandRot);
            msg.WriteCompressedQuaternion(rHandRot);
            return msg;
        }
    }

    public class SceneTransitionMessage : INetworkMessage
    {
        public string sceneName;

        public SceneTransitionMessage(P2PMessage msg)
        {
            sceneName = msg.ReadUnicodeString();
        }

        public SceneTransitionMessage()
        {

        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.SceneTransition);
            msg.WriteUnicodeString(sceneName);
            return msg;
        }
    }

    public class PlayerNameMessage : INetworkMessage
    {
        public string name;

        public PlayerNameMessage()
        {

        }

        public PlayerNameMessage(P2PMessage msg)
        {
            name = msg.ReadUnicodeString();
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.PlayerName);
            msg.WriteUnicodeString(name);
            return msg;
        }
    }

    public class OtherPlayerNameMessage : INetworkMessage
    {
        public byte playerId;
        public string name;

        public OtherPlayerNameMessage()
        {
        }

        public OtherPlayerNameMessage(P2PMessage msg)
        {
            playerId = msg.ReadByte();
            name = msg.ReadUnicodeString();
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.OtherPlayerName);
            msg.WriteByte(playerId);
            msg.WriteUnicodeString(name);
            return msg;
        }
    }

    public class ClientJoinMessage : INetworkMessage
    {
        public byte playerId;
        public string name;
        public SteamId steamId;

        public ClientJoinMessage()
        {
        }

        public ClientJoinMessage(P2PMessage msg)
        {
            playerId = msg.ReadByte();
            steamId.Value = msg.ReadUlong();
            name = msg.ReadUnicodeString();
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.Join);
            msg.WriteByte(playerId);
            msg.WriteUlong(steamId.Value);
            msg.WriteUnicodeString(name);
            return msg;
        }
    }

    public class SetPartyIdMessage : INetworkMessage
    {
        public string partyId;

        public SetPartyIdMessage()
        {
        }

        public SetPartyIdMessage(P2PMessage msg)
        {
            partyId = msg.ReadUnicodeString();
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.SetPartyId);
            msg.WriteUnicodeString(partyId);
            return msg;
        }
    }

    public class IDAllocationMessage : INetworkMessage
    {
        public string namePath;
        public byte allocatedId;

        public IDAllocationMessage()
        {

        }

        public IDAllocationMessage(P2PMessage msg)
        {
            namePath = msg.ReadUnicodeString();
            allocatedId = msg.ReadByte();
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();

            msg.WriteByte((byte)MessageType.IdAllocation);
            msg.WriteUnicodeString(namePath);
            msg.WriteByte(allocatedId);

            return msg;
        }
    }

    public class IDRequestMessage : INetworkMessage
    {
        public string name;

        public IDRequestMessage()
        {

        }

        public IDRequestMessage(P2PMessage msg)
        {
            name = msg.ReadUnicodeString();
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();

            msg.WriteByte((byte)MessageType.IdRequest);
            msg.WriteUnicodeString(name);

            return msg;
        }
    }

    public class ObjectSyncMessage : INetworkMessage
    {
        public byte id;
        public Vector3 position;
        public Quaternion rotation;

        public ObjectSyncMessage()
        { }

        public ObjectSyncMessage(P2PMessage msg)
        {
            id = msg.ReadByte();
            position = msg.ReadVector3();
            rotation = msg.ReadCompressedQuaternion();
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.ObjectSync);
            msg.WriteByte(id);
            msg.WriteVector3(position);
            msg.WriteCompressedQuaternion(rotation);

            return msg;
        }
    }
}