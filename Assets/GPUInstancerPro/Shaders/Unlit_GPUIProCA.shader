Shader "GPUInstancerPro/CrowdAnimations/Universal Render Pipeline/Unlit"
{
    Properties
    {
        [MainTexture] _BaseMap("Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1, 1, 1, 1)
        _Cutoff("AlphaCutout", Range(0.0, 1.0)) = 0.5

        // BlendMode
        _Surface("__surface", Float) = 0.0
        _Blend("__mode", Float) = 0.0
        _Cull("__cull", Float) = 2.0
        [ToggleUI] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _BlendOp("__blendop", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _SrcBlendAlpha("__srcA", Float) = 1.0
        [HideInInspector] _DstBlendAlpha("__dstA", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _AlphaToMask("__alphaToMask", Float) = 0.0

        // Editmode props
        _QueueOffset("Queue offset", Float) = 0.0

        // ObsoleteProperties
        [HideInInspector] _MainTex("BaseMap", 2D) = "white" {}
        [HideInInspector] _Color("Base Color", Color) = (0.5, 0.5, 0.5, 1)
        [HideInInspector] _SampleGI("SampleGI", float) = 0.0 // needed from bakedlit
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "IgnoreProjector" = "True"
            "UniversalMaterialType" = "Unlit"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100

        // -------------------------------------
        // Render State Commands
        Blend [_SrcBlend][_DstBlend], [_SrcBlendAlpha][_DstBlendAlpha]
        ZWrite [_ZWrite]
        Cull [_Cull]

        Pass
        {
            Name "Unlit"

            // -------------------------------------
            // Render State Commands
            AlphaToMask[_AlphaToMask]

            HLSLPROGRAM
#define UNIVERSAL_DOTS_PRAGMAS_INCLUDED // Added by GPUIPro

            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex UnlitPassVertexGPUICA
            #pragma fragment UnlitPassFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ALPHAMODULATE_ON

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fog
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            //--------------------------------------
            // GPU Instancing

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitForwardPass.hlsl"
            
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" // Added by GPUIPro

#include_with_pragmas "Packages/com.gurbu.gpui-pro/Runtime/Shaders/Include/GPUInstancerSetup.hlsl"
#include_with_pragmas "Packages/com.gurbu.gpui-pro.crowd-animations/Runtime/Shaders/Include/GPUICrowdSetup.hlsl"
#pragma instancing_options procedural:setupGPUI
#pragma multi_compile_instancing
#ifdef GPUI_PRO_CROWD_ACTIVE
    #undef UNITY_SETUP_INSTANCE_ID
    #define UNITY_SETUP_INSTANCE_ID(input)
#endif

Varyings UnlitPassVertexGPUICA(Attributes input, uint vertexID : SV_VertexID)
{
    #ifdef GPUI_PRO_CROWD_ACTIVE
        UnitySetupInstanceID(UNITY_GET_INSTANCE_ID(input));
        setupGPUI();
        GPUI_CROWD_VERTEX(vertexID, input.positionOS.xyz, input.normalOS.xyz, input.tangentOS.xyz);
    #endif
    return UnlitPassVertex(input);
}
ENDHLSL
        }

        // Fill GBuffer data to prevent "holes", just in case someone wants to reuse GBuffer for non-lighting effects.
        // Deferred lighting is stenciled out.
        Pass
        {
            Name "GBuffer"
            Tags
            {
                "LightMode" = "UniversalGBuffer"
            }

            HLSLPROGRAM
#define UNIVERSAL_DOTS_PRAGMAS_INCLUDED // Added by GPUIPro

            #pragma target 4.5

            // Deferred Rendering Path does not support the OpenGL-based graphics API:
            // Desktop OpenGL, OpenGL ES 3.0, WebGL 2.0.
            #pragma exclude_renderers gles3 glcore

            // -------------------------------------
            // Shader Stages
            #pragma vertex UnlitPassVertexGPUICA
            #pragma fragment UnlitPassFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ALPHAMODULATE_ON

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            //--------------------------------------
            // GPU Instancing

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitGBufferPass.hlsl"
            
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" // Added by GPUIPro

#include_with_pragmas "Packages/com.gurbu.gpui-pro/Runtime/Shaders/Include/GPUInstancerSetup.hlsl"
#include_with_pragmas "Packages/com.gurbu.gpui-pro.crowd-animations/Runtime/Shaders/Include/GPUICrowdSetup.hlsl"
#pragma instancing_options procedural:setupGPUI
#pragma multi_compile_instancing
#ifdef GPUI_PRO_CROWD_ACTIVE
    #undef UNITY_SETUP_INSTANCE_ID
    #define UNITY_SETUP_INSTANCE_ID(input)
#endif

Varyings UnlitPassVertexGPUICA(Attributes input, uint vertexID : SV_VertexID)
{
    #ifdef GPUI_PRO_CROWD_ACTIVE
        UnitySetupInstanceID(UNITY_GET_INSTANCE_ID(input));
        setupGPUI();
        GPUI_CROWD_VERTEX_VN(vertexID, input.positionOS.xyz, input.normalOS.xyz);
    #endif
    return UnlitPassVertex(input);
}
ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite On
            ColorMask R

            HLSLPROGRAM
#define UNIVERSAL_DOTS_PRAGMAS_INCLUDED // Added by GPUIPro

            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex DepthOnlyVertexGPUICA
            #pragma fragment DepthOnlyFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ALPHATEST_ON

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            //--------------------------------------
            // GPU Instancing

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" // Added by GPUIPro

#include_with_pragmas "Packages/com.gurbu.gpui-pro/Runtime/Shaders/Include/GPUInstancerSetup.hlsl"
#include_with_pragmas "Packages/com.gurbu.gpui-pro.crowd-animations/Runtime/Shaders/Include/GPUICrowdSetup.hlsl"
#pragma instancing_options procedural:setupGPUI
#pragma multi_compile_instancing
#ifdef GPUI_PRO_CROWD_ACTIVE
    #undef UNITY_SETUP_INSTANCE_ID
    #define UNITY_SETUP_INSTANCE_ID(input)
#endif

Varyings DepthOnlyVertexGPUICA(Attributes input, uint vertexID : SV_VertexID)
{
    #ifdef GPUI_PRO_CROWD_ACTIVE
        UnitySetupInstanceID(UNITY_GET_INSTANCE_ID(input));
        setupGPUI();
        GPUI_CROWD_VERTEX_V(vertexID, input.position.xyz);
    #endif
    return DepthOnlyVertex(input);
}
ENDHLSL
        }

        Pass
        {
            Name "DepthNormalsOnly"
            Tags
            {
                "LightMode" = "DepthNormalsOnly"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite On

            HLSLPROGRAM
#define UNIVERSAL_DOTS_PRAGMAS_INCLUDED // Added by GPUIPro

            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex DepthNormalsVertexGPUICA
            #pragma fragment DepthNormalsFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ALPHATEST_ON

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT // forward-only variant
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            //--------------------------------------
            // GPU Instancing

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitDepthNormalsPass.hlsl"
            
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" // Added by GPUIPro

#include_with_pragmas "Packages/com.gurbu.gpui-pro/Runtime/Shaders/Include/GPUInstancerSetup.hlsl"
#include_with_pragmas "Packages/com.gurbu.gpui-pro.crowd-animations/Runtime/Shaders/Include/GPUICrowdSetup.hlsl"
#pragma instancing_options procedural:setupGPUI
#pragma multi_compile_instancing
#ifdef GPUI_PRO_CROWD_ACTIVE
    #undef UNITY_SETUP_INSTANCE_ID
    #define UNITY_SETUP_INSTANCE_ID(input)
#endif

Varyings DepthNormalsVertexGPUICA(Attributes input, uint vertexID : SV_VertexID)
{
    #ifdef GPUI_PRO_CROWD_ACTIVE
        UnitySetupInstanceID(UNITY_GET_INSTANCE_ID(input));
        setupGPUI();
        GPUI_CROWD_VERTEX(vertexID, input.positionOS.xyz, input.normal.xyz, input.tangentOS.xyz);
    #endif
    return DepthNormalsVertex(input);
}
ENDHLSL
        }

        // This pass it not used during regular rendering, only for lightmap baking.
        Pass
        {
            Name "Meta"
            Tags
            {
                "LightMode" = "Meta"
            }

            // -------------------------------------
            // Render State Commands
            Cull Off

            HLSLPROGRAM
#define UNIVERSAL_DOTS_PRAGMAS_INCLUDED // Added by GPUIPro

            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex UniversalVertexMetaGPUICA
            #pragma fragment UniversalFragmentMetaUnlit

            // -------------------------------------
            // Unity defined keywords
            #pragma shader_feature EDITOR_VISUALIZATION

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitMetaPass.hlsl"
            
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" // Added by GPUIPro

#include_with_pragmas "Packages/com.gurbu.gpui-pro/Runtime/Shaders/Include/GPUInstancerSetup.hlsl"
#include_with_pragmas "Packages/com.gurbu.gpui-pro.crowd-animations/Runtime/Shaders/Include/GPUICrowdSetup.hlsl"
#pragma instancing_options procedural:setupGPUI
#pragma multi_compile_instancing
#ifdef GPUI_PRO_CROWD_ACTIVE
    #undef UNITY_SETUP_INSTANCE_ID
    #define UNITY_SETUP_INSTANCE_ID(input)
#endif

Varyings UniversalVertexMetaGPUICA(Attributes input, uint vertexID : SV_VertexID)
{
    #ifdef GPUI_PRO_CROWD_ACTIVE
        UnitySetupInstanceID(UNITY_GET_INSTANCE_ID(input));
        setupGPUI();
        GPUI_CROWD_VERTEX_VN(vertexID, input.positionOS.xyz, input.normalOS.xyz);
    #endif
    return UniversalVertexMeta(input);
}
ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.UnlitShader"
}