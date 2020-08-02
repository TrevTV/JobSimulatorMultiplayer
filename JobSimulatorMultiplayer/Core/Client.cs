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
using System.Linq;
using Il2CppSystem.Diagnostics.Tracing;
using System.Collections;
using static UnityEngine.Object;

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

            MelonCoroutines.Start(PhysicSyncLoad());
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

            RichPresence.OnJoin -= RichPresence_OnJoin;
            RichPresence.SetActivity(new Activity() { Details = "Idle", Assets = { LargeImage = "jobsim" } });
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

                                    /*MelonModLogger.Log($@"oppm-----------------    
                                    SteamID: {oppm.playerId}
                                    LeftHand: {oppm.lHandPos.ToString()}    
                                    RightHand: {oppm.rHandPos.ToString()}    
                                    Head: {oppm.headPos.ToString()}    
                                    ---------------------");*/
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

                                    MelonModLogger.Log($@"ppm------------------    
                                    SteamID: {ppm.playerId}
                                    LeftHand: {ppm.lHandPos.ToString()}    
                                    RightHand: {ppm.rHandPos.ToString()}    
                                    Head: {ppm.headPos.ToString()}    
                                    ---------------------");
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
                        case MessageType.ObjectSync:
                            {
                                ObjectSyncMessage osm = new ObjectSyncMessage(msg);
                                MelonModLogger.Log($"Received object sync");

                                for (int i = 0; i < osm.objectsToSync.Count; i++)
                                {
                                    GameObject obj = ObjectIDManager.GetObject(osm.objectsToSync.Keys.ToList()[i]).gameObject;

                                    if (!obj)
                                    {
                                        MelonModLogger.LogError($"Couldn't find object with ID {obj.name}");
                                    }
                                    else
                                    {
                                        obj.transform.position = osm.objectsToSync.Values.ToList()[i].Item1;
                                        obj.transform.rotation = osm.objectsToSync.Values.ToList()[i].Item2;
                                    }

                                    MelonModLogger.Log($"got sync message with id: {obj.name}");
                                } //oh, yeah
                                // but that's in the loop and won't bring if it doesn't deserialize the objects correctly
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

                foreach (var id in ObjectIDManager.objects.Keys)
                {
                    ObjectIDManager.GetObject(id).gameObject.GetComponent<Rigidbody>().isKinematic = true;
                }
            }
        }

        private PlayerRep GetPlayerRep(byte playerId)
        {
            return playerObjects[playerId];
        }

        public void SendToServer(P2PMessage msg, P2PSend send)
        {
            byte[] msgBytes = msg.GetBytes();
            SteamNetworking.SendP2PPacket(ServerId, msgBytes, msgBytes.Length, 0, send);
        }

        private void SendSync() //logging is extremely slow
        {
            ObjectSyncMessage osm = new ObjectSyncMessage();
            foreach (var pair in ObjectIDManager.objects)
            {
                ServerSyncedObject sso = pair.Value;

                if (sso.transform.hasChanged)
                {
                    sso.transform.hasChanged = false;
                    // Sync it
                    //pair.Value.lastSyncedPos = pair.Value.transform.position;
                    //pair.Value.lastSyncedRotation = pair.Value.transform.rotation;
                    osm.objectsToSync.Add(sso.IDHolder.ID, Tuple.Create(sso.gameObject.transform.position, sso.gameObject.transform.rotation));
                }
            }
            SendToServer(osm.MakeMsg(), P2PSend.Unreliable);
        }

        public IEnumerator PhysicSyncLoad()
        {
            ObjectIDManager.objects.Clear();

            yield return new WaitForSeconds(3);

            MelonModLogger.Log("Getting and adding all Rigidbodies");
            var rbs = FindObjectsOfType<Rigidbody>();
            foreach (var rb in rbs)
            {
                if (rb.gameObject.transform.root.gameObject.name.Contains("HMD") || rb.isKinematic == true)
                    continue;

                var sso = rb.gameObject.AddComponent<ServerSyncedObject>();
                var idHolder = rb.gameObject.AddComponent<IDHolder>();

                idHolder.ID = ObjectIDManager.GenerateID(sso);
                ObjectIDManager.AddObject(idHolder.ID, sso);
                MelonModLogger.Log($"added {rb.gameObject.name} with generated id {idHolder.ID.ToString()}");
            }
        }
    }
}