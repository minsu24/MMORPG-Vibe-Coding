Shader "Eastern Fantasy/Loot Icon"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Bright Contour", Color) = (.92,.97,1,.85)
        _OutlineWidth ("Contour Pixels", Range(0,6)) = 2
        _Brightness ("Brightness", Range(0,2)) = 1.12
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment LootFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            float4 _MainTex_TexelSize;
            fixed4 _OutlineColor;
            float _OutlineWidth;
            float _Brightness;
            fixed4 LootFragment(v2f input) : SV_Target
            {
                fixed4 color = SampleSpriteTexture(input.texcoord);
                // Keep the contour legible when large source sprites are drawn small.
                float2 step = max(_MainTex_TexelSize.xy, fwidth(input.texcoord)) * _OutlineWidth;
                float edge = min(min(SampleSpriteTexture(input.texcoord + float2(step.x,0)).a,
                                     SampleSpriteTexture(input.texcoord - float2(step.x,0)).a),
                                 min(SampleSpriteTexture(input.texcoord + float2(0,step.y)).a,
                                     SampleSpriteTexture(input.texcoord - float2(0,step.y)).a));
                color.rgb *= _Brightness;
                color.rgb = lerp(color.rgb, _OutlineColor.rgb, (1-smoothstep(.05,.8,edge)) * _OutlineColor.a);
                color *= input.color;
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
