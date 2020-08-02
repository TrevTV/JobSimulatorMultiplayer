using MelonLoader;
using UnityEngine;
using Steamworks;
using JobSimulatorMultiplayer.Core;
using JobSimulatorMultiplayer.Representations;
using UnhollowerRuntimeLib;
using JobSimulatorMultiplayer.MonoBehaviours;
using System.Collections;
using static UnityEngine.Object;
using OwlchemyVR;

namespace JobSimulatorMultiplayer
{
    public static class BuildInfo
    {
        public const string Name = "JobSimulatorMultiplayer_ALPHA_TESTING"; // Name of the Mod.  (MUST BE SET)
        public const string Author = "L4rs/TrevTV/Someone Somewhere"; // Author of the Mod.  (Set as null if none)
        public const string Company = null; // Company that made the Mod.  (Set as null if none)
        public const string Version = "0.0.1"; // Version of the Mod.  (MUST BE SET)
        public const string DownloadLink = null; // Download Link for the Mod.  (Set as null if none)
    }

    public class JobSimulatorMultiplayer : MelonMod
    {
        public const int MAX_PLAYERS = 16;
        public const byte PROTOCOL_VERSION = 30;

        public Client client;
        public Server server;

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

            MelonModLogger.LogWarning("ALPHA TESTING BUILD");
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

                if (Input.GetKeyDown(KeyCode.P))
                {
                    new PlayerRep("Dummy", SteamClient.SteamId);
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
            //MelonCoroutines.Start(PhysicSyncLoad());
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
            ObjectIDManager.objects.Clear();

            yield return new WaitForSeconds(3);

            MelonModLogger.Log("Getting and adding all Rigidbodies");
            var rbs = FindObjectsOfType<Rigidbody>();
            foreach (var rb in rbs)
            {
                if (rb.gameObject.transform.root.gameObject.name.Contains("HMD") || rb.isKinematic == true)
                    continue;

                rb.isKinematic = true;

                var sso = rb.gameObject.AddComponent<ServerSyncedObject>();
                var idHolder = rb.gameObject.AddComponent<IDHolder>();

                idHolder.ID = ObjectIDManager.GenerateID(sso);
                ObjectIDManager.AddObject(idHolder.ID, sso);
                MelonModLogger.Log($"added {rb.gameObject.name} with generated id {idHolder.ID.ToString()}");
            }
        }
    }
}
