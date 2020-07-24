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
        public static Dictionary<int, ServerSyncedObject> objects = new Dictionary<int, ServerSyncedObject>();
        private static int lastID = 0;

        public static void Reset()
        {
            objects.Clear();
        }

        public static ServerSyncedObject GetObject(int wi)
        {
            return objects[wi];
        }

        public static void AddObject(int wi, ServerSyncedObject obj)
        {
            if (objects.ContainsKey(wi))
                return;

            objects.Add(wi, obj);
        }

        public static int GenerateID(ServerSyncedObject syncedObject)
        {
            var id = syncedObject.gameObject.name.GetHashCode();

            do
            {
                id = (syncedObject.gameObject.name += "1").GetHashCode();
            }
            while (objects.ContainsKey(id));

            objects.Add(id, syncedObject);
            return id;
        }
    }
}
