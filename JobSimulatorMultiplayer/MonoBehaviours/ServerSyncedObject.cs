using System;
using UnityEngine;

namespace JobSimulatorMultiplayer.MonoBehaviours
{
    public class ServerSyncedObject : MonoBehaviour
    {
        public ServerSyncedObject(IntPtr ptr) : base(ptr) { }
        public Vector3 lastSyncedPos = Vector3.zero;
        public Quaternion lastSyncedRotation = Quaternion.identity;
        private IDHolder _idHolder;
        public IDHolder IDHolder
        {
            get
            {
                if (!_idHolder)
                    _idHolder = GetComponent<IDHolder>();
                return _idHolder;
            }
        }

        public bool NeedsSync()
        {
            return (transform.position - lastSyncedPos).sqrMagnitude > 0.05f || Quaternion.Angle(transform.rotation, lastSyncedRotation) > 2.0f;
        }
    }
}
