using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using Steamworks;
using JobSimulatorMultiplayer.Core;
using JobSimulatorMultiplayer.Representations;
using UnhollowerRuntimeLib;
using JobSimulatorMultiplayer.MonoBehaviours;
using Discord;
using Harmony;
using System.Collections;
using PSC;
using System;
using static UnityEngine.Object;
using OwlchemyVR;

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
            ModPrefs.RegisterPrefBool("MPMod", "ForceLargePlayspace", true);

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
                }

                // If the user is not hosting, start their server
                if (Input.GetKeyDown(KeyCode.S))
                {
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
            // MelonCoroutines.Start(PhysicSyncLoad());
        }

        public override void OnApplicationQuit()
        {
            if (client.isConnected)
                client.Disconnect();

            if (server.IsRunning)
                server.StopServer();
        }

        public IEnumerator PhysicSyncLoad()
        {
            yield return new WaitForSeconds(5);

            ObjectIDManager.objects.Clear();

            MelonModLogger.Log("Getting and adding all Rigidbodies");
            var wids = FindObjectsOfType<WorldItem>();
            foreach (var wid in wids)
            {
                if (wid.gameObject.transform.root.gameObject.name.Contains("HMD") || ObjectIDManager.objects.ContainsKey(wid))
                    continue;
                
                var sso = wid.gameObject.AddComponent<ServerSyncedObject>();
                var idHolder = wid.gameObject.AddComponent<IDHolder>();
                //TODO: ID Generation, Syned across Host + Clients
                idHolder.ID = 1;
                ObjectIDManager.AddObject(wid, sso);
                MelonModLogger.Log($"added {wid.gameObject.name}");
            }
        }
    }
}
