using Aether;
using UnityEngine;

public class ActiveOnClient : MonoBehaviour
{
    private void Awake()
    {
        if (NetworkApplication.IsClientOnly)
        {
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(true);
            }
        }
    }
}
