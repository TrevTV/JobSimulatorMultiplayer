using MelonLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.Object;
using JobSimulatorMultiplayer.Structs;
using JobSimulatorMultiplayer.Networking;

namespace JobSimulatorMultiplayer
{
    public class Util
    {
        public static string GetFullNamePath(GameObject obj)
        {
            if (obj.transform.parent == null)
                return obj.name;

            return GetFullNamePath(obj.transform.parent.gameObject) + "/" + obj.name + "|" + obj.transform.GetSiblingIndex();
        }

        public static GameObject GetObjectFromFullPath(string path)
        {
            string[] pathComponents = path.Split('/');

            // First object won't have a sibling index -
            // better hope that the game doesn't have identically named roots!
            // TODO: Could potentially work around this by
            // manually assigning IDs to each root upon scene load
            // but bleh

            GameObject rootObj;
            rootObj = GameObject.Find(pathComponents[0]);
            if (rootObj == null)
                return null;

            if (rootObj.transform.parent != null)
            {
                throw new Exception("Tried to find a root object but didn't get a root object. Try again, dumbass.");
            }

            GameObject currentObj = rootObj;

            for (int i = 1; i < pathComponents.Length; i++)
            {
                string[] splitComponent = pathComponents[i].Split('|');

                int siblingIdx = int.Parse(splitComponent[1]);
                string name = splitComponent[0];

                GameObject newObj = rootObj.transform.GetChild(siblingIdx).gameObject;

                if (newObj.name != name)
                {
                    throw new Exception("Name didn't match expected name at sibling index. Try again, dumbass.");
                }

                currentObj = newObj;
            }

            return currentObj;
        }
    }
}
