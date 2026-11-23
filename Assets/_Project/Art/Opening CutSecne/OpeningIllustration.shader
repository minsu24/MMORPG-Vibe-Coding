Shader "Eastern Fantasy/Opening Illustration"
{
    Properties
    {
        [PerRendererData] _MainTex ("Illustration", 2D) = "white" {}
        _CameraData ("Zoom and focus", Vector) = (1, 0.5, 0.5, 0)
        _EffectData ("Effect and time", Vector) = (0, 0, 0, 8)
        _WipeData ("Wipe", Vector) = (1, 0, 0, 0)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct AppData { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct Interpolators { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            sampler2D _MainTex;
            float4 _CameraData, _EffectData, _WipeData;

            Interpolators vert(AppData input)
            {
                Interpolators output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(i), hash(i + float2(1, 0)), f.x),
                            lerp(hash(i + float2(0, 1)), hash(i + 1), f.x), f.y);
            }

            float wash(float2 p)
            {
                return 0.57 * noise(p) + 0.29 * noise(p * 2.03 + 7.5) +
                       0.14 * noise(p * 4.11 - 3.1);
            }

            // 각 구름은 화면 바깥쪽에 뿌리를 두어 가운데에 세로 이음선이 생기지 않습니다.
            float cloud(float2 uv, float distanceOut, float mirror, float time)
            {
                uv.x = lerp(uv.x, 1.0 - uv.x, mirror) + distanceOut;
                float2 p = float2(uv.x * 7.1 + mirror * 8.1, uv.y * 3.4 + time * 0.10);
                float edge = 0.51 - uv.x + (wash(p) - 0.48) * 0.43;
                edge += sin(uv.y * 11.0 + mirror * 3.6) * 0.045;
                return smoothstep(-0.048, 0.105, edge);
            }

            float floatingSpecks(float2 uv, float time, float2 grid, float rise)
            {
                float2 p = uv * grid - float2(time * 0.61, time * rise);
                float2 cell = floor(p);
                float seed = hash(cell);
                float2 center = 0.2 + 0.6 * float2(seed, hash(cell + 9.27));
                float radius = lerp(0.024, 0.085, seed);
                float dotShape = 1.0 - smoothstep(radius * 0.18, radius, length(frac(p) - center));
                return dotShape * step(0.49, seed) * (0.58 + 0.42 * sin(time * 2.6 + seed * 31));
            }

            fixed4 frag(Interpolators input) : SV_Target
            {
                float2 uv = input.uv;
                float invZoom = rcp(max(1.0, _CameraData.x));
                float2 artUV = uv * invZoom + saturate(_CameraData.yz) * (1.0 - invZoom);
                float3 color = tex2D(_MainTex, artUV).rgb;
                float mode = _EffectData.x;
                float cue = _EffectData.y;
                float time = _EffectData.z;
                float grain = hash(floor(uv * float2(1280, 720)));
                float2 scaled = uv * float2(1.77778, 1);

                if (mode == 1 || mode == 5)
                {
                    float opening = smoothstep(0.03, 2.35, time);
                    float away = (mode == 1 ? opening : 1.0 - cue) * 0.70;
                    float left = cloud(uv, away, 0.0, time);
                    float right = cloud(uv, away, 1.0, time);
                    float paper = wash(uv * float2(17, 10) + time * 0.03);
                    float3 cloudColor = lerp(float3(0.58, 0.52, 0.45), float3(0.99, 0.95, 0.84), paper * 0.54 + 0.43);
                    color = lerp(color, cloudColor, saturate(max(left, right) * (0.88 + 0.12 * grain)));
                    if (mode == 5)
                    {
                        // 직전 장면의 먹이 종이에 마르며 밝아지는 흐름을 이어갑니다.
                        float dry = smoothstep(0.02, 1.23, time);
                        float seep = wash(float2(uv.x * 4.4, uv.y * 3.8));
                        float ink = 1.0 - smoothstep(seep - 0.18, seep + 0.15, dry);
                        color = lerp(color, float3(0.014, 0.011, 0.020), ink);
                        color += float3(0.66, 0.49, 0.18) * floatingSpecks(uv, time, float2(33, 19), 0.42) * (1.0 - max(left, right)) * 0.36;
                    }
                }
                else if (mode == 2)
                {
                    // 금빛이 그림에 고정되도록 카메라 이동 전의 그림 좌표를 사용합니다.
                    float2 delta = (artUV - float2(0.539, 0.475)) * float2(1.77778, 1.0);
                    float distanceFromSeal = length(delta);
                    float pulse = sin(cue * 3.14159);
                    float glow = exp(-distanceFromSeal * distanceFromSeal * 39.0);
                    float ripple = exp(-pow((distanceFromSeal - cue * 0.43) * 30, 2));
                    float sparks = floatingSpecks(artUV, time, float2(39, 22), 1.1);
                    color += float3(0.85, 0.49, 0.15) * (pulse * (glow * 0.32 + ripple * 0.16) + sparks * glow * 0.61);
                }
                else if (mode == 4)
                {
                    float2 delta = (artUV - float2(0.54, 0.56)) * float2(1.77778, 1);
                    float vapour = wash(scaled * 8.0 - float2(time * 0.42, time * 0.69));
                    float aura = exp(-abs(delta.x + sin(delta.y * 12) * 0.055) * 12.0) * smoothstep(0.42, 0.74, vapour);
                    color += float3(0.31, 0.074, 0.43) * aura * (0.45 + cue * 0.5);
                    float inkPaper = wash(scaled * 4.5) * 0.67 + wash(scaled * 19.0) * 0.33;
                    float ink = smoothstep(inkPaper - 0.09, inkPaper + 0.15, cue);
                    color = lerp(color, float3(0.014, 0.011, 0.020), ink);
                }
                else if (mode == 6)
                {
                    float haze = wash(float2(uv.x * 6.0 - time * 0.34, uv.y * 3.5 - time * 0.28));
                    float sparks = floatingSpecks(uv, time, float2(39, 22), 1.8);
                    color = lerp(color, float3(0.23, 0.088, 0.048), 0.27 * haze);
                    color += float3(0.94, 0.36, 0.08) * sparks * 0.7;
                }

                // 흩날리는 모래의 경계와 한지 결을 겹친 좌→우 공개·소거입니다.
                float ragged = (wash(float2(uv.x * 25, uv.y * 13)) - 0.5) * 0.10;
                float enterEdge = _WipeData.x * 1.16 - 0.08 - uv.x + ragged;
                float exitEdge = uv.x - (_WipeData.y * 1.16 - 0.08) + ragged;
                float enter = _WipeData.z > 0 && _WipeData.x < 1 ? smoothstep(-0.018, 0.024, enterEdge) : 1.0;
                float exit = _WipeData.w > 0 && _WipeData.y > 0 ? smoothstep(-0.018, 0.024, exitEdge) : 1.0;
                color *= enter * exit;
                if (mode == 3 && (enter < 1.0 || exit < 1.0))
                {
                    float edge = max(1.0 - saturate(abs(enterEdge) * 22), 1.0 - saturate(abs(exitEdge) * 22));
                    float sand = floatingSpecks(float2(1.0 - uv.y, uv.x), time, float2(54, 90), 6.9);
                    color += float3(0.69, 0.51, 0.31) * edge * sand * 0.90;
                }

                return fixed4(color, 1.0) * input.color;
            }
            ENDCG
        }
    }
}
