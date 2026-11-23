Shader "Eastern Fantasy/FX/Cave Atmosphere"
{
    Properties
    {
        _Smoke ("Smoke Shape", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
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
                half _Smoke;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half FragMask(float2 p)
            {
                float radius = length(p);
                half dust = 1 - smoothstep(.04, .95, radius);
                // Soft, uneven wisps. The particles' rotation and growth change the silhouette over time.
                float bend = sin(7 * p.y + 2 * p.x) * .15;
                float filament = exp(-4 * abs(p.x + bend)) * (.68 + .32 * cos(5 * p.y - 3 * p.x));
                float smoke = (1 - smoothstep(.06, 1, radius)) * (.33 + .67 * filament);
                return lerp(dust * dust, smoke, _Smoke);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return half4(input.color.rgb, input.color.a * FragMask(input.uv * 2 - 1));
            }
            ENDHLSL
        }
    }
}
