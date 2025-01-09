using UnityEngine;

namespace Aether
{
    public abstract class SingletonBehaviour<TSelf> : MonoBehaviour
        where TSelf : SingletonBehaviour<TSelf>
    {
        protected virtual void Start()
        {
            TSelf[] components = FindObjectsByType<TSelf>(FindObjectsSortMode.None);

            if (components.Length > 1)
                ThrowHelper.TooManyScriptsOnScene(nameof(TSelf));
        }
    }
}
