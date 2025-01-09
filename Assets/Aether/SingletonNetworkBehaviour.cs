using UnityEngine;

namespace Aether
{
    public abstract class SingletonNetworkBehaviour<TSelf> : NetworkBehaviour
        where TSelf : SingletonNetworkBehaviour<TSelf>
    {
        protected virtual void Start()
        {
            TSelf[] components = FindObjectsByType<TSelf>(FindObjectsSortMode.None);

            if (components.Length > 1)
                ThrowHelper.TooManyScriptsOnScene(nameof(TSelf));
        }
    }
}
