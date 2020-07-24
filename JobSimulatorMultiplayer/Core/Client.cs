using JobSimulatorMultiplayer.Networking;
using JobSimulatorMultiplayer.Representations;
using MelonLoader;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using UnityEngine;
using JobSimulatorMultiplayer.MonoBehaviours;
using Discord;
using OwlchemyVR;

namespace JobSimulatorMultiplayer.Core
{
    public class Client
    {
        public SteamId ServerId
        {
            get; private set;
        }

        private readonly Dictionary<byte, PlayerRep> playerObjects = new Dictionary<byte, PlayerRep>(JobSimulatorMultiplayer.MAX_PLAYERS);
        private readonly Dictionary<byte, string> playerNames = new Dictionary<byte, string>(JobSimulatorMultiplayer.MAX_PLAYERS);
        private readonly Dictionary<byte, SteamId> largePlayerIds = new Dictionary<byte, SteamId>(JobSimulatorMultiplayer.MAX_PLAYERS);
        private readonly Dictionary<SteamId, byte> smallPlayerIds = new Dictionary<SteamId, byte>(JobSimulatorMultiplayer.MAX_PLAYERS);
        public bool isConnected = false;

        public void SetupRP()
        {
            RichPresence.OnJoin += RichPresence_OnJoin;
        }

        public void RecreatePlayers()
        {
            List<byte> ids = new List<byte>();
            List<SteamId> steamIds = new List<SteamId>();

            foreach (byte id in playerObjects.Keys)
            {
                ids.Add(id);
                steamIds.Add(playerObjects[id].steamId);
            }

            int i = 0;
            foreach (byte id in ids)
            {
                playerObjects[id] = new PlayerRep(playerNames[id], steamIds[i]);
            }
        }

        public void Connect(string obj)
        {
            MelonModLogger.Log("Starting client and connecting");

            ServerId = ulong.Parse(obj);
            MelonModLogger.Log("Connecting to " + obj);

            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.Join);
            msg.WriteByte(JobSimulatorMultiplayer.PROTOCOL_VERSION);
            msg.WriteUnicodeString(SteamClient.Name);
            SteamNetworking.SendP2PPacket(ServerId, msg.GetBytes());

            isConnected = true;

            SteamNetworking.OnP2PSessionRequest = OnP2PSessionRequest;
            SteamNetworking.OnP2PConnectionFailed = OnP2PConnectionFailed;
        }

        private void OnP2PConnectionFailed(SteamId id, P2PSessionError err)
        {
            if (id == ServerId)
            {
                MelonModLogger.LogError("Got P2P connection error " + err.ToString());
                foreach (PlayerRep pr in playerObjects.Values)
                {
                    pr.Destroy();
                }
            }
        }

        private void OnP2PSessionRequest(SteamId id)
        {
            if (id != ServerId)
            {
                MelonModLogger.LogError("Got a P2P session request from something that is not the server.");
            }
            else
            {
                SteamNetworking.AcceptP2PSessionWithUser(id);
            }
        }

        private void RichPresence_OnJoin(string obj)
        {
            Connect(obj);
        }

        public void Disconnect()
        {
            try
            {
                foreach (PlayerRep r in playerObjects.Values)
                    r.Destroy();
            }
            catch (Exception)
            {
                MelonModLogger.LogError("Caught exception destroying player objects");
            }

            MelonModLogger.Log("Disconnecting...");
            isConnected = false;
            ServerId = 0;
            playerObjects.Clear();
            playerNames.Clear();
            largePlayerIds.Clear();
            smallPlayerIds.Clear();

            SteamNetworking.CloseP2PSessionWithUser(ServerId);
            //PlayerHooks.OnPlayerGrabObject -= PlayerHooks_OnPlayerGrabObject;
            //PlayerHooks.OnPlayerLetGoObject -= PlayerHooks_OnPlayerLetGoObject;
        }

