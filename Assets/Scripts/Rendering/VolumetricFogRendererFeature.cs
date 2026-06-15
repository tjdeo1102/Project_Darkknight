using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public sealed class VolumetricFogRendererFeature
    : ScriptableRendererFeature
{
    [SerializeField] private Material material;

    private VolumetricFogPass pass;

    public override void Create()
    {
        pass = new VolumetricFogPass();
        pass.renderPassEvent =
            RenderPassEvent.BeforeRenderingTransparents;
    }

    public override void AddRenderPasses(
        ScriptableRenderer renderer,
        ref RenderingData renderingData)
    {
        if (material == null)
            return;

        if (renderingData.cameraData.cameraType != CameraType.Game)
            return;

        if (renderingData.cameraData.camera.targetTexture != null)
            return;

        pass.Setup(material);
        renderer.EnqueuePass(pass);
    }

    private sealed class VolumetricFogPass
        : ScriptableRenderPass
    {
        private Material material;

        public void Setup(Material fogMaterial)
        {
            material = fogMaterial;
            requiresIntermediateTexture = true;
        }

        public override void RecordRenderGraph(
            RenderGraph renderGraph,
            ContextContainer frameData)
        {
            UniversalResourceData resources =
                frameData.Get<UniversalResourceData>();

            if (resources.isActiveTargetBackBuffer)
                return;

            TextureHandle source =
                resources.activeColorTexture;

            TextureDesc destinationDescriptor =
                renderGraph.GetTextureDesc(source);

            destinationDescriptor.name =
                "Volumetric Fog Color";

            destinationDescriptor.clearBuffer = false;
            destinationDescriptor.depthBufferBits =
                DepthBits.None;

            TextureHandle destination =
                renderGraph.CreateTexture(
                    destinationDescriptor);

            var parameters =
                new RenderGraphUtils.BlitMaterialParameters(
                    source,
                    destination,
                    material,
                    0);

            renderGraph.AddBlitPass(
                parameters,
                "Volumetric Fog");

            resources.cameraColor = destination;
        }
    }
}
