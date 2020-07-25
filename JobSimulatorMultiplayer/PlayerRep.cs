using Steamworks;
using MelonLoader;
using UnityEngine;
using static UnityEngine.Object;

namespace JobSimulatorMultiplayer.Representations
{
    public class PlayerRep
    {
        public GameObject models;
        public GameObject head;
        public GameObject handL;
        public GameObject handR;
        public SteamId steamId;

        private static AssetBundle bundle;

        public static void LoadPlayer()
        {
            bundle = AssetBundle.LoadFromFile("playermodels.mp");
            if (bundle == null)
                MelonModLogger.LogError("Failed to load the asset bundle");
        }

        // Constructor
        public PlayerRep(string name, SteamId steamId)
        {
            this.steamId = steamId;

            // Grab these body parts from the rigTransforms
            MelonModLogger.Log("1");
            head = Instantiate(bundle.LoadAsset("Assets/head.prefab").Cast<GameObject>());
            MelonModLogger.Log("2");
            handL = Instantiate(bundle.LoadAsset("Assets/leftHand.prefab").Cast<GameObject>());
            MelonModLogger.Log("3");
            handR = Instantiate(bundle.LoadAsset("Assets/rightHand.prefab").Cast<GameObject>());
            MelonModLogger.Log("4");

            // MelonCoroutines.Start(AsyncAvatarRoutine(steamId));

            // Change the shader to the one that's already used in the game
            // Without this, the player model will essentially cause headaches from looking at it
            foreach (MeshRenderer smr in head.GetComponentsInChildren<MeshRenderer>())
                foreach (Material m in smr.sharedMaterials)
                    try { m.shader = Shader.Find("Standard"); } catch { }
            MelonModLogger.Log("5");

            foreach (MeshRenderer smr in handL.GetComponentsInChildren<MeshRenderer>())
                foreach (Material m in smr.sharedMaterials)
                    try { m.shader = Shader.Find("Standard"); } catch { }
            MelonModLogger.Log("6");

            foreach (MeshRenderer smr in handR.GetComponentsInChildren<MeshRenderer>())
                foreach (Material m in smr.sharedMaterials)
                    try { m.shader = Shader.Find("Standard"); } catch { }
            MelonModLogger.Log("7");
        }

        // Destroys the GameObjects stored inside this class, preparing this instance for deletion
        public void Destroy()
        {
            UnityEngine.Object.Destroy(models);
            UnityEngine.Object.Destroy(head);
            UnityEngine.Object.Destroy(handL);
            UnityEngine.Object.Destroy(handR);
        }
    }
}