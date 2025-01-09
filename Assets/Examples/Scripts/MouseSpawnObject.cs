using Aether;
using UnityEngine;

public class MouseSpawnObject : MonoBehaviour
{
    [SerializeField] private GameObject m_spawnObject;

    private void Update()
    {
        if (NetworkApplication.IsServer && Input.GetMouseButtonDown(0))
        {
            Camera cam = Camera.main;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray) == false)
            {
                Vector3 pos = ray.GetPoint(5);

                NetworkGameObjectInteractions.Spawn(m_spawnObject, pos, Quaternion.identity, transform);
            }
        }
    }
}
