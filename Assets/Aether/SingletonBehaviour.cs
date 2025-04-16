using UnityEngine;

namespace Aether
{
    public abstract class SingletonBehaviour<TSelf> : MonoBehaviour
        where TSelf : SingletonBehaviour<TSelf>
    {
        private static TSelf s_instance;

        private bool m_destroyed = false;

        protected bool ShouldBeDestroyed => m_destroyed;

        protected virtual void Start()
        {
            if (s_instance == null)
            {
                s_instance = this as TSelf;
            }
            else
            {
                Destroy(this);
                m_destroyed = true;
            }
        }
    }
}
