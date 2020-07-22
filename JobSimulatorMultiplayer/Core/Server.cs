using Steamworks;
using Steamworks.Data;
using MelonLoader;
using System;
using System.Collections.Generic;
using JobSimulatorMultiplayer.Representations;
using JobSimulatorMultiplayer.Networking;
using UnityEngine.SceneManagement;

namespace JobSimulatorMultiplayer.Core
{
    public class Server
    {
        private readonly Dictionary<byte, PlayerRep> playerObjects = new Dictionary<byte, PlayerRep>(JobSimulatorMultiplayer.MAX_PLAYERS);
        private readonly Dictionary<byte, string> playerNames = new Dictionary<byte, string>(JobSimulatorMultiplayer.MAX_PLAYERS);
        private readonly List<ulong> players = new List<ulong>();
        private readonly Dictionary<SteamId, byte> smallPlayerIds = new Dictionary<SteamId, byte>(JobSimulatorMultiplayer.MAX_PLAYERS);
        private readonly Dictionary<byte, SteamId> largePlayerIds = new Dictionary<byte, SteamId>(JobSimulatorMultiplayer.MAX_PLAYERS);
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
                        default:
                            MelonModLogger.Log("Unknown message type: " + type.ToString());
                            break;
                    }

                }
            }
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
    }
}