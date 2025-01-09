#if UNITY_EDITOR
using UnityEngine;

namespace Aether.Editor
{
    public class EditorLocalConnection : MonoBehaviour
    {
        private void Start()
        {
            if (NetworkApplication.IsClient == false && NetworkApplication.IsServer == false)
            {
                NetworkApplication.CreateClientDispatcher();
                NetworkApplication.CreateServerDispatcher();

                NetworkApplication.CreateLocalConnection();
            }
        }
    }
}
#endif
