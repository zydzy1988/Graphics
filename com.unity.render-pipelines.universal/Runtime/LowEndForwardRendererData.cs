#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif
using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering.Universal
{
    [Serializable, ReloadGroup, ExcludeFromPreset]
    [MovedFrom("UnityEngine.Rendering.LWRP")]
    public class LowEndForwardRendererData : ScriptableRendererData
    {
#if UNITY_EDITOR
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812")]
        internal class CreateLowEndForwardRendererAsset : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var instance = CreateInstance<LowEndForwardRendererData>();
                AssetDatabase.CreateAsset(instance, pathName);
                ResourceReloader.ReloadAllNullIn(instance, UniversalRenderPipelineAsset.packagePath);
                Selection.activeObject = instance;
            }
        }

        [MenuItem("Assets/Create/Rendering/Universal Render Pipeline/LowEndForward Renderer", priority = CoreUtils.assetCreateMenuPriority2)]
        static void CreateLowEndForwardRendererData()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateLowEndForwardRendererAsset>(), "CustomLowEndForwardRendererData.asset", null, null);
        }
#endif

        [SerializeField] LayerMask m_OpaqueLayerMask = -1;
        [SerializeField] LayerMask m_TransparentLayerMask = -1;
        [SerializeField] StencilStateData m_DefaultStencilState = new StencilStateData();

        protected override ScriptableRenderer Create()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                ResourceReloader.TryReloadAllNullIn(this, UniversalRenderPipelineAsset.packagePath);
            }
#endif
            return new LowEndForwardRenderer(this);
        }

        /// <summary>
        /// Use this to configure how to filter opaque objects.
        /// </summary>
        public LayerMask opaqueLayerMask
        {
            get => m_OpaqueLayerMask;
            set
            {
                SetDirty();
                m_OpaqueLayerMask = value;
            }
        }

        /// <summary>
        /// Use this to configure how to filter transparent objects.
        /// </summary>
        public LayerMask transparentLayerMask
        {
            get => m_TransparentLayerMask;
            set
            {
                SetDirty();
                m_TransparentLayerMask = value;
            }
        }

        public StencilStateData defaultStencilState
        {
            get => m_DefaultStencilState;
            set
            {
                SetDirty();
                m_DefaultStencilState = value;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

#if UNITY_EDITOR
            ResourceReloader.TryReloadAllNullIn(this, UniversalRenderPipelineAsset.packagePath);
#endif
        }
    }
}
