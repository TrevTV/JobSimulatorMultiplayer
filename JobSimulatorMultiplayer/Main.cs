﻿using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using Steamworks;
using JobSimulatorMultiplayer.Core;
using JobSimulatorMultiplayer.Representations;
using UnhollowerRuntimeLib;
using JobSimulatorMultiplayer.MonoBehaviours;
using Discord;

namespace JobSimulatorMultiplayer
{
    public static class BuildInfo
    {
        public const string Name = "JobSimulatorMultiplayer"; // Name of the Mod.  (MUST BE SET)
        public const string Author = "L4rs/TrevTV/Someone Somewhere"; // Author of the Mod.  (Set as null if none)
        public const string Company = null; // Company that made the Mod.  (Set as null if none)
        public const string Version = "0.0.1"; // Version of the Mod.  (MUST BE SET)
        public const string DownloadLink = null; // Download Link for the Mod.  (Set as null if none)
    }

    public class JobSimulatorMultiplayer : MelonMod
    {
        // note of reference branch
        // https://github.com/someonesomewheredev/boneworks-mp/tree/accessories

        public const int MAX_PLAYERS = 16;
        public const byte PROTOCOL_VERSION = 30;

        private Client client;
        private Server server;

        public override void OnApplicationStart()
        {
            // Setup MonoBehaviors
            ClassInjector.RegisterTypeInIl2Cpp<ServerSyncedObject>();
            ClassInjector.RegisterTypeInIl2Cpp<IDHolder>();

            // Register Prefs
            ModPrefs.RegisterCategory("MPMod", "Multiplayer Settings");
            ModPrefs.RegisterPrefString("MPMod", "HostSteamID", "0");

            // Start Server Stuff
            SteamClient.Init(448280);

            MelonModLogger.Log($"Multiplayer initialising with protocol version {PROTOCOL_VERSION}.");

            SteamNetworking.AllowP2PPacketRelay(true);

            client = new Client();
            server = new Server();
            PlayerRep.LoadPlayer();

            // Setup Discord Presence
            RichPresence.Initialise(736050983335100436);
            client.SetupRP();

            MelonModLogger.Log("MPMod Loaded");
        }

        public override void OnUpdate()
        {
            RichPresence.Update();

            if (!client.isConnected && !server.IsRunning)
            {
                // If the user is not connected, start their client and attempt a connection
                if (Input.GetKeyDown(KeyCode.C))
                {
                    client.Connect(ModPrefs.GetString("MPMod", "HostSteamID"));
                    SteamFriends.SetRichPresence("steam_display", "Playing multiplayer on " + SceneManager.GetActiveScene().name);
                    SteamFriends.SetRichPresence("connect", "--jobsimulator-multiplayer-id-connect " + client.ServerId);
                    SteamFriends.SetRichPresence("steam_player_group", client.ServerId.ToString());
                }

                // If the user is not hosting, start their server
                if (Input.GetKeyDown(KeyCode.S))
                {
                    SteamFriends.SetRichPresence("steam_display", "Hosting multiplayer on " + SceneManager.GetActiveScene().name);
                    SteamFriends.SetRichPresence("connect", "--jobsimulator-multiplayer-id-connect " + SteamClient.SteamId);
                    SteamFriends.SetRichPresence("steam_player_group", SteamClient.SteamId.ToString());
                    server.StartServer();
                }
            }
            else
            {
                // If the user is connected, disconnect them
                if (Input.GetKeyDown(KeyCode.C))
                    client.Disconnect();

                // If the user is hosting, stop their server
                if (Input.GetKeyDown(KeyCode.S))
                {
                    MelonModLogger.Log("Stopping server...");
                    server.StopServer();
                }
            }
        }

        public override void OnFixedUpdate()
        {
            if (client.isConnected)
                client.Update();

            if (server.IsRunning)
                server.Update();
        }

        public override void OnLevelWasInitialized(int level)
        {
            #region Physic Sync
            ObjectIDManager.objects.Clear();

            var rbs = GameObject.FindObjectsOfType<Rigidbody>();
            foreach (var rb in rbs)
            {
                var sso = rb.gameObject.AddComponent<ServerSyncedObject>();
                try { ObjectIDManager.AddObject((byte)rb.GetInstanceID(), sso); } catch { }
            }
            #endregion
        }

        public override void OnApplicationQuit()
        {
            if (client.isConnected)
                client.Disconnect();

            if (server.IsRunning)
                server.StopServer();
        }
    }
}
