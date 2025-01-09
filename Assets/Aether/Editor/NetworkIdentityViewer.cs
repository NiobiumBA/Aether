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

            if (identity.InitState == NetworkIdentity.InitializationState.OnScene)
                EditorGUILayout.LabelField("SceneId", identity.SceneId.ToString());
            else if (identity.InitState == NetworkIdentity.InitializationState.AsPrefab)
                EditorGUILayout.LabelField("AssetId", identity.AssetId.ToString());
        }
    }
}
#endif
