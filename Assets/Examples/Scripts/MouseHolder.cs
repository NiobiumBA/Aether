using Aether;
using UnityEngine;

public class MouseHolder : MonoBehaviour
{
    private void OnMouseDrag()
    {
        if (NetworkApplication.IsServer)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            transform.position = ray.GetPoint(5);
        }
    }
}
