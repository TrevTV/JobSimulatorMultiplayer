using Steamworks;
using MelonLoader;
using UnityEngine;
using static UnityEngine.Object;
using JobSimulatorMultiplayer.Structs;
using System.IO;

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
            bundle = AssetBundle.LoadFromFile(Directory.GetCurrentDirectory() + "\\playermodels.mp");
            if (bundle == null)
                MelonModLogger.LogError("Failed to load the asset bundle");

            GameObject prefab = bundle.LoadAsset("Assets/models.prefab").Cast<GameObject>();
            if (prefab == null)
                MelonModLogger.LogError("Failed to load models from the asset bundle???");
        }

        // Constructor
        public PlayerRep(string name, SteamId steamId)
        {
            this.steamId = steamId;

            // Create this player's "Ford" to represent them, known as their rep
            GameObject models = Instantiate(bundle.LoadAsset("Assets/Player.prefab").Cast<GameObject>());

            // Grab these body parts from the rigTransforms
            head = models.transform.Find("head").gameObject;
            handL = models.transform.Find("leftHand").gameObject;
            handR = models.transform.Find("rightHand").gameObject;

            // MelonCoroutines.Start(AsyncAvatarRoutine(steamId));

            // Change the shader to the one that's already used in the game
            // Without this, the player model will only show in one eye
            foreach (SkinnedMeshRenderer smr in models.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                foreach (Material m in smr.sharedMaterials)
                {
                    m.shader = Shader.Find("Standard");
                }
            }
            foreach (MeshRenderer smr in models.GetComponentsInChildren<MeshRenderer>())
            {
                foreach (Material m in smr.sharedMaterials)
                {
                    m.shader = Shader.Find("Standard");
                }
            }

            this.models = models;
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