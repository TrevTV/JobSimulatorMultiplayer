﻿using Discord;
using JobSimulatorMultiplayer.MonoBehaviours;
using JobSimulatorMultiplayer.Networking;
using JobSimulatorMultiplayer.Representations;
using MelonLoader;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.Object;

namespace JobSimulatorMultiplayer.Core
{
    public class Server
    {
        private readonly Dictionary<byte, PlayerRep> playerObjects = new Dictionary<byte, PlayerRep>(JobSimulatorMultiplayer.MAX_PLAYERS);
        private readonly Dictionary<byte, string> playerNames = new Dictionary<byte, string>(JobSimulatorMultiplayer.MAX_PLAYERS);
        private readonly List<ulong> players = new List<ulong>();
        private readonly Dictionary<SteamId, byte> smallPlayerIds = new Dictionary<SteamId, byte>(JobSimulatorMultiplayer.MAX_PLAYERS);
        private readonly Dictionary<byte, SteamId> largePlayerIds = new Dictionary<byte, SteamId>(JobSimulatorMultiplayer.MAX_PLAYERS);
        private readonly Dictionary<GameObject, ServerSyncedObject> syncedObjectCache = new Dictionary<GameObject, ServerSyncedObject>();
        private string partyId = "";
        private byte smallIdCounter = 0;

