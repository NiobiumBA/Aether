#if UNITY_EDITOR
using UnityEditor;

namespace Aether.Editor
{
    [CustomEditor(typeof(NetworkIdentity))]
    public class NetworkIdentityViewer : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            NetworkIdentity identity = (NetworkIdentity)target;

            EditorGUILayout.LabelField("InitState", identity.InitState.ToString());

            string label = identity.NetId.ToString();

            if (identity.InitState == NetworkIdentity.InitializationState.OnScene)
                EditorGUILayout.LabelField("SceneId", label);
            else if (identity.InitState == NetworkIdentity.InitializationState.AsPrefab)
                EditorGUILayout.LabelField("AssetId", label);
        }
    }
}
#endif
