Shader "Eastern Fantasy/Sakura Petal"
{
    Properties
    {
        _Color("Tint", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
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
                float2 p = input.uv * 2.0 - 1.0;
                float width = 0.65 * sqrt(saturate(1.0 - p.y * p.y)) * (0.8 + 0.2 * p.y);
                float edge = width - abs(p.x);
                float aa = max(fwidth(edge), 0.015);
                float alpha = smoothstep(-aa, aa, edge) * (1.0 - smoothstep(0.91, 1.0, abs(p.y)));
                // A small cleft and a warmer base suggest a cherry blossom petal.
                float notch = smoothstep(0.64, 0.96, p.y) * (1.0 - smoothstep(0.0, 0.16, abs(p.x)));
                alpha *= 1.0 - notch;
                half3 pink = lerp(half3(0.94, 0.57, 0.68), half3(1.0, 0.92, 0.93), saturate(input.uv.y));
                return half4(pink * input.color.rgb, alpha * input.color.a);
            }
            ENDHLSL
        }
    }
}
