Shader "Eastern Fantasy/Remote Mountain Atmosphere"
{
    Properties
    {
        _MotionTime ("Wind time", Float) = 0
        _FogDensity ("Fog density", Range(0,1)) = 0.28
        _CloudDensity ("Cloud density", Range(0,1)) = 0.72
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _MotionTime, _FogDensity, _CloudDensity;

            struct Varyings
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(appdata_full input)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.uv = input.texcoord;
                return output;
            }

            float hash(float2 coordinate)
            {
                return frac(sin(dot(coordinate, float2(127.1, 311.7))) * 43758.5453);
            }

            float noise(float2 coordinate)
            {
                float2 cell = floor(coordinate);
                float2 fraction = frac(coordinate);
                fraction = fraction * fraction * (3.0 - 2.0 * fraction);
                return lerp(
                    lerp(hash(cell), hash(cell + float2(1, 0)), fraction.x),
                    lerp(hash(cell + float2(0, 1)), hash(cell + 1.0), fraction.x),
                    fraction.y);
            }

            float wisps(float2 coordinate)
            {
                return noise(coordinate) * 0.55
                    + noise(coordinate * 2.1 + 5.7) * 0.3
                    + noise(coordinate * 4.3 + 13.2) * 0.15;
            }

            fixed4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float time = _MotionTime;
                float valley = 1.0 - smoothstep(0.45, 0.8, uv.y);
                float drifting = wisps(float2(uv.x * 19.0 - time * 0.065,
                    uv.y * 8.0 + sin(uv.x * 12.0 + time * 0.07) * 0.5));
                float crossing = wisps(float2(uv.x * 27.0 - time * 0.11 + 12.0,
                    uv.y * 13.0 + time * 0.025));
                float fog = _FogDensity * (0.2 + valley * (0.35 + drifting * 0.65)
                    + smoothstep(0.42, 0.76, crossing) * 0.3);

                float cloudHeight = smoothstep(0.61, 0.78, uv.y);
                float2 cloudFlow = float2(uv.x * 12.0 - time * 0.11,
                    uv.y * 9.0 + sin(uv.x * 9.0 - time * 0.035) * 0.28);
                float clouds = wisps(cloudFlow);
                float cloudShape = smoothstep(0.34, 0.64, clouds);
                float cloud = _CloudDensity * cloudHeight * cloudShape;
                float opacity = fog + cloud * (1.0 - fog);
                float3 fogColor = float3(0.55, 0.64, 0.74);
                float3 cloudColor = lerp(float3(0.48, 0.56, 0.68),
                    float3(0.17, 0.24, 0.36), smoothstep(0.42, 0.7, clouds));
                float3 color = lerp(fogColor, cloudColor, cloud / max(opacity, 0.001));
                return fixed4(color, opacity);
            }
            ENDCG
        }
    }
}