        public bool IsRunning { get; private set; }

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
                        case MessageType.Join:
                            {
                                if (msg.ReadByte() != JobSimulatorMultiplayer.PROTOCOL_VERSION)
                                {
                                    // Somebody tried to join with an incompatible verison
                                    P2PMessage m2 = new P2PMessage();
                                    m2.WriteByte((byte)MessageType.JoinRejected);
                                    SteamNetworking.SendP2PPacket(packet.Value.SteamId, m2.GetBytes(), -1, 0, P2PSend.Reliable);
                                    SteamNetworking.CloseP2PSessionWithUser(packet.Value.SteamId);
                                }
                                else
                                {
                                    MelonModLogger.Log("Player joined with SteamID: " + packet.Value.SteamId);
                                    players.Add(packet.Value.SteamId);
                                    MelonModLogger.Log("Player count: " + players.Count);
                                    byte newPlayerId = smallIdCounter;
                                    smallPlayerIds.Add(packet.Value.SteamId, newPlayerId);
                                    largePlayerIds.Add(newPlayerId, packet.Value.SteamId);
                                    smallIdCounter++;

                                    string name = msg.ReadUnicodeString();
                                    MelonModLogger.Log("Name: " + name);

                                    foreach (var smallId in playerNames.Keys)
                                    {
                                        ClientJoinMessage cjm = new ClientJoinMessage
                                        {
                                            playerId = smallId,
                                            name = playerNames[smallId],
                                            steamId = largePlayerIds[smallId]
                                        };
                                        SteamNetworking.SendP2PPacket(packet.Value.SteamId, cjm.MakeMsg().GetBytes(), -1, 0, P2PSend.Reliable);
                                    }

                                    ClientJoinMessage cjm2 = new ClientJoinMessage
                                    {
                                        playerId = 0,
                                        name = SteamClient.Name,
                                        steamId = SteamClient.SteamId
                                    };
                                    SteamNetworking.SendP2PPacket(packet.Value.SteamId, cjm2.MakeMsg().GetBytes(), -1, 0, P2PSend.Reliable);

                                    playerNames.Add(newPlayerId, name);

                                    ClientJoinMessage cjm3 = new ClientJoinMessage
                                    {
                                        playerId = newPlayerId,
                                        name = name,
                                        steamId = packet.Value.SteamId
                                    };
                                    ServerSendToAllExcept(cjm3, P2PSend.Reliable, packet.Value.SteamId);

                                    playerObjects.Add(newPlayerId, new PlayerRep(name, packet.Value.SteamId));

                                    RichPresence.SetActivity(
                                        new Activity()
                                        {
                                            State = "Hosting a server",
                                            Assets = { LargeImage = "jobsim" },
                                            Secrets = new ActivitySecrets() { Join = SteamClient.SteamId.ToString() },
                                            Party = new ActivityParty()
                                            {
                                                Id = partyId,
                                                Size = new PartySize()
                                                {
                                                    CurrentSize = players.Count + 1,
                                                    MaxSize = JobSimulatorMultiplayer.MAX_PLAYERS
                                                }
                                            }
                                        });

                                    SceneTransitionMessage stm = new SceneTransitionMessage()
                                    {
                                        sceneName = SceneManager.GetActiveScene().name
                                    };
                                    SendToId(stm, P2PSend.Reliable, packet.Value.SteamId);

                                    SetPartyIdMessage spid = new SetPartyIdMessage()
                                    {
                                        partyId = partyId
                                    };
                                    SendToId(spid, P2PSend.Reliable, packet.Value.SteamId);
                                }
                                break;
                            }
                        case MessageType.Disconnect:
                            {
                                MelonModLogger.Log("Player left with SteamID: " + packet.Value.SteamId);
                                byte smallId = smallPlayerIds[packet.Value.SteamId];

                                P2PMessage disconnectMsg = new P2PMessage();
                                disconnectMsg.WriteByte((byte)MessageType.Disconnect);
                                disconnectMsg.WriteByte(smallId);
                                foreach (SteamId p in players)
                                {
                                    SteamNetworking.SendP2PPacket(p, disconnectMsg.GetBytes(), -1, 0, P2PSend.Reliable);
                                }

                                playerObjects[smallId].Destroy();
                                playerObjects.Remove(smallId);
                                players.RemoveAll((ulong val) => val == packet.Value.SteamId);
                                smallPlayerIds.Remove(packet.Value.SteamId);
                                break;
                            }
                        case MessageType.PlayerPosition:
                            {
                                if (smallPlayerIds.ContainsKey(packet.Value.SteamId))
                                {
                                    byte playerId = smallPlayerIds[packet.Value.SteamId];
                                    PlayerRep pr = GetPlayerRep(playerId);

                                    PlayerPositionMessage ppm = new PlayerPositionMessage(msg);
                                    pr.head.transform.position = ppm.headPos;
                                    pr.handL.transform.position = ppm.lHandPos;
                                    pr.handR.transform.position = ppm.rHandPos;

                                    pr.head.transform.rotation = ppm.headRot;
                                    pr.handL.transform.rotation = ppm.lHandRot;
                                    pr.handR.transform.rotation = ppm.rHandRot;

                                    /*MelonModLogger.Log($@"---------------------    
                                    SteamID: {pr.steamId.ToString()}
                                    LeftHand: {ppm.lHandPos.ToString()}    
                                    RightHand: {ppm.rHandPos.ToString()}    
                                    Head: {ppm.headPos.ToString()}    
                                    ---------------------");*/

                                    OtherPlayerPositionMessage relayOPPM = new OtherPlayerPositionMessage
                                    {
                                        headPos = ppm.headPos,
                                        lHandPos = ppm.lHandPos,
                                        rHandPos = ppm.rHandPos,

                                        headRot = ppm.headRot,
                                        lHandRot = ppm.lHandRot,
                                        rHandRot = ppm.rHandRot,
                                        playerId = ppm.playerId
                                    };
                                    ServerSendToAllExcept(relayOPPM, P2PSend.Unreliable, packet.Value.SteamId);
                                }
                                break;
                            }
                        case MessageType.ObjectSync:
                            {
                                ObjectSyncMessage osm = new ObjectSyncMessage(msg);

                                for(int i=0; i < osm.objectsToSync.Count; i++)
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
                                }
                                break;
                            }
                        default:
                            MelonModLogger.Log("Unknown message type: " + type.ToString());
                            break;
                    }
                }
            }

            /*
            foreach (var pair in ObjectIDManager.objects)
            {
                ServerSyncedObject sso = pair.Value;
                if (sso.NeedsSync())
                {
                    // Sync it
                    pair.Value.lastSyncedPos = pair.Value.transform.position;
                    pair.Value.lastSyncedRotation = pair.Value.transform.rotation;

                    ObjectSyncMessage osm = new ObjectSyncMessage
                    {
                        ID = sso.IDHolder.ID,
                        position = pair.Value.transform.position,
                        rotation = pair.Value.transform.rotation
                    };

                    //ServerSendToAll(osm, P2PSend.Unreliable);
                }
            }
            */

            SendSync();

            if (GlobalStorage.Instance.MasterHMDAndInputController != null)
            {
                OtherPlayerPositionMessage ppm = new OtherPlayerPositionMessage
                {
                    headPos = GlobalStorage.Instance.MasterHMDAndInputController.camTransform.position,
                    lHandPos = GlobalStorage.Instance.MasterHMDAndInputController.LeftHand.cfjTransform.position,
                    rHandPos = GlobalStorage.Instance.MasterHMDAndInputController.RightHand.cfjTransform.position,

                    headRot = GlobalStorage.Instance.MasterHMDAndInputController.camTransform.rotation,
                    lHandRot = GlobalStorage.Instance.MasterHMDAndInputController.LeftHand.cfjTransform.rotation,
                    rHandRot = GlobalStorage.Instance.MasterHMDAndInputController.RightHand.cfjTransform.rotation,
                };

                ServerSendToAll(ppm, P2PSend.Unreliable);
            }
        }

        private PlayerRep GetPlayerRep(byte playerId)
        {
            return playerObjects[playerId];
        }

        private void OnP2PSessionRequest(SteamId id)
        {
            SteamNetworking.AcceptP2PSessionWithUser(id);
            MelonModLogger.Log("Accepted session for " + id.ToString());
        }

        private void OnP2PConnectionFailed(SteamId id, P2PSessionError error)
        {
            if (error == P2PSessionError.NoRightsToApp)
            {
                MelonModLogger.LogError("You don't own the game on Steam.");
            }
            else if (error == P2PSessionError.NotRunningApp)
            {
                // Probably a leaver
                if (smallPlayerIds.ContainsKey(id))
                {
                    MelonModLogger.Log("Player left with SteamID: " + id);
                    byte smallId = smallPlayerIds[id];

                    P2PMessage disconnectMsg = new P2PMessage();
                    disconnectMsg.WriteByte((byte)MessageType.Disconnect);
                    disconnectMsg.WriteByte(smallId);

                    foreach (SteamId p in players)
                    {
                        SteamNetworking.SendP2PPacket(p, disconnectMsg.GetBytes(), -1, 0, P2PSend.Reliable);
                    }

                    playerObjects[smallId].Destroy();
                    playerObjects.Remove(smallId);
                    players.RemoveAll((ulong val) => val == id);
                    smallPlayerIds.Remove(id);
                }
            }
            else if (error == P2PSessionError.Timeout)
            {
                MelonModLogger.LogError("Connection with " + id + "timed out.");

                byte smallId = smallPlayerIds[id];

                P2PMessage disconnectMsg = new P2PMessage();
                disconnectMsg.WriteByte((byte)MessageType.Disconnect);
                disconnectMsg.WriteByte(smallId);

                foreach (SteamId p in players)
                {
                    SteamNetworking.SendP2PPacket(p, disconnectMsg.GetBytes(), -1, 0, P2PSend.Reliable);
                }

                playerObjects[smallId].Destroy();
                playerObjects.Remove(smallId);
                players.RemoveAll((ulong val) => val == id);
                smallPlayerIds.Remove(id);
            }
            else
            {
                MelonModLogger.LogError("Unhandled P2P error: " + error.ToString());
            }
        }

        public void StartServer()
        {
            MelonModLogger.Log("Starting server...");
            // localRigTransforms = BWUtil.GetLocalRigTransforms();
            partyId = SteamClient.SteamId + "P" + DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

            SteamNetworking.OnP2PSessionRequest = OnP2PSessionRequest;
            SteamNetworking.OnP2PConnectionFailed = OnP2PConnectionFailed;

            IsRunning = true;
            RichPresence.SetActivity(
                new Activity()
                {
                    Details = "Hosting a server",
                    Assets = { LargeImage = "jobsim" },
                    Secrets = new ActivitySecrets() { Join = SteamClient.SteamId.ToString() },
                    Party = new ActivityParty()
                    {
                        Id = partyId,
                        Size = new PartySize()
                        {
                            CurrentSize = 1,
                            MaxSize = JobSimulatorMultiplayer.MAX_PLAYERS
                        }
                    }
                });

            MelonCoroutines.Start(PhysicSyncLoad());
        }

        public void StopServer()
        {
            IsRunning = false;

            try
            {
                foreach (PlayerRep r in playerObjects.Values)
                {
                    r.Destroy();
                }
            }
            catch (Exception)
            {
                MelonModLogger.LogError("Caught exception destroying player objects");
            }

            playerObjects.Clear();
            playerNames.Clear();
            smallPlayerIds.Clear();
            largePlayerIds.Clear();
            smallIdCounter = 1;

            P2PMessage shutdownMsg = new P2PMessage();
            shutdownMsg.WriteByte((byte)MessageType.ServerShutdown);

            foreach (SteamId p in players)
            {
                SteamNetworking.SendP2PPacket(p, shutdownMsg.GetBytes(), -1, 0, P2PSend.Reliable);
                SteamNetworking.CloseP2PSessionWithUser(p);
            }

            players.Clear();

            SteamNetworking.OnP2PSessionRequest = null;
            SteamNetworking.OnP2PConnectionFailed = null;

            RichPresence.SetActivity(new Activity() { Details = "Idle", Assets = { LargeImage = "jobsim" } });
        }

        private void ServerSendToAll(INetworkMessage msg, P2PSend send)
        {
            P2PMessage pMsg = msg.MakeMsg();
            byte[] bytes = pMsg.GetBytes();
            foreach (SteamId p in players)
            {
                SteamNetworking.SendP2PPacket(p, bytes, bytes.Length, 0, send);
            }
        }

        private void ServerSendToAllExcept(INetworkMessage msg, P2PSend send, SteamId except)
        {
            P2PMessage pMsg = msg.MakeMsg();
            byte[] bytes = pMsg.GetBytes();
            foreach (SteamId p in players)
            {
                if (p != except)
                    SteamNetworking.SendP2PPacket(p, bytes, bytes.Length, 0, send);
            }
        }

        private void SendToId(INetworkMessage msg, P2PSend send, SteamId id)
        {
            P2PMessage pMsg = msg.MakeMsg();
            byte[] bytes = pMsg.GetBytes();
            SteamNetworking.SendP2PPacket(id, bytes, bytes.Length, 0, send);
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
            ServerSendToAll(osm, P2PSend.Unreliable);
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
                // MelonModLogger.Log($"added {rb.gameObject.name} with generated id {idHolder.ID.ToString()}");
            }
        }
    }
}