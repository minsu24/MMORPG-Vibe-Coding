// Based on the installed URP Sprite-Lit-Default shader; retains 2D lighting and sprite flipping.
Shader "Eastern Fantasy/Village Lantern Swing"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _SwingDegrees("Lantern Swing Angle", Range(0, 6)) = 2.5
        _SwingSpeed("Lantern Swing Speed", Range(0.1, 3)) = 1.4
        _SwingPhase("Lantern Swing Phase", Float) = 0
        _WaterStrength("River Ripple Strength In Pixels", Range(0, 4)) = 0
        _WaterSpeed("River Flow Speed", Range(0.1, 3)) = 0.8
        _WaterShimmer("River Shimmer", Range(0, 0.1)) = 0.035
        _WaterPhase("River Flow Phase", Float) = 0
        _ClothStrength("Cloth Wind Strength In Pixels", Range(0, 12)) = 0
        _ClothSpeed("Cloth Wind Speed", Range(0.1, 3)) = 1.6
        _ClothPhase("Cloth Wind Phase", Float) = 0
        _TreeStrength("Large Tree Wind Strength In Pixels", Range(0, 16)) = 0
        _TreeSpeed("Large Tree Wind Speed", Range(0.1, 3)) = 1.2
        _TreePhase("Large Tree Wind Phase", Float) = 0
        [Toggle] _ForegroundLanterns("Foreground Lanterns", Float) = 0
        _Softness("Detail Softening", Range(0, 1)) = 0
        _Contrast("Contrast", Range(0.5, 1.5)) = 1
        _Saturation("Saturation", Range(0, 1)) = 1
        _Brightness("Brightness", Range(0, 1.5)) = 1
        _OutlineColor("Outline Color", Color) = (0.16, 0.09, 0.04, 1)
        _OutlineWidth("Outline Width In Texture Pixels", Range(0, 6)) = 0
        _FlashAmount("Hit Flash", Range(0, 1)) = 0
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        [MaterialToggle] _ZWrite("ZWrite", Float) = 0

        // Legacy properties. They're here so that materials using this shader can gracefully fallback to the legacy sprite shader.
        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex LitVertex
            #pragma fragment LitFragment

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"

            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color        : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_LIT_OUTPUTS
                half4 color        : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Lit2DCommon.hlsl"

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _OutlineColor;
                half _Softness;
                half _Contrast;
                half _Saturation;
                half _Brightness;
                half _OutlineWidth;
                half _FlashAmount;
                float _SwingDegrees;
                float _SwingSpeed;
                float _SwingPhase;
                float _ForegroundLanterns;
                float _WaterStrength;
                float _WaterSpeed;
                float _WaterShimmer;
                float _WaterPhase;
                float _ClothStrength;
                float _ClothSpeed;
                float _ClothPhase;
                float _TreeStrength;
                float _TreeSpeed;
                float _TreePhase;
            CBUFFER_END

            #include "Assets/_Project/Effect/VillageHarmony.hlsl"

            #include "Assets/_Project/Effect/VillageLanternSwing.hlsl"
            #include "Assets/_Project/Effect/VillageWaterFlow.hlsl"
            #include "Assets/_Project/Effect/VillageClothWind.hlsl"
            #include "Assets/_Project/Effect/VillageTreeWind.hlsl"
            Varyings LitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonLitVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;

                return o;
            }

            half4 LitFragment(Varyings input) : SV_Target
            {
                input.uv = ApplyVillageTreeWind(input.uv);
                input.uv = ApplyVillageClothWind(input.uv);
                input.uv = ApplyVillageLanternSwing(input.uv);
                input.uv = ApplyVillageWaterFlow(input.uv);
                half4 main = ApplyReadability(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv), input.uv, input.color);
                main = ApplyVillageWaterShimmer(main, input.uv);
                half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, input.uv);
                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv));
                SurfaceData2D surfaceData;
                InputData2D inputData;
                InitializeSurfaceData(main.rgb, main.a, mask, normalTS, surfaceData);
                InitializeInputData(input.uv, input.lightingUV, inputData);
                return CombinedShapeLightShared(surfaceData, inputData);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "NormalsRendering"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex NormalsRenderingVertex
            #pragma fragment NormalsRenderingFragment

            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_NORMALS_INPUTS
                float4 color        : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_NORMALS_OUTPUTS
                half4   color           : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Normals2DCommon.hlsl"

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START( UnityPerMaterial )
                half4 _Color;
                half4 _OutlineColor;
                half _Softness;
                half _Contrast;
                half _Saturation;
                half _Brightness;
                half _OutlineWidth;
                half _FlashAmount;
                float _SwingDegrees;
                float _SwingSpeed;
                float _SwingPhase;
                float _ForegroundLanterns;
                float _WaterStrength;
                float _WaterSpeed;
                float _WaterShimmer;
                float _WaterPhase;
                float _ClothStrength;
                float _ClothSpeed;
                float _ClothPhase;
                float _TreeStrength;
                float _TreeSpeed;
                float _TreePhase;
            CBUFFER_END

            #include "Assets/_Project/Effect/VillageLanternSwing.hlsl"
            #include "Assets/_Project/Effect/VillageWaterFlow.hlsl"
            #include "Assets/_Project/Effect/VillageClothWind.hlsl"
            #include "Assets/_Project/Effect/VillageTreeWind.hlsl"
            Varyings NormalsRenderingVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonNormalsVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;

                return o;
            }

            half4 NormalsRenderingFragment(Varyings input) : SV_Target
            {
                input.uv = ApplyVillageTreeWind(input.uv);
                input.uv = ApplyVillageClothWind(input.uv);
                input.uv = ApplyVillageLanternSwing(input.uv);
                input.uv = ApplyVillageWaterFlow(input.uv);
                return CommonNormalsFragment(input, input.color);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" "Queue"="Transparent" "RenderType"="Transparent"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"
          
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _OutlineColor;
                half _Softness;
                half _Contrast;
                half _Saturation;
                half _Brightness;
                half _OutlineWidth;
                half _FlashAmount;
                float _SwingDegrees;
                float _SwingSpeed;
                float _SwingPhase;
                float _ForegroundLanterns;
                float _WaterStrength;
                float _WaterSpeed;
                float _WaterShimmer;
                float _WaterPhase;
                float _ClothStrength;
                float _ClothSpeed;
                float _ClothPhase;
                float _TreeStrength;
                float _TreeSpeed;
                float _TreePhase;
            CBUFFER_END

            #include "Assets/_Project/Effect/VillageHarmony.hlsl"

            #include "Assets/_Project/Effect/VillageLanternSwing.hlsl"
            #include "Assets/_Project/Effect/VillageWaterFlow.hlsl"
            #include "Assets/_Project/Effect/VillageClothWind.hlsl"
            #include "Assets/_Project/Effect/VillageTreeWind.hlsl"
            Varyings UnlitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonUnlitVertex(input);
                o.color = input.color *_Color * unity_SpriteColor;
                return o;
            }

            half4 UnlitFragment(Varyings input) : SV_Target
            {
                input.uv = ApplyVillageTreeWind(input.uv);
                input.uv = ApplyVillageClothWind(input.uv);
                input.uv = ApplyVillageLanternSwing(input.uv);
                input.uv = ApplyVillageWaterFlow(input.uv);
                return ApplyVillageWaterShimmer(ApplyReadability(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv), input.uv, input.color), input.uv);
            }
            ENDHLSL
        }
    }
}
