Shader "Custom/SpectatorWobble_URP"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "white" {}
        _BaseColor("Color", Color) = (1,1,1,1)
        _WobbleStrength("Wobble Strength", Float) = 0.05
        _WobbleSpeed("Wobble Speed", Float) = 2.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            float4 _BaseMap_ST; // ✅ Tiling va Offset uchun
            float4 _BaseColor;
            float _WobbleStrength;
            float _WobbleSpeed;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float wobble = sin(_Time.y * _WobbleSpeed + IN.positionOS.x * 5.0) * _WobbleStrength;
                IN.positionOS.y += wobble;

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);

                // ✅ Tiling va Offset qo‘llash
                OUT.uv = IN.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                return texColor * _BaseColor;
            }
            ENDHLSL
        }
    }
}
