Shader "Hidden/EasternFantasy/MinimapInk"
{
    Properties { _MainTex ("Scenery", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float luminance(float2 uv)
            { return dot(tex2D(_MainTex, uv).rgb, float3(.299, .587, .114)); }
            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;
                float gray = luminance(uv);
                float2 step = _MainTex_TexelSize.xy * 1.5;
                float edge = abs(luminance(uv + float2(step.x,0)) - luminance(uv - float2(step.x,0)))
                    + abs(luminance(uv + float2(0,step.y)) - luminance(uv - float2(0,step.y)));
                float grain = frac(sin(dot(floor(uv * float2(1024,512)),float2(12.9898,78.233))) * 43758.5453);
                float wash = saturate((pow(max(gray,0), .45) - .12) * 1.35);
                float3 paper = float3(.76,.62,.38);
                float3 ink = float3(.012,.04,.033);
                float3 color = lerp(ink, paper, wash);
                color -= min(edge * .22, .08);
                color += (grain - .5) * .018;
                float vignette = saturate(dot((uv - .5) * float2(1.1,1.4), (uv - .5) * float2(1.1,1.4)));
                color *= 1 - vignette * .28;
                return fixed4(color,1);
            }
            ENDHLSL
        }
    }
}
