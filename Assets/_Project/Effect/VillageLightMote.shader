Shader "Eastern Fantasy/Village Light Mote"
{
    Properties
    {
        _Color("Light Color", Color) = (1, 0.94, 0.76, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; float phase : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color * _Color;
                output.phase = dot(TransformObjectToWorld(input.positionOS.xyz).xy, float2(0.7, 1.3));
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.uv * 2.0 - 1.0;
                float radius = dot(p, p);
                // A distinct warm core stays readable over the sunlit village.
                // The wider, faint halo keeps the particles soft rather than flat dots.
                float core = exp(-radius * 20.0);
                float halo = exp(-radius * 3.5) * (1.0 - smoothstep(0.65, 1.0, radius));
                float glow = 0.88 + 0.12 * sin(_Time.y * 1.1 + input.phase);
                float alpha = saturate(core * 0.9 + halo * 0.4) * input.color.a * glow;
                return half4(input.color.rgb * lerp(1.0, 2.2, core), alpha);
            }
            ENDHLSL
        }
    }
}
