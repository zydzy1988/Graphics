using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// LowEnd forward renderer for Universal RP.
    /// </summary>
    public sealed class LowEndForwardRenderer : ScriptableRenderer
    {
        DrawObjectsPass m_RenderOpaqueForwardPass;
        DrawObjectsPass m_RenderTransparentForwardPass;
        // InvokeOnRenderObjectCallbackPass m_OnRenderObjectCallbackPass;

        RenderTargetHandle m_ActiveCameraColorAttachment;
        RenderTargetHandle m_ActiveCameraDepthAttachment;
        RenderTargetHandle m_CameraColorAttachment;
        RenderTargetHandle m_CameraDepthAttachment;

        ForwardLights m_ForwardLights;
        StencilState m_DefaultStencilState;

        public LowEndForwardRenderer(LowEndForwardRendererData data) : base(data)
        {
            StencilStateData stencilData = data.defaultStencilState;
            m_DefaultStencilState = StencilState.defaultValue;
            m_DefaultStencilState.enabled = stencilData.overrideStencilState;
            m_DefaultStencilState.SetCompareFunction(stencilData.stencilCompareFunction);
            m_DefaultStencilState.SetPassOperation(stencilData.passOperation);
            m_DefaultStencilState.SetFailOperation(stencilData.failOperation);
            m_DefaultStencilState.SetZFailOperation(stencilData.zFailOperation);

            // Note: Since all custom render passes inject first and we have stable sort,
            // we inject the builtin passes in the before events.
            m_RenderOpaqueForwardPass = new DrawObjectsPass("Render Opaques", true, RenderPassEvent.BeforeRenderingOpaques, RenderQueueRange.opaque, data.opaqueLayerMask, m_DefaultStencilState, stencilData.stencilReference);
            m_RenderTransparentForwardPass = new DrawObjectsPass("Render Transparents", false, RenderPassEvent.BeforeRenderingTransparents, RenderQueueRange.transparent, data.transparentLayerMask, m_DefaultStencilState, stencilData.stencilReference);
            // m_OnRenderObjectCallbackPass = new InvokeOnRenderObjectCallbackPass(RenderPassEvent.BeforeRenderingPostProcessing);

            // RenderTexture format depends on camera and pipeline (HDR, non HDR, etc)
            // Samples (MSAA) depend on camera and pipeline
            m_CameraColorAttachment.Init("_CameraColorTexture");
            m_CameraDepthAttachment.Init("_CameraDepthAttachment");
            m_ForwardLights = new ForwardLights();

            supportedRenderingFeatures = new RenderingFeatures()
            {
                cameraStacking = true,
            };
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ref CameraData cameraData = ref renderingData.cameraData;
            Camera camera = cameraData.camera;

            // Set render state command buffer
            CommandBuffer setRenderStateCmd = CommandBufferPool.Get(k_SetCameraRenderStateTag);
            // Release command buffer
            CommandBuffer releaseCmd = CommandBufferPool.Get(k_ReleaseResourcesTag);

            // Set Camera
            context.SetupCameraProperties(camera, false, 0);

#if UNITY_EDITOR
            float time = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
#else
            float time = Time.time;
#endif
            float deltaTime = Time.deltaTime;
            float smoothDeltaTime = Time.smoothDeltaTime;

            // Set Time
            SetShaderTimeValues(time, deltaTime, smoothDeltaTime, setRenderStateCmd);
            context.ExecuteCommandBuffer(setRenderStateCmd);
            setRenderStateCmd.Clear();

            // Special path for UI Camera
            if (renderingData.cameraData.renderType == CameraRenderType.UI)
            {
                // Draw UI
                ExecuteRenderPass(context, m_RenderTransparentForwardPass, ref renderingData, 0);

                // Release
                m_RenderTransparentForwardPass.FrameCleanup(releaseCmd);
                context.ExecuteCommandBuffer(releaseCmd);

                CommandBufferPool.Release(releaseCmd);
                CommandBufferPool.Release(setRenderStateCmd);
                return;
            }

            m_ForwardLights.LowEndSetup(context, ref renderingData);
            if (cameraData.renderType == CameraRenderType.Overlay)
            {
                setRenderStateCmd.SetViewProjectionMatrices(cameraData.viewMatrix, cameraData.projectionMatrix);
            }

            // Draw objects
            ExecuteRenderPass(context, m_RenderOpaqueForwardPass, ref renderingData, 0);
            ExecuteRenderPass(context, m_RenderTransparentForwardPass, ref renderingData, 0);

            // Invoke OnRenderObjectCallback
            // ExecuteRenderPass(context, m_OnRenderObjectCallbackPass, ref renderingData, 0);

            // Draw Gizmos...
            DrawGizmos(context, camera, GizmoSubset.PreImageEffects);
            DrawGizmos(context, camera, GizmoSubset.PostImageEffects);

            // Release
            m_RenderOpaqueForwardPass.FrameCleanup(releaseCmd);
            m_RenderTransparentForwardPass.FrameCleanup(releaseCmd);
            context.ExecuteCommandBuffer(releaseCmd);

            // Happens when rendering the last camera in the camera stack.
            if (renderingData.resolveFinalTarget)
            {
                m_RenderOpaqueForwardPass.OnFinishCameraStackRendering(releaseCmd);
                m_RenderTransparentForwardPass.OnFinishCameraStackRendering(releaseCmd);
                FinishRendering(releaseCmd);
            }

            CommandBufferPool.Release(releaseCmd);
            CommandBufferPool.Release(setRenderStateCmd);
            return;
        }

        /// <inheritdoc />
        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ref CameraData cameraData = ref renderingData.cameraData;

            // Special path for UI Camera
            if (renderingData.cameraData.renderType == CameraRenderType.UI)
            {
                var camTargetId = RenderTargetHandle.CameraTarget.Identifier();
                ConfigureCameraTarget(camTargetId, camTargetId);
                return;
            }

            // Configure all settings require to start a new camera stack (base camera only)
            if (cameraData.renderType == CameraRenderType.Base)
            {
                m_ActiveCameraColorAttachment = RenderTargetHandle.CameraTarget;
                m_ActiveCameraDepthAttachment = RenderTargetHandle.CameraTarget;
            }
            else
            {
                m_ActiveCameraColorAttachment = m_CameraColorAttachment;
                m_ActiveCameraDepthAttachment = m_CameraDepthAttachment;
            }
            ConfigureCameraTarget(m_ActiveCameraColorAttachment.Identifier(), m_ActiveCameraDepthAttachment.Identifier());
        }

        /// <inheritdoc />
        public override void SetupCullingParameters(ref ScriptableCullingParameters cullingParameters,
            ref CameraData cameraData)
        {
            cullingParameters.shadowDistance = 0.0f;

            if (cameraData.renderType == CameraRenderType.UI)
            {
                cullingParameters.cullingOptions = CullingOptions.None;
                cullingParameters.maximumVisibleLights = 0;
                return;
            }
        }
    }
}
