using Il2CppSystem.Security.Cryptography;
using JobSimulatorMultiplayer.MonoBehaviours;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace JobSimulatorMultiplayer.Core
{
    public static class ObjectIDManager
    {
        public static Dictionary<byte, ServerSyncedObject> objects = new Dictionary<byte, ServerSyncedObject>();

        public static void Reset()
        {
            objects.Clear();
        }

        public static ServerSyncedObject GetObject(byte id)
        {
            return objects[id];
        }

        public static void AddObject(byte id, ServerSyncedObject obj)
        {
            objects.Add(id, obj);
        }
    }
}
