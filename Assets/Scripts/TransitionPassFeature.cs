using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TransitionPassFeature : ScriptableRendererFeature
{
    class TransitionRenderPass : ScriptableRenderPass
    {
        public TransitionRenderPass(Material material)
        {
            if (material == null)
            {
                return;
            }

            this.material = material;
            afterPostProcessTexture.Init("_AfterPostProcessTexture");
            _tempRenderTargetHandle.Init("_TempRT");
        }
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!renderingData.cameraData.postProcessEnabled)
            {
                return;
            }

            if (renderingData.cameraData.cameraType != CameraType.Game)
            {
                return;
            }

            var source = afterPostProcessTexture.Identifier();
            var cmd = CommandBufferPool.Get(RenderPassName);
            cmd.Clear();
            cmd.SetGlobalTexture(mainTexPropertyId, source);
            
            var tempTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            tempTargetDescriptor.depthBufferBits = 0;
            cmd.GetTemporaryRT(_tempRenderTargetHandle.id, tempTargetDescriptor);

            Blit(cmd, source, _tempRenderTargetHandle.Identifier(), material);
            Blit(cmd, _tempRenderTargetHandle.Identifier(), source);
            
            cmd.ReleaseTemporaryRT(_tempRenderTargetHandle.id);
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
        
        private const string RenderPassName = nameof(TransitionRenderPass);
        private readonly int mainTexPropertyId = Shader.PropertyToID("_MainTex");
        
        private RenderTargetHandle _tempRenderTargetHandle;
        
        private RenderTargetHandle afterPostProcessTexture;
        
        
        RenderTargetIdentifier renderTargetId;// = new RenderTargetIdentifier(renderTexture);

        private Material material;
    }
    
    [SerializeField] private Material material;

    TransitionRenderPass renderPass;

    /// <inheritdoc/>
    public override void Create()
    {
        renderPass = new TransitionRenderPass(material);
        
        // Configures where the render pass should be injected.
        renderPass.renderPassEvent = RenderPassEvent.AfterRendering;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(renderPass);
    }
}


