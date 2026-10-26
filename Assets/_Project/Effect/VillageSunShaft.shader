Shader "Eastern Fantasy/Village Sun Shaft"
{
    Properties
    {
        _Color("Sunlight Color", Color) = (1, 0.91, 0.70, 1)
        _Intensity("Intensity", Range(0, 0.5)) = 0.11
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Intensity;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                // The transverse coordinate grows with radius, so the cone
                // stays smooth across both triangles all the way to its source.
                float across = (input.uv.x * 2.0 - 1.0) / max(input.uv.y, 0.0001);
                float edge = pow(saturate(1.0 - abs(across)), 2.0);
                float lengthFade = smoothstep(0.035, 0.18, input.uv.y)
                    * (1.0 - smoothstep(0.68, 1.0, input.uv.y));
                // Stable shafts with a very slow change in haze, rather than a waving image.
                float haze = 0.94 + 0.06 * sin(_Time.y * 0.24 + input.uv.y * 3.0);
                return half4(input.color.rgb, edge * lengthFade * haze * _Intensity * input.color.a);
            }
            ENDHLSL
        }
    }
}
