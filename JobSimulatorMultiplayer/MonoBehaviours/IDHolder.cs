using System;
using UnityEngine;

namespace JobSimulatorMultiplayer.MonoBehaviours
{
    public class IDHolder : MonoBehaviour
    {
        public IDHolder(IntPtr ptr) : base(ptr) { }
        public int ID;
    }
}
