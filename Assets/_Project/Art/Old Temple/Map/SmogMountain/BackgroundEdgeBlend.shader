Shader "EasternFantasy/Background Edge Blend"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FadeWidth ("Left overlap fraction", Range(0.001, 0.5)) = 0.25
        _SpriteHalfWidth ("Sprite half width before scale", Float) = 8.36
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float edge : TEXCOORD1; fixed4 color : COLOR; };
            sampler2D _MainTex;
            fixed4 _Color;
            float _FadeWidth, _SpriteHalfWidth;
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.edge = (v.vertex.x + _SpriteHalfWidth) / (2 * _SpriteHalfWidth);
                o.color = v.color * _Color;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
                c.a *= smoothstep(0, _FadeWidth, i.edge);
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
