Shader "EasternFantasy/Temple Gate Platform Tile"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _TileRect ("Sprite UV rectangle", Vector) = (0.0125598,0.11796,0.976077,0.88204)
        _Overlap ("Horizontal seam blend fraction", Range(0.01,0.2)) = 0.0784314
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="False" }
        Cull Off
        ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            sampler2D _MainTex;
            fixed4 _Color;
            float4 _TileRect;
            float _Overlap;
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }
            fixed4 samplePremultiplied(float x, float y)
            {
                fixed4 c = tex2D(_MainTex, float2(_TileRect.x + x * _TileRect.z, y));
                c.rgb *= c.a;
                return c;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                // Continuous SpriteRenderer tiles restart their UVs at each sprite rect.
                float t = saturate((i.uv.x - _TileRect.x) / _TileRect.z);
                float period = 1 - _Overlap;
                float x = t * period;
                fixed4 c = samplePremultiplied(x + _Overlap, i.uv.y);
                float blend = smoothstep(period - _Overlap, period, x);
                // Both ends converge to the same interior sample, including fog alpha.
                fixed4 next = samplePremultiplied(max(0, x - (period - _Overlap)), i.uv.y);
                c = lerp(c, next, blend);
                c.rgb *= i.color.rgb * i.color.a;
                c.a *= i.color.a;
                return c;
            }
            ENDCG
        }
    }
}
