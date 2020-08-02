using Steamworks;
using MelonLoader;
using UnityEngine;
using static UnityEngine.Object;
using Steamworks.Data;
using System.Threading.Tasks;
using System.Collections;
using System.Runtime.InteropServices;
using System;
using TMPro;
using OwlchemyVR;

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
            head = Instantiate(bundle.LoadAsset("Assets/head.prefab").Cast<GameObject>());
            handL = Instantiate(bundle.LoadAsset("Assets/leftHand.prefab").Cast<GameObject>());
            handR = Instantiate(bundle.LoadAsset("Assets/rightHand.prefab").Cast<GameObject>());

            //MelonCoroutines.Start(AsyncAvatarRoutine(steamId));

            // Change the shader to the one that's already used in the game
            // Without this, the player model will essentially cause headaches from looking at it
            foreach (MeshRenderer smr in head.GetComponentsInChildren<MeshRenderer>())
                foreach (Material m in smr.sharedMaterials)
                {
                    m.SetColor("_Color", UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f));
                    try { m.shader = Shader.Find("Standard"); } catch { }

                }

            foreach (MeshRenderer smr in handL.GetComponentsInChildren<MeshRenderer>())
                foreach (Material m in smr.sharedMaterials)
                    try { m.shader = Shader.Find("Standard"); } catch { }

            foreach (MeshRenderer smr in handR.GetComponentsInChildren<MeshRenderer>())
                foreach (Material m in smr.sharedMaterials)
                    try { m.shader = Shader.Find("Standard"); } catch { }
        }

        // Destroys the GameObjects stored inside this class, preparing this instance for deletion
        public void Destroy()
        {
            UnityEngine.Object.Destroy(models);
            UnityEngine.Object.Destroy(head);
            UnityEngine.Object.Destroy(handL);
            UnityEngine.Object.Destroy(handR);
        }

        private void AddBrain()
        {
            GameObject brain = Instantiate(Resources.FindObjectsOfTypeAll<PrinterController>()[0].lifelessHeadPrefab);

            brain.transform.parent = head.transform;
            brain.transform.position = head.transform.position - new Vector3(0f, 0f, 0.11f);
            brain.transform.rotation = head.transform.rotation;
            GameObject.Destroy(brain.GetComponent<Rigidbody>());
            GameObject.Destroy(brain.GetComponent<GrabbableItem>());
        }

        /*
        private IEnumerator AsyncAvatarRoutine(SteamId id)
        {
            Task<Image?> imageTask = SteamFriends.GetLargeAvatarAsync(id);
            while (!imageTask.IsCompleted)
            {
                // WaitForEndOfFrame is broken in MelonLoader, so use WaitForSeconds
                yield return new WaitForSeconds(0.011f);
            }

            if (imageTask.Result.HasValue)
            {
                GameObject avatar = GameObject.CreatePrimitive(PrimitiveType.Quad);
                UnityEngine.Object.Destroy(avatar.GetComponent<Collider>());
                var avatarMr = avatar.GetComponent<MeshRenderer>();
                var avatarMat = avatarMr.material;
                avatarMat.shader = Shader.Find("Unlit/Texture");

                var avatarIcon = imageTask.Result.Value;

                Texture2D returnTexture = new Texture2D((int)avatarIcon.Width, (int)avatarIcon.Height, TextureFormat.RGBA32, false, true);
                GCHandle pinnedArray = GCHandle.Alloc(avatarIcon.Data, GCHandleType.Pinned);
                IntPtr pointer = pinnedArray.AddrOfPinnedObject();
                returnTexture.LoadRawTextureData(pointer, avatarIcon.Data.Length);
                returnTexture.Apply();
                pinnedArray.Free();

                avatarMat.mainTexture = returnTexture;

                avatar.transform.SetParent(head.transform);
                avatar.transform.localScale = new Vector3(0.25f, -0.25f, 0.25f);
                avatar.transform.localPosition = new Vector3(0.0f, 0.1f, 0.0f);
            }
        }
        */

    }
}