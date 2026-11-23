Shader "Eastern Fantasy/Remote Mountain Background"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _LeftTex ("Left landscape", 2D) = "white" {}
        _MiddleTex ("Middle landscape", 2D) = "white" {}
        _RightTex ("Right landscape", 2D) = "white" {}
        _WorldBounds ("World left, bottom, width, height", Vector) = (-10.48899, -6.5998, 57.6, 10.8)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _LeftTex, _MiddleTex, _RightTex;
            float4 _WorldBounds;

            struct Varyings
            {
                float4 position : SV_POSITION;
                float2 landscape : TEXCOORD0;
                fixed4 color : COLOR;
            };

            Varyings vert(appdata_full input)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(input.vertex);
                float2 world = mul(unity_ObjectToWorld, input.vertex).xy;
                output.landscape = (world - _WorldBounds.xy) / _WorldBounds.zw;
                output.color = input.color;
                return output;
            }

            fixed4 frag(Varyings input) : SV_Target
            {
                float x = input.landscape.x * 57.6;
                float y = input.landscape.y;
                fixed4 left = tex2D(_LeftTex, float2(x / 22.4, y));
                fixed4 middle = tex2D(_MiddleTex, float2((x - 16.0) / 25.6, y));
                fixed4 right = tex2D(_RightTex, float2((x - 35.2) / 22.4, y));
                // All three sprites sample one world-aligned landscape across their borders.
                fixed4 landscape = lerp(left, middle, smoothstep(17.6, 20.8, x));
                landscape = lerp(landscape, right, smoothstep(36.8, 40.0, x));
                return landscape * input.color;
            }
            ENDCG
        }
    }
}
