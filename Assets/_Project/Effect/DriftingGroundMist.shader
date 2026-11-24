Shader "Eastern Fantasy/FX/Drifting Ground Mist"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _PreviewTime ("Preview time (-1 for live time)", Float) = -1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _PreviewTime;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 world : TEXCOORD1;
                half4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                SetUpSpriteInstanceProperties();
                float3 positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                output.positionCS = TransformObjectToHClip(positionOS);
                output.uv = input.uv;
                output.world = TransformObjectToWorld(positionOS).xy;
                output.color = input.color * unity_SpriteColor;
                return output;
            }

            float Hash(float2 cell)
            {
                float3 value = frac(cell.xyx * .1031);
                value += dot(value, value.yzx + 33.33);
                return frac((value.x + value.y) * value.z);
            }

            float Noise(float2 samplePos)
            {
                float2 cell = floor(samplePos);
                float2 f = frac(samplePos);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(cell), Hash(cell + float2(1, 0)), f.x),
                    lerp(Hash(cell + float2(0, 1)), Hash(cell + 1), f.x), f.y);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float seconds = _PreviewTime < 0 ? _Time.y : _PreviewTime;
                // Fade every side so patches can overlap without showing sprite edges.
                float2 edge = smoothstep(float2(.28, .12), 1, abs(input.uv * 2 - 1));
                float feather = (1 - edge.x) * (1 - edge.y);

                float2 drifting = input.world - seconds * float2(.86, .08);
                float curl = Noise(drifting * float2(.40, 1.30) + 13.1) - .5;
                float2 wispUV = drifting * float2(.64, 3.30) + float2(curl * .42, curl * 1.55);
                float wispNoise = .72 * Noise(wispUV) + .28 * Noise(wispUV * float2(1.7, 1.4) + 9.7);
                float wisp = smoothstep(.34, .72, wispNoise);
                float wispBand = 1 - smoothstep(.055, .30, abs(input.uv.y - .50 - curl * .44));

                // The broader layer rolls more slowly behind the brighter wisps.
                float2 rolling = input.world - seconds * float2(.39, -.055);
                float softCloud = smoothstep(.22, .74, Noise(rolling * float2(.37, 1.55) + 26.3));
                float cloudBand = 1 - smoothstep(.10, .48, abs(input.uv.y - .42));
                float density = .81 * wisp * wispBand + .30 * softCloud * cloudBand;
                return half4(input.color.rgb * lerp(.88, 1.06, wisp), input.color.a * feather * saturate(density));
            }
            ENDHLSL
        }
    }
}
