using UnityEngine.Rendering;
using UnityEngine;
using UnityEditor;
using UnityEngine.Profiling;

partial class CameraRenderer
{

    partial void DrawUnsupportedShaders();

    //draw lines of camera
    partial void DrawGizmos();

    //draw ui button
    partial void PrepareForSceneWindow();

    //make buffer's name equal to camera's, ensure each camera gets its own scope
    partial void PrepareBuffer();

#if UNITY_EDITOR
    static Material errorMaterial;

    string SampleName { get; set; }


    //cover all unity's default shaders
    static ShaderTagId[] legacyShaderTagIds =
    {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("vertexLMRGBM"),
        new ShaderTagId("VertexLM")
    };

    partial void DrawUnsupportedShaders()
    {

        if (errorMaterial == null)
        {
            errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }
            

        var drawingSettings = new DrawingSettings(legacyShaderTagIds[0], new SortingSettings(camera)) { overrideMaterial = errorMaterial };

        for(int i = 1; i < legacyShaderTagIds.Length; i ++)
        {
            drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
        }

        //dont care the other settings
        var filterSettings = FilteringSettings.defaultValue;

        context.DrawRenderers(cullingResults, ref drawingSettings, ref filterSettings);
    }

    partial void DrawGizmos()
    {
        if(Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }
    }

    partial void PrepareForSceneWindow()
    {
        if(camera.cameraType == CameraType.SceneView)
        {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        }
    }

    partial void PrepareBuffer()
    {
        //see in the Profiler
        Profiler.BeginSample("Editor Only");
        buffer.name = SampleName = camera.name;
        Profiler.EndSample();
    }

#else
    const string SampleName = bufferName;

#endif
}
