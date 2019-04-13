// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "SCM/InvisibleUnit" {
    Properties {
        [Space(15)][Header(Main Properties)]
		[Space(10)]_MainTex("Base (RGB)", 2D) = "white" {}
        [Space(10)]_Color ("Color", Color) = (1,1,1,1)
        _EmissiveIntensity ("Emissive Intensity", Range(0, 5)) = 2
        [Space(10)]_FresnelStrength ("Fresnel Strength", Range(0.05, 10)) = 1
        [MaterialToggle] _InvertFresnel ("Invert Fresnel", Float ) = 0

        [Space(15)][Header(Animation Properties)]
        [Space(10)][MaterialToggle] _InvertEffect ("Invert Effect", Float ) = 0
        [Space(10)]_AnimatedNormalmapCloud ("Animated Normal map (Cloud)", 2D) = "bump" {}
        _NormalIntensityCloud ("Normal Intensity", Range(0, 2)) = 2
        [Space(25)]_AnimationSpeed ("Animation Speed", Range(0, 1)) = 0.2
        _RotationDegree ("Rotation (Degree)", Float ) = 0
        [MaterialToggle] _SwitchAnimationFlow ("Switch Animation Flow", Float ) = 1
        [Space(10)]_NormalIntensity2 ("Normal Intensity2", Range(0, 2)) = 1
        [MaterialToggle] _SwitchAnimationFlow2 ("Switch Animation Flow2", Float ) = 0
    }
    SubShader {
        Tags {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }
        GrabPass{ }
        Pass {
            Name "FORWARD"
            Tags {
                "LightMode"="ForwardBase"
            }
            
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDBASE
            #include "UnityCG.cginc"
            #pragma multi_compile_fwdbase_fullshadows
            #pragma multi_compile_fog
            #pragma only_renderers d3d9 d3d11 glcore gles gles3 metal d3d11_9x xboxone ps4 psp2 n3ds wiiu 
            #pragma target 3.0
            uniform sampler2D _GrabTexture;
            uniform float4 _TimeEditor;
            uniform float _FresnelStrength;
            uniform float4 _Color;
            uniform float _NormalIntensityCloud;
            uniform float _EmissiveIntensity;
            uniform float _AnimationSpeed;
            uniform fixed _InvertEffect;
            uniform fixed _SwitchAnimationFlow;
            uniform float _RotationDegree;
            uniform float _NormalIntensity2;
            uniform fixed _SwitchAnimationFlow2;
            uniform fixed _InvertFresnel;
            uniform sampler2D _AnimatedNormalmapCloud; uniform float4 _AnimatedNormalmapCloud_ST;
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float4 posWorld : TEXCOORD0;
                float3 normalDir : TEXCOORD1;
                float3 tangentDir : TEXCOORD2;
                float3 bitangentDir : TEXCOORD3;
                float4 screenPos : TEXCOORD4;
                UNITY_FOG_COORDS(5)
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.normalDir = UnityObjectToWorldNormal(v.normal);
                o.tangentDir = normalize( mul( unity_ObjectToWorld, float4( v.tangent.xyz, 0.0 ) ).xyz );
                o.bitangentDir = normalize(cross(o.normalDir, o.tangentDir) * v.tangent.w);
                float4 objPos = mul ( unity_ObjectToWorld, float4(0,0,0,1) );
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                o.pos = UnityObjectToClipPos(v.vertex );
                UNITY_TRANSFER_FOG(o,o.pos);
                o.screenPos = o.pos;
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
                float4 objPos = mul ( unity_ObjectToWorld, float4(0,0,0,1) );
                #if UNITY_UV_STARTS_AT_TOP
                    float grabSign = -_ProjectionParams.x;
                #else
                    float grabSign = _ProjectionParams.x;
                #endif
                i.normalDir = normalize(i.normalDir);
                i.screenPos = float4( i.screenPos.xy / i.screenPos.w, 0, 0 );
                i.screenPos.y *= _ProjectionParams.x;
                float3x3 tangentTransform = float3x3( i.tangentDir, i.bitangentDir, i.normalDir);
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
                float4 node_9568 = _Time + _TimeEditor;
                float node_1245 = (node_9568.g*_AnimationSpeed);
                float Speed = node_1245;
                float node_4251 = Speed;
                float AngleRotation = (((_RotationDegree*3.141592654)/180.0)+90.0);
                float node_4002_ang = AngleRotation;
                float node_4002_spd = 1.0;
                float node_4002_cos = cos(node_4002_spd*node_4002_ang);
                float node_4002_sin = sin(node_4002_spd*node_4002_ang);
                float2 node_4002_piv = float2(0.5,0.5);
                float2 node_4002 = (mul(mul( unity_WorldToObject, float4(((i.posWorld.rgb-objPos.rgb)/8.0),0) ).xyz.rgb.gr-node_4002_piv,float2x2( node_4002_cos, -node_4002_sin, node_4002_sin, node_4002_cos))+node_4002_piv);
                float2 _SwitchAnimationFlow2_var = lerp( (node_4002+node_4251*float2(1,1)), (node_4002+node_4251*float2(-1,-1)), _SwitchAnimationFlow2 );
                float3 _NormalmapLocal = UnpackNormal(tex2D(_AnimatedNormalmapCloud,TRANSFORM_TEX(_SwitchAnimationFlow2_var, _AnimatedNormalmapCloud)));
                float node_7154_ang = AngleRotation;
                float node_7154_spd = 1.0;
                float node_7154_cos = cos(node_7154_spd*node_7154_ang);
                float node_7154_sin = sin(node_7154_spd*node_7154_ang);
                float2 node_7154_piv = float2(0.5,0.5);
                float2 node_7154 = (mul(mul( UNITY_MATRIX_V, float4(((i.posWorld.rgb-objPos.rgb)/8.0),0) ).xyz.rgb.gr-node_7154_piv,float2x2( node_7154_cos, -node_7154_sin, node_7154_sin, node_7154_cos))+node_7154_piv);
                float2 _SwitchAnimationFlow_var = lerp( (node_7154+node_1245*float2(1,1)), (node_7154+node_1245*float2(-1,-1)), _SwitchAnimationFlow );
                float3 _NormalmapView = UnpackNormal(tex2D(_AnimatedNormalmapCloud,TRANSFORM_TEX(_SwitchAnimationFlow_var, _AnimatedNormalmapCloud)));
                float3 node_782_nrm_base = lerp(float3(0,0,1),_NormalmapLocal.rgb,_NormalIntensity2) + float3(0,0,1);
                float3 node_782_nrm_detail = lerp(float3(0,0,1),_NormalmapView.rgb,_NormalIntensityCloud) * float3(-1,-1,1);
                float3 node_782_nrm_combined = node_782_nrm_base*dot(node_782_nrm_base, node_782_nrm_detail)/node_782_nrm_base.z - node_782_nrm_detail;
                float3 node_782 = node_782_nrm_combined;
                float3 normalLocal = lerp( node_782, (node_782*2.0+-1.0), _InvertEffect );
                float3 normalDirection = normalize(mul( normalLocal, tangentTransform )); // Perturbed normals
                float2 sceneUVs = float2(1,grabSign)*i.screenPos.xy*0.5+0.5;
                float4 sceneColor = tex2D(_GrabTexture, sceneUVs);
////// Lighting:
////// Emissive:
                float node_5849 = pow(1.0-max(0,dot(normalDirection, viewDirection)),_FresnelStrength);
                float node_3554 = (node_5849*node_5849*node_5849);
                float3 node_7250 = ((_Color.rgb*lerp( node_3554, (node_3554*-1.0+1.0), _InvertFresnel ))*_EmissiveIntensity);
                float3 emissive = node_7250;
                float3 finalColor = emissive + tex2D( _GrabTexture, sceneUVs.rg).rgb;
                fixed4 finalRGBA = fixed4(finalColor,1);
                UNITY_APPLY_FOG(i.fogCoord, finalRGBA);
                return finalRGBA;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
