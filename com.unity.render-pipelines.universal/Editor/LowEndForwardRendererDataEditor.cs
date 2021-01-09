using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(LowEndForwardRendererData), true)]
    [MovedFrom("UnityEditor.Rendering.LWRP")] public class LowEndForwardRendererDataEditor : ScriptableRendererDataEditor
    {
        private static class Styles
        {
            public static readonly GUIContent RendererTitle = new GUIContent("Forward Renderer", "Custom Forward Renderer for Universal RP.");
            public static readonly GUIContent FilteringLabel = new GUIContent("Filtering", "Controls filter rendering settings for this renderer.");
            public static readonly GUIContent OpaqueMask = new GUIContent("Opaque Layer Mask", "Controls which opaque layers this renderer draws.");
            public static readonly GUIContent TransparentMask = new GUIContent("Transparent Layer Mask", "Controls which transparent layers this renderer draws.");
            public static readonly GUIContent defaultStencilStateLabel = EditorGUIUtility.TrTextContent("Default Stencil State", "Configure stencil state for the opaque and transparent render passes.");
        }

        SerializedProperty m_OpaqueLayerMask;
        SerializedProperty m_TransparentLayerMask;
        SerializedProperty m_DefaultStencilState;

        private void OnEnable()
        {
            m_OpaqueLayerMask = serializedObject.FindProperty("m_OpaqueLayerMask");
            m_TransparentLayerMask = serializedObject.FindProperty("m_TransparentLayerMask");
            m_DefaultStencilState = serializedObject.FindProperty("m_DefaultStencilState");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Styles.RendererTitle, EditorStyles.boldLabel); // Title
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(Styles.FilteringLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_OpaqueLayerMask, Styles.OpaqueMask);
            EditorGUILayout.PropertyField(m_TransparentLayerMask, Styles.TransparentMask);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Overrides", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_DefaultStencilState, Styles.defaultStencilStateLabel, true);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();

            base.OnInspectorGUI(); // Draw the base UI, contains ScriptableRenderFeatures list
        }
    }
}
