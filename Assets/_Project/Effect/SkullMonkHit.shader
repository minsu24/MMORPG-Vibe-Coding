Shader "Eastern Fantasy/FX/Skull Monk Hit"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        [PerRendererData] _BurstMode ("Burst", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex HitVertex
            #pragma fragment HitFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float _BurstMode;
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
                half alpha : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings HitVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                SetUpSpriteInstanceProperties();
                output.positionCS = TransformObjectToHClip(UnityFlipSprite(input.positionOS, unity_SpriteProps.xy));
                output.uv = input.uv;
                output.alpha = input.color.a * unity_SpriteColor.a;
                return output;
            }

            half4 HitFragment(Varyings input) : SV_Target
            {
                if (_BurstMode < .5)
                {
                    half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a * input.alpha;
                    return half4(half3(1, .85, .65) * alpha, alpha);
                }

                float age = 1 - input.alpha;
                float2 burstUV = input.uv * 2 - 1;
                float distanceToCenter = length(burstUV);
                float glow = (1 - smoothstep(.03, .30 + age * .24, distanceToCenter)) * pow(1 - age, 3);
                float spark = 0;
                [unroll]
                for (int i = 0; i < 8; i++)
                {
                    float variation = frac(i * .618);
                    float angle = i * .7854 + (variation - .5) * .45 + .27;
                    float2 direction = float2(cos(angle), sin(angle));
                    float alongRay = dot(burstUV, direction);
                    float acrossRay = abs(dot(burstUV, float2(-direction.y, direction.x)));
                    float tip = .23 + variation * .23 + age * (.34 + variation * .17);
                    float tail = max(.02, tip - (.22 - age * .12));
                    float width = (.034 - age * .016) * (1 - .7 * smoothstep(tail, tip, alongRay));
                    float streak = smoothstep(tail, tail + .045, alongRay)
                        * (1 - smoothstep(tip - .035, tip, alongRay))
                        * (1 - smoothstep(width * .35, width, acrossRay));
                    spark = max(spark, streak);
                }
                spark *= 1 - smoothstep(.16, 1, age);

                half3 gold = half3(1, .56, .16);
                half3 whiteGold = half3(1, .93, .73);
                half glowAlpha = glow * .65;
                return half4(gold * glowAlpha * (1 - spark) + whiteGold * spark,
                    glowAlpha * (1 - spark) + spark);
            }
            ENDHLSL
        }
    }
}
