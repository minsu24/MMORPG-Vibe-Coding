Shader "Eastern Fantasy/FX/Cave Lantern Glow"
{
    Properties
    {
        _GlowColor ("Glow Color and Strength", Color) = (1, .57, .24, .34)
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent" "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline" "DisableBatching"="True"
        }
        Blend One OneMinusSrcColor
        Cull Off
        ZWrite Off
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _GlowColor;
            CBUFFER_END
            float _CaveLanternTime;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float phase : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                float2 center = TransformObjectToWorld(float3(0, 0, 0)).xy;
                output.phase = frac(sin(dot(center, float2(17.17, 39.31))) * 4375.43) * 6.28318;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float t = _CaveLanternTime;
                float phase = input.phase;
                float flicker = .83 + .20 * sin(t * (2.1 + phase * .12) + phase)
                    + .08 * sin(t * 5.1 + phase * 2.7);
                float2 p = input.uv * 2 - 1;
                float radiusSquared = dot(p, p);
                float halo = exp(-3.4 * radiusSquared) * (1 - smoothstep(.52, 1, radiusSquared));
                float core = exp(-24 * radiusSquared);
                float strength = (.75 * halo + .25 * core) * flicker * _GlowColor.a;
                return half4(_GlowColor.rgb * strength, 0);
            }
            ENDHLSL
        }
    }
}