        public void Update()
        {
            while (SteamNetworking.IsP2PPacketAvailable(0))
            {
                P2Packet? packet = SteamNetworking.ReadP2PPacket(0);

                if (packet.HasValue)
                {
                    P2PMessage msg = new P2PMessage(packet.Value.Data);

                    MessageType type = (MessageType)msg.ReadByte();

                    switch (type)
                    {
                        case MessageType.OtherPlayerPosition:
                            {
                                OtherPlayerPositionMessage oppm = new OtherPlayerPositionMessage(msg);

                                if (playerObjects.ContainsKey(oppm.playerId))
                                {
                                    PlayerRep pr = GetPlayerRep(oppm.playerId);

                                    pr.head.transform.position = oppm.headPos;
                                    pr.handL.transform.position = oppm.lHandPos;
                                    pr.handR.transform.position = oppm.rHandPos;

                                    pr.head.transform.rotation = oppm.headRot;
                                    pr.handL.transform.rotation = oppm.lHandRot;
                                    pr.handR.transform.rotation = oppm.rHandRot;
                                }

                                break;
                            }
                        case MessageType.PlayerPosition:
                            {
                                PlayerPositionMessage ppm = new PlayerPositionMessage(msg);

                                if (playerObjects.ContainsKey(ppm.playerId))
                                {
                                    PlayerRep pr = GetPlayerRep(ppm.playerId);

                                    pr.head.transform.position = ppm.headPos;
                                    pr.handL.transform.position = ppm.lHandPos;
                                    pr.handR.transform.position = ppm.rHandPos;

                                    pr.head.transform.rotation = ppm.headRot;
                                    pr.handL.transform.rotation = ppm.lHandRot;
                                    pr.handR.transform.rotation = ppm.rHandRot;
                                }

                                break;
                            }
                        case MessageType.ServerShutdown:
                            {
                                foreach (PlayerRep pr in playerObjects.Values)
                                {
                                    pr.Destroy();
                                }
                                break;
                            }
                        case MessageType.Disconnect:
                            {
                                byte pid = msg.ReadByte();
                                playerObjects[pid].Destroy();
                                playerObjects.Remove(pid);
                                largePlayerIds.Remove(pid);
                                playerNames.Remove(pid);
                                break;
                            }
                        case MessageType.JoinRejected:
                            {
                                MelonModLogger.LogError("Join rejected - you are using an incompatible version of the mod!");
                                Disconnect();
                                break;
                            }
                        case MessageType.Join:
                            {
                                ClientJoinMessage cjm = new ClientJoinMessage(msg);
                                largePlayerIds.Add(cjm.playerId, cjm.steamId);
                                playerNames.Add(cjm.playerId, cjm.name);
                                playerObjects.Add(cjm.playerId, new PlayerRep(cjm.name, cjm.steamId));
                                break;
                            }
                        case MessageType.IdAllocation:
                            {
                                IDAllocationMessage iam = new IDAllocationMessage(msg);
                                GameObject obj = Util.GetObjectFromFullPath(iam.namePath);
                                ObjectIDManager.AddObject(obj.GetComponent<WorldItem>(), obj.GetComponent<ServerSyncedObject>());
                                obj.AddComponent<IDHolder>().ID = iam.allocatedId;
                                break;
                            }
                        case MessageType.ObjectSync:
                            {
                                ObjectSyncMessage osm = new ObjectSyncMessage(msg);
                                GameObject obj = ObjectIDManager.GetObject(osm.worldItem).gameObject;

                                MelonModLogger.Log($"got sync message with worlditem: {obj.name}");

                                if (!obj)
                                {
                                    MelonModLogger.LogError($"Couldn't find object with ID {obj.name}");
                                }
                                else
                                {
                                    obj.transform.position = osm.position;
                                    obj.transform.rotation = osm.rotation;
                                }
                                break;
                            }
                        case MessageType.SetPartyId:
                            {
                                SetPartyIdMessage spid = new SetPartyIdMessage(msg);
                                RichPresence.SetActivity(
                                    new Activity()
                                    {
                                        State = "Connected to a server",
                                        Assets = { LargeImage = "jobsim" },
                                        Secrets = new ActivitySecrets() { Join = ServerId.ToString() },
                                        Party = new ActivityParty()
                                        {
                                            Id = spid.partyId,
                                            Size = new PartySize()
                                            {
                                                CurrentSize = 1,
                                                MaxSize = JobSimulatorMultiplayer.MAX_PLAYERS
                                            }
                                        }
                                    });
                                break;
                            }
                    }
                }
            }
            {
                if (GlobalStorage.Instance.MasterHMDAndInputController != null)
                {
                    PlayerPositionMessage ppm = new PlayerPositionMessage
                    {
                        headPos = GlobalStorage.Instance.MasterHMDAndInputController.camTransform.position,
                        lHandPos = GlobalStorage.Instance.MasterHMDAndInputController.LeftHand.cfjTransform.position,
                        rHandPos = GlobalStorage.Instance.MasterHMDAndInputController.RightHand.cfjTransform.position,
                        
                        headRot = GlobalStorage.Instance.MasterHMDAndInputController.camTransform.rotation,
                        lHandRot = GlobalStorage.Instance.MasterHMDAndInputController.LeftHand.cfjTransform.rotation,
                        rHandRot = GlobalStorage.Instance.MasterHMDAndInputController.RightHand.cfjTransform.rotation,
                    };

                    SendToServer(ppm.MakeMsg(), P2PSend.Unreliable);
                }

                /*foreach (var pair in ObjectIDManager.objects)
                {
                    if (pair.Value.NeedsSync())
                    {
                        pair.Value.lastSyncedPos = pair.Value.transform.position;
                        pair.Value.lastSyncedRotation = pair.Value.transform.rotation;

                        ObjectSyncMessage osm = new ObjectSyncMessage
                        {
                            id = pair.Key,
                            position = pair.Value.transform.position,
                            rotation = pair.Value.transform.rotation
                        };

                        SendToServer(osm.MakeMsg(), P2PSend.Unreliable);
                    }
                }*/
            }
        }

        private PlayerRep GetPlayerRep(byte playerId)
        {
            return playerObjects[playerId];
        }

        private void SendToServer(P2PMessage msg, P2PSend send)
        {
            byte[] msgBytes = msg.GetBytes();
            SteamNetworking.SendP2PPacket(ServerId, msgBytes, msgBytes.Length, 0, send);
        }
    }
}