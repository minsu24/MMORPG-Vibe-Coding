Shader "Eastern Fantasy/UI/Mountain Portal Fog"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Coverage ("Coverage", Range(0, 1)) = 0
        _Arrival ("Arrival", Float) = 0
        _Origin ("Origin", Range(0, 1)) = 0.5
        _Drift ("Drift", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _Coverage, _Arrival, _Origin, _Drift;
            struct Varyings
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            Varyings vert(appdata_full input)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.uv = input.texcoord;
                output.color = input.color;
                return output;
            }

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float noise(float2 p)
            {
                float2 cell = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(cell), hash(cell + float2(1, 0)), f.x),
                    lerp(hash(cell + float2(0, 1)), hash(cell + 1.0), f.x), f.y);
            }

            fixed4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float2 cloud = uv * float2(5.2, 8.6) - float2(_Drift * 1.28, _Drift * 0.28);
                float billow = noise(cloud) * 0.67 + noise(cloud * 2.3 + 7.1) * 0.33;
                float risingEdge = length(float2((uv.x - _Origin) * 0.69, uv.y * 0.89)) + (billow - 0.5) * 0.22;
                float sweepingEdge = (1.0 - uv.x) * 0.92 + (uv.y - 0.5) * 0.18 + (billow - 0.5) * 0.34;
                float edge = lerp(risingEdge, sweepingEdge, _Arrival);
                float mask = 1.0 - smoothstep(_Coverage * 1.36 - 0.21, _Coverage * 1.36 + 0.15, edge);
                mask = lerp(mask * smoothstep(0.0, 0.18, _Coverage), 1.0, smoothstep(0.78, 1.0, _Coverage));
                fixed3 fog = lerp(fixed3(0.43, 0.51, 0.52), fixed3(0.86, 0.88, 0.83), billow);
                return fixed4(fog, mask * input.color.a);
            }
            ENDCG
        }
    }
}
