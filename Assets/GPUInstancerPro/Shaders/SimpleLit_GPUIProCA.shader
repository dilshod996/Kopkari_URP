// Shader targeted for low end devices. Single Pass Forward Rendering.
Shader "GPUInstancerPro/CrowdAnimations/Universal Render Pipeline/Simple Lit"
{
    // Keep properties of StandardSpecular shader for upgrade reasons.
    Properties
    {
        [MainTexture] _BaseMap("Base Map (RGB) Smoothness / Alpha (A)", 2D) = "white" {}
        [MainColor]   _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        _Cutoff("Alpha Clipping", Range(0.0, 1.0)) = 0.5

        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _SpecColor("Specular Color", Color) = (0.5, 0.5, 0.5, 0.5)
        _SpecGlossMap("Specular Map", 2D) = "white" {}
        _SmoothnessSource("Smoothness Source", Float) = 0.0
        _SpecularHighlights("Specular Highlights", Float) = 1.0

        [HideInInspector] _BumpScale("Scale", Float) = 1.0
        [NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump" {}

        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0)
        [NoScaleOffset]_EmissionMap("Emission Map", 2D) = "white" {}

        // Blending state
        _Surface("__surface", Float) = 0.0
        _Blend("__blend", Float) = 0.0
        _Cull("__cull", Float) = 2.0
        [ToggleUI] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _SrcBlendAlpha("__srcA", Float) = 1.0
        [HideInInspector] _DstBlendAlpha("__dstA", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _BlendModePreserveSpecular("_BlendModePreserveSpecular", Float) = 1.0
        [HideInInspector] _AlphaToMask("__alphaToMask", Float) = 0.0

        [ToggleUI] _ReceiveShadows("Receive Shadows", Float) = 1.0
        // Editmode props
        _QueueOffset("Queue offset", Float) = 0.0

        // ObsoleteProperties
        [HideInInspector] _MainTex("BaseMap", 2D) = "white" {}
        [HideInInspector] _Color("Base Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _Shininess("Smoothness", Float) = 0.0
        [HideInInspector] _GlossinessSource("GlossinessSource", Float) = 0.0
        [HideInInspector] _SpecSource("SpecularHighlights", Float) = 0.0

        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "SimpleLit"
            "IgnoreProjector" = "True"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            // -------------------------------------
            // Render State Commands
            // Use same blending / depth states as Standard shader
            Blend[_SrcBlend][_DstBlend], [_SrcBlendAlpha][_DstBlendAlpha]
            ZWrite[_ZWrite]
            Cull[_Cull]
            AlphaToMask[_AlphaToMask]

            HLSLPROGRAM
#define UNIVERSAL_DOTS_PRAGMAS_INCLUDED // Added by GPUIPro

            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex LitPassVertexSimpleGPUICA
            #pragma fragment LitPassFragmentSimple

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
            #pragma shader_feature_local_fragment _ _SPECGLOSSMAP _SPECULAR_COLOR
            #pragma shader_feature_local_fragment _GLOSSINESS_FROM_BASE_ALPHA

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile_fog
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            //--------------------------------------
            // GPU Instancing

            //--------------------------------------
            // Defines
            #define BUMP_SCALE_NOT_SUPPORTED 1

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitForwardPass.hlsl"
            
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" // Added by GPUIPro

#include_with_pragmas "Packages/com.gurbu.gpui-pro/Runtime/Shaders/Include/GPUInstancerSetup.hlsl"
#include_with_pragmas "Packages/com.gurbu.gpui-pro.crowd-animations/Runtime/Shaders/Include/GPUICrowdSetup.hlsl"
#pragma instancing_options procedural:setupGPUI
#pragma multi_compile_instancing
#ifdef GPUI_PRO_CROWD_ACTIVE
    #undef UNITY_SETUP_INSTANCE_ID
    #define UNITY_SETUP_INSTANCE_ID(input)
#endif

Varyings LitPassVertexSimpleGPUICA(Attributes input, uint vertexID : SV_VertexID)
{
    #ifdef GPUI_PRO_CROWD_ACTIVE
        UnitySetupInstanceID(UNITY_GET_INSTANCE_ID(input));
        setupGPUI();
        GPUI_CROWD_VERTEX(vertexID, input.positionOS.xyz, input.normalOS.xyz, input.tangentOS.xyz);
    #endif
    return LitPassVertexSimple(input);
}
ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
#define UNIVERSAL_DOTS_PRAGMAS_INCLUDED // Added by GPUIPro

            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex ShadowPassVertexGPUICA
            #pragma fragment ShadowPassFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _GLOSSINESS_FROM_BASE_ALPHA

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            //--------------------------------------
            // GPU Instancing

            // This is used during shadow map generation to differentiate between directional and punctual light shadows, as they use different formulas to apply Normal Bias
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" // Added by GPUIPro

#include_with_pragmas "Packages/com.gurbu.gpui-pro/Runtime/Shaders/Include/GPUInstancerSetup.hlsl"
#include_with_pragmas "Packages/com.gurbu.gpui-pro.crowd-animations/Runtime/Shaders/Include/GPUICrowdSetup.hlsl"
#pragma instancing_options procedural:setupGPUI
#pragma multi_compile_instancing
#ifdef GPUI_PRO_CROWD_ACTIVE
    #undef UNITY_SETUP_INSTANCE_ID
    #define UNITY_SETUP_INSTANCE_ID(input)
#endif

Varyings ShadowPassVertexGPUICA(Attributes input, uint vertexID : SV_VertexID)
{
    #ifdef GPUI_PRO_CROWD_ACTIVE
        UnitySetupInstanceID(UNITY_GET_INSTANCE_ID(input));
        setupGPUI();
        GPUI_CROWD_VERTEX_VN(vertexID, input.positionOS.xyz, input.normalOS.xyz);
    #endif
    return ShadowPassVertex(input);
}
ENDHLSL
        }

        Pass
        {
            Name "GBuffer"
            Tags
            {
                "LightMode" = "UniversalGBuffer"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite[_ZWrite]
            ZTest LEqual
            Cull[_Cull]

            HLSLPROGRAM
#define UNIVERSAL_DOTS_PRAGMAS_INCLUDED // Added by GPUIPro

            #pragma target 4.5

            // Deferred Rendering Path does not support the OpenGL-based graphics API:
            // Desktop OpenGL, OpenGL ES 3.0, WebGL 2.0.
            #pragma exclude_renderers gles3 glcore

            // -------------------------------------
            // Shader Stages
            #pragma vertex LitPassVertexSimpleGPUICA
            #pragma fragment LitPassFragmentSimple

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            //#pragma shader_feature _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local_fragment _ _SPECGLOSSMAP _SPECULAR_COLOR
            #pragma shader_feature_local_fragment _GLOSSINESS_FROM_BASE_ALPHA
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            //#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            //#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_fragment _ _RENDER_PASS_ENABLED
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            //--------------------------------------
            // GPU Instancing

            //--------------------------------------
            // Defines
            #define BUMP_SCALE_NOT_SUPPORTED 1

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitGBufferPass.hlsl"
            
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" // Added by GPUIPro

#include_with_pragmas "Packages/com.gurbu.gpui-pro/Runtime/Shaders/Include/GPUInstancerSetup.hlsl"
#include_with_pragmas "Packages/com.gurbu.gpui-pro.crowd-animations/Runtime/Shaders/Include/GPUICrowdSetup.hlsl"
#pragma instancing_options procedural:setupGPUI
#pragma multi_compile_instancing
#ifdef GPUI_PRO_CROWD_ACTIVE
    #undef UNITY_SETUP_INSTANCE_ID
    #define UNITY_SETUP_INSTANCE_ID(input)
#endif

Varyings LitPassVertexSimpleGPUICA(Attributes input, uint vertexID : SV_VertexID)
{
    #ifdef GPUI_PRO_CROWD_ACTIVE
        UnitySetupInstanceID(UNITY_GET_INSTANCE_ID(input));
        setupGPUI();
        GPUI_CROWD_VERTEX(vertexID, input.positionOS.xyz, input.normalOS.xyz, input.tangentOS.xyz);
    #endif
    return LitPassVertexSimple(input);
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
            Cull[_Cull]

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
            #pragma shader_feature_local_fragment _GLOSSINESS_FROM_BASE_ALPHA

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            //--------------------------------------
            // GPU Instancing

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitInput.hlsl"
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

        // This pass is used when drawing to a _CameraNormalsTexture texture
        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
#define UNIVERSAL_DOTS_PRAGMAS_INCLUDED // Added by GPUIPro

            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex DepthNormalsVertexGPUICA
            #pragma fragment DepthNormalsFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _GLOSSINESS_FROM_BASE_ALPHA

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            // Universal Pipeline keywords
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            //--------------------------------------
            // GPU Instancing

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitDepthNormalsPass.hlsl"
            
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
            #pragma fragment UniversalFragmentMetaSimple

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _SPECGLOSSMAP
            #pragma shader_feature EDITOR_VISUALIZATION

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitMetaPass.hlsl"

            
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

        Pass
        {
            Name "Universal2D"
            Tags
            {
                "LightMode" = "Universal2D"
                "RenderType" = "Transparent"
                "Queue" = "Transparent"
            }

            HLSLPROGRAM
#define UNIVERSAL_DOTS_PRAGMAS_INCLUDED // Added by GPUIPro

            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex vertGPUICA
            #pragma fragment frag

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Universal2D.hlsl"
            
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" // Added by GPUIPro

#include_with_pragmas "Packages/com.gurbu.gpui-pro/Runtime/Shaders/Include/GPUInstancerSetup.hlsl"
#include_with_pragmas "Packages/com.gurbu.gpui-pro.crowd-animations/Runtime/Shaders/Include/GPUICrowdSetup.hlsl"
#pragma instancing_options procedural:setupGPUI
#pragma multi_compile_instancing
#ifdef GPUI_PRO_CROWD_ACTIVE
    #undef UNITY_SETUP_INSTANCE_ID
    #define UNITY_SETUP_INSTANCE_ID(input)
#endif

Varyings vertGPUICA(Attributes input, uint vertexID : SV_VertexID)
{
    #ifdef GPUI_PRO_CROWD_ACTIVE
        UnitySetupInstanceID(UNITY_GET_INSTANCE_ID(input));
        setupGPUI();
        GPUI_CROWD_VERTEX_V(vertexID, input.positionOS.xyz);
    #endif
    return vert(input);
}
ENDHLSL
        }
    }

    Fallback  "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.SimpleLitShader"
}