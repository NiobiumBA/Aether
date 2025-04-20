using Aether;
using UnityEngine;

namespace Examples
{
    public class TimeSyncExample : MonoBehaviour
    {
        private void Update()
        {
            float xCoord = Mathf.Sin(NetworkTime.TimeSinceServerStartup * 2) * 2;

            transform.position = new Vector3(xCoord, transform.position.y, transform.position.z);
        }
    }
}
