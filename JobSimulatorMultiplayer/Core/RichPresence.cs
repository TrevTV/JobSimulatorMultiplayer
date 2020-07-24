using System;
using Discord;
using MelonLoader;
using UnityEngine.SceneManagement;

namespace JobSimulatorMultiplayer.Core
{
    public static class RichPresence
    {
        private static Discord.Discord discord;

        public static event Action<string> OnJoin;

        public static void Initialise(long clientId)
        {
            MelonModLogger.Log("Initalizing Discord RPC...");
            discord = new Discord.Discord(clientId, 0);
            discord.GetActivityManager().RegisterSteam(823500);
            discord.GetActivityManager().UpdateActivity(new Activity() { Details = "Idle", Assets = { LargeImage = "jobsim" } }, ActivityUpdateHandler);
            discord.GetActivityManager().OnActivityJoin += RichPresence_OnActivityJoin;
        }

        private static void RichPresence_OnActivityJoin(string secret)
        {
            OnJoin?.Invoke(secret);
        }

        private static void ActivityUpdateHandler(Result res)
        {
            MelonModLogger.Log("Got result " + res.ToString() + " when updating activity");
        }

        public static void Update()
        {
            discord.RunCallbacks();
        }

        public static void SetActivity(Activity act)
        {
            discord.GetActivityManager().UpdateActivity(act, ActivityUpdateHandler);
        }

        public static Tuple<string, string> GetCurrentLevelName()
        {
            switch (SceneManager.GetActiveScene().buildIndex)
            {
                case 0:
                    return Tuple.Create("Loading", "jobsim");
                case 1:
                    return Tuple.Create("Museum", "museum");
                case 2:
                    return Tuple.Create("Office", "office");
                case 3:
                    return Tuple.Create("Kitchen", "kitchen");
                case 4:
                    return Tuple.Create("Auto Shop", "autoshop");
                case 5:
                    return Tuple.Create("Convience Store", "store");
                default:
                    return Tuple.Create("Unknown", "jobsim");
            }
        }
    }
}
