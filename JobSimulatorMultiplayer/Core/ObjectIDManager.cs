using Il2CppSystem.Security.Cryptography;
using JobSimulatorMultiplayer.MonoBehaviours;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using OwlchemyVR;

namespace JobSimulatorMultiplayer.Core
{
    public static class ObjectIDManager
    {
        public static Dictionary<WorldItem, ServerSyncedObject> objects = new Dictionary<WorldItem, ServerSyncedObject>();
        private static int lastID = 0;

        public static void Reset()
        {
            objects.Clear();
        }

        public static ServerSyncedObject GetObject(WorldItem wi)
        {
            return objects[wi];
        }

        public static void AddObject(WorldItem wi, ServerSyncedObject obj)
        {
            objects.Add(wi, obj);
        }
    }
}
