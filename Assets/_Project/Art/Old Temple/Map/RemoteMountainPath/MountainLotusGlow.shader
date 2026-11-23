Shader "Eastern Fantasy/Mountain Lotus Glow"
{
    Properties
    {
        _Color ("Lotus light", Color) = (0.45, 0.95, 0.8, 0.65)
        _MotionTime ("Light time", Float) = 0
        _LaunchPulse ("Launch pulse", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha One
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color;
            float _MotionTime, _LaunchPulse;
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

            fixed4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float time = _MotionTime;
                float breath = 0.82 + sin(time * 1.8) * 0.18;
                float2 pool = float2((uv.x - 0.5) / 0.4, (uv.y - 0.14) / 0.07);
                float halo = exp(-dot(pool, pool) * 2.3) * 0.6 * breath;
                float columnWidth = 0.08 + uv.y * 0.05;
                float column = exp(-pow((uv.x - 0.5) / columnWidth, 2.0) * 2.0);
                column *= smoothstep(0.1, 0.2, uv.y) * (1.0 - smoothstep(0.25, 0.9, uv.y));
                float light = halo + column * 0.12 * breath;
                for (int i = 0; i < 3; i++)
                {
                    float phase = frac(time * 0.32 + i / 3.0);
                    float height = 0.14 + phase * 0.65;
                    float radius = 0.08 + phase * 0.3;
                    float2 ellipse = float2((uv.x - 0.5) / radius, (uv.y - height) / 0.034);
                    float ring = exp(-pow((length(ellipse) - 1.0) / 0.13, 2.0));
                    light += ring * sin(phase * UNITY_PI) * 0.3;
                }
                float edgeFade = smoothstep(0.0, 0.06, uv.x) * (1.0 - smoothstep(0.94, 1.0, uv.x));
                light *= edgeFade * (1.0 + _LaunchPulse * 1.5);
                return fixed4(_Color.rgb, saturate(light) * _Color.a);
            }
            ENDCG
        }
    }
}
