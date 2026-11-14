Shader "CursedCrown/MenuOverlay"
{
    Properties
    {
        [MainTexture] _MainTex("Texture", 2D) = "white" {}
        _AlphaOnly("Font alpha mask", Float) = 0
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            ZTest Always ZWrite Off Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; UNITY_VERTEX_OUTPUT_STEREO };
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            float _AlphaOnly;
            CBUFFER_END
            Varyings vert(Attributes v)
            {
                Varyings o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.uv=v.uv;o.color=v.color;return o;
            }
            half4 frag(Varyings i):SV_Target
            { UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); half4 sample=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv); sample.rgb=lerp(sample.rgb,half3(1,1,1),_AlphaOnly); return sample*_BaseColor*i.color; }
            ENDHLSL
        }
    }
}
