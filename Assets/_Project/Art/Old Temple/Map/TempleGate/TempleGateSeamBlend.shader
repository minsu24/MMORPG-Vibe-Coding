Shader "EasternFantasy/Temple Gate Seam Blend"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Inset ("Source border inset (UV)", Vector) = (0.00239234,0.0042508,0,0)
        _BlendRight ("Right overlap (UV)", Float) = 0
        _BlendBottom ("Bottom overlap (UV)", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
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
            float4 _Inset;
            float _BlendRight, _BlendBottom;
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                // Skip the thin baked-in frame without changing the source art.
                float2 uv = lerp(_Inset.xy, 1 - _Inset.xy, i.uv);
                fixed4 c = tex2D(_MainTex, uv) * i.color;
                float a = 1;
                if (_BlendRight > 0) a *= smoothstep(0, _BlendRight, 1 - i.uv.x);
                if (_BlendBottom > 0) a *= smoothstep(0, _BlendBottom, i.uv.y);
                c.a *= a;
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
