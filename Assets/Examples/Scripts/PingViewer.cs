using Aether;
using UnityEngine;
using UnityEngine.UI;

namespace Examples
{
    public class PingViewer : MonoBehaviour
    {
        [SerializeField] private Text m_text;

        private void Update()
        {
            m_text.text = $"Ping: {Mathf.RoundToInt(NetworkTime.Ping * 1000)}";
        }
    }
}
