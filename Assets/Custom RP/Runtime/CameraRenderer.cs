using UnityEngine.Rendering;
using UnityEngine;

public partial class CameraRenderer
{
    ScriptableRenderContext context;

    Camera camera;

    const string bufferName = "Render Camera";

    CommandBuffer buffer = new CommandBuffer { name = bufferName };

    CullingResults cullingResults;

    //we only support unlit shaders
    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");

    static ShaderTagId litShaderTagId = new ShaderTagId("CustomLit");

    Lighting lighting = new Lighting();

    public void Render(ScriptableRenderContext context, Camera camera, bool useDynamicBatching, bool useGPUInstancing, ShadowSettings shadowSettings)
    {
        this.context = context;
        this.camera = camera;

        PrepareBuffer();
        PrepareForSceneWindow();

        if (!Cull(shadowSettings.maxDistance))
            return;

        buffer.BeginSample(SampleName);
        ExecuteBuffer();
        
        //set up the lighting, shadows first
        lighting.Setup(context, cullingResults, shadowSettings);
        buffer.EndSample(SampleName);
        
        //
        Setup();

        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
        //draw all unsupported shaders
        DrawUnsupportedShaders();
        //draw lines of camera
        DrawGizmos();

        lighting.Cleanup();

        Submit();
    }

    void Setup()
    {
        //apply camera's properties to the context: view or projection matrix or others
        context.SetupCameraProperties(camera);
        //combine results of both cameras  
        CameraClearFlags flags = camera.clearFlags;
        //clear the earlier content of the RT to guarantee proper rendering
        buffer.ClearRenderTarget(flags <= CameraClearFlags.Depth, 
                                 flags == CameraClearFlags.Color, 
                                 flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
        buffer.BeginSample(SampleName);
        ExecuteBuffer(); 
    }

    //enabel dynamic batching or gpu instancing
    void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
    {
        //先画不透明物体
        //pass the camera to SortingSettings constructor to determine whether orthographic or distance-based sorting applies
        //setting the criteria property of the sorting settings
        var sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonOpaque };
        //unlitShaderTagId as the first argument
        var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings) { enableDynamicBatching = useDynamicBatching, enableInstancing = useGPUInstancing, perObjectData = PerObjectData.Lightmaps | PerObjectData.LightProbe | PerObjectData.LightProbeProxyVolume};
        drawingSettings.SetShaderPassName(1, litShaderTagId);
        //render queue range indicates which render queues are allowed
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

        //再画天空盒
        context.DrawSkybox(camera);

        //再画透明物体
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;

        //render visible things with culling results as an argument
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }

    void Submit()
    {
        buffer.EndSample(SampleName);
        ExecuteBuffer();
        context.Submit();
    }

    void ExecuteBuffer()
    {
        //copy commands but doesn't clear it
        context.ExecuteCommandBuffer(buffer);
        //clear 
        buffer.Clear();
    }

    //returns whether the parameters could be successfully retrieved on the camera
    bool Cull(float maxShadowDistance)
    {;
        if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
        {
            //shadow distance is set via the culling parameters
            p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
            //actual Culling is done by invoking Cull on the context, produces a CullingResults struct
            cullingResults = context.Cull(ref p);
            return true;
        }
        return false;
    }

}
