Shader "Eastern Fantasy/UI/Map Ink Reveal"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Reveal ("Ink spread", Range(0,1)) = 1
        _Seed ("Ink seed", Float) = 1
        _DetailFogEnabled ("Detail discovery fog", Float) = 0
        _DetailRegionCount ("Detail region count", Float) = 0
        _DetailFarm ("Farm detail map", Float) = 0
        _UVRect ("Sprite UV rectangle", Vector) = (0,0,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float4 local:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            sampler2D _MainTex;
            fixed4 _Color, _TextureSampleAdd;
            float4 _ClipRect, _UVRect;
            float _Reveal, _Seed;
            float _DetailFogEnabled, _DetailRegionCount, _DetailFarm;
            float4 _DetailRegions[6], _DetailNames[6], _DetailNumbers[6], _DetailBodies[6];
            v2f vert(appdata v)
            {
                v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.local=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color*_Color; return o;
            }
            float hash21(float2 p) { return frac(sin(dot(p,float2(127.1,311.7))+_Seed*17.3)*43758.5453); }
            float noise(float2 p)
            {
                float2 i=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(hash21(i),hash21(i+float2(1,0)),f.x),lerp(hash21(i+float2(0,1)),hash21(i+1),f.x),f.y);
            }
            float insideRect(float2 p,float4 r)
            { return step(r.x,p.x)*step(p.x,r.z)*step(r.y,p.y)*step(p.y,r.w); }
            float3 discoverySample(float2 uv)
            {
                float2 p=(uv-_UVRect.xy)/max(_UVRect.zw,float2(.00001,.00001));
                float erased=0;
                [unroll] for(int index=0;index<6;index++)
                    if(index<_DetailRegionCount && (_DetailRegions[index].z<.5 || _DetailRegions[index].w<.999))
                        erased=max(erased,max(insideRect(p,_DetailNames[index]),insideRect(p,_DetailNumbers[index])));
                return lerp(tex2D(_MainTex,uv).rgb,float3(.045,.06,.075),erased);
            }
            float3 discoveredMap(float2 uv,float3 original)
            {
                float2 p=(uv-_UVRect.xy)/max(_UVRect.zw,float2(.00001,.00001));
                float fog=0, number=0, label=0;
                float drift=(noise(float2(p.y*9,p.x*5))-.5)*.032;
                [unroll] for(int index=0;index<6;index++)
                {
                    if(index<_DetailRegionCount && (_DetailRegions[index].z<.5 || _DetailRegions[index].w<.999))
                    {
                        float4 region=_DetailRegions[index];
                        float left=region.x<.001 ? 1 : smoothstep(region.x-.016,region.x+.016,p.x+drift);
                        float right=region.y>.999 ? 1 : 1-smoothstep(region.y-.016,region.y+.016,p.x+drift);
                        // A ragged ink front grows out from the newly discovered region's center.
                        float progress=region.z<.5 ? 0 : region.w;
                        float2 local=float2((p.x-(region.x+region.y)*.5)/max(region.y-region.x,.001), (p.y-.48)*.7);
                        float field=length(local)+(noise(p*float2(36,22))-.5)*.18;
                        float cleared=progress>=.999 ? 1 : smoothstep(field-.07,field+.07,lerp(-.15,.9,progress));
                        fog+=left*right*(1-cleared);
                        float4 diamond=_DetailNumbers[index];
                        float2 q=abs((p-(diamond.xy+diamond.zw)*.5)/max((diamond.zw-diamond.xy)*.5,.00001));
                        number=max(number,step(q.x+q.y,1));
                        float4 body=_DetailBodies[index];
                        float halfWidth=(body.z-body.x)*.5;
                        float tipHeight=(body.z-body.x)*1920.0/1080.0*.6;
                        float bodyShape=insideRect(p,body)*step(abs(p.x-(body.x+body.z)*.5),halfWidth*saturate((p.y-body.y)/max(tipHeight,.00001)));

                        float innerBody=step(abs(p.x-(body.x+body.z)*.5),max(0,halfWidth*saturate((p.y-body.y)/max(tipHeight,.00001))-3.0/1920.0));
                        label=max(label,insideRect(p,_DetailNames[index])*innerBody*(1-smoothstep(.67,1,progress)));
                    }
                }
                fog=saturate(fog);
                if(fog<.001 && label<.001) return original;
                float2 radius=float2(28.0/1920.0,28.0/1080.0)*_UVRect.zw;
                float3 blurred=discoverySample(uv)*.04;
                blurred+=discoverySample(uv+float2(radius.x,0))*.12;
                blurred+=discoverySample(uv-float2(radius.x,0))*.12;
                blurred+=discoverySample(uv+float2(0,radius.y))*.12;
                blurred+=discoverySample(uv-float2(0,radius.y))*.12;
                blurred+=discoverySample(uv+radius)*.12;
                blurred+=discoverySample(uv-radius)*.12;
                blurred+=discoverySample(uv+float2(radius.x,-radius.y))*.12;
                blurred+=discoverySample(uv+float2(-radius.x,radius.y))*.12;
                float gray=dot(blurred,float3(.299,.587,.114));
                float clouds=noise(p*float2(10,8))*.7+noise(p*float2(24,19))*.3;
                float3 inkFog=lerp(gray*float3(.50,.60,.69),float3(.045,.062,.083),.66+clouds*.18);
                // Leave a subdued hint of the connecting route along the lower landscape.
                float routeY=lerp(.64+.10*p.x,.53+.10*p.x,_DetailFarm);
                float route=1-smoothstep(.025,.065,abs((1-p.y)-routeY));
                float fogAmount=fog*(1-number)*(1-route*.10);
                float3 result=lerp(original,inkFog,fogAmount);
                // The original place names are baked into the artwork; erase only their interiors.
                return lerp(result,float3(.025,.031,.032),label);
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv)+_TextureSampleAdd;
                if(_DetailFogEnabled>.5) c.rgb=discoveredMap(i.uv,c.rgb);
                c*=i.color;
                if(_Reveal < .999)
                {
                    float2 p=(i.uv-_UVRect.xy)/max(_UVRect.zw,float2(.00001,.00001));
                    float2 origin=float2(.2+frac(_Seed*.37)*.14,.42+frac(_Seed*.21)*.18);
                    float grain=noise(p*12)*.7+noise(p*29)*.3;
                    float field=length((p-origin)*float2(1,1.28))+(grain-.5)*.19;
                    float spread=lerp(-.22,1.6,_Reveal);
                    float mask=smoothstep(field-.055,field+.055,spread);
                    float edge=mask*(1-smoothstep(0,.12,spread-field));
                    float gray=dot(c.rgb,float3(.299,.587,.114));
                    float3 wetInk=gray*float3(.48,.38,.28);
                    c.rgb=lerp(c.rgb,wetInk,saturate(edge*.72+(1-_Reveal)*.36));
                    c.a*=mask*step(.0001,_Reveal);
                }
                #ifdef UNITY_UI_CLIP_RECT
                c.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(c.a-.001);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
