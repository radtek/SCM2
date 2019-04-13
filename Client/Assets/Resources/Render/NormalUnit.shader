// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_LightMatrix0' with 'unity_WorldToLight'

// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "SCM/NormalUnit"
{  
    Properties
    {  
        _MainTint("MainTint", Color) = (1, 1, 1, 1)
        _MainTex("Base (RGB)", 2D) = "white" {}
        _SpecPower("SpecPower", Range(0, 1)) = 0
        _State("State", int) = 0

        _Diffuse("Diffuse", Color) = (1, 1, 1, 1)
        _Specular("Specular", Color) = (1, 1, 1, 1)
        _Gloss("Gloss", Range(8.0, 256)) = 20
        _CellDensity("CellDensity", Range(0, 1)) = 0.3

        _Factor("Factor", Range(1, 2)) = 0
        _OutlineColor("OutlineColor", Color) = (0, 0, 0, 1)

        _Reflectivity("Reflectivity", Range (0,1)) = 1
        _Environment("Environment", Cube) = "white" {}
    }  
  
    SubShader
    {  
        Tags { "RenderType"="Opaque" "Queue" = "Transparent"}   
        LOD 150  

        Pass
        {
            Cull Front

            ZWrite Off
            ZTEST Less
            ColorMask RGB

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
 
            struct v2f
            {
                float4 pos : POSITION;
                float4 color : COLOR;
            };

            float _Factor;
            half4 _OutlineColor;
 
            v2f vert(appdata_full v)
            {
                v2f o;

				float3 vt = v.vertex * _Factor;
                o.pos = UnityObjectToClipPos(vt);

                return o;
            }
 
            half4 frag(v2f IN):COLOR
            {
                return _OutlineColor;
            }

            ENDCG
        }

        Pass
        {
            Tags { "LightMode"="ForwardBase"}

            Blend SrcAlpha OneMinusSrcAlpha

            Cull Back

            CGPROGRAM  

            #pragma multi_compile_fwdbase
            #pragma vertex vert  
            #pragma fragment frag  
              
            #include "UnityCG.cginc"  
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            fixed4 _MainTint;
            sampler2D _MainTex;  
            float4 _MainTex_ST; 
//            float4 _LightColor0; 
            float _SpecPower;
            int _State;

            fixed4 _Diffuse;
            fixed4 _Specular;
            float _Gloss;
            float _CellDensity;

            samplerCUBE _Environment;
            float _Reflectivity;

            struct appdata_t
            {  
                float4 vertex : POSITION;  
                float3 normal : NORMAL;
                float2 texcoord : TEXCOORD0;  
            };
  
            struct v2f
            {
                half2 texcoord : TEXCOORD3;  
                float4 screenPos : TEXCOORD4;
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 r : TEXCOORD2;
                SHADOW_COORDS(2)
            }; 

            float3 Reflect(float3 i, float3 n)
            {
                return i - 2.0 * n * dot(n, i);
            }

            // 像素抖动 只有 4 挡半透明
            void PixelJitter(float2 xy, fixed a, bool isDynamic)
            {
                float x = ceil(xy.x);
                float y = ceil(xy.y);

                if (isDynamic)
                {
                    if (a < 0.25)
                    {
                        if (frac(_Time.y) > 0.5)
                        {
                            if (fmod(x, 2) < 1 || fmod(y, 2) < 1)
                                clip(-1);
                        }
                        else
                        {
                            if (!(fmod(x, 2) < 1.5 || fmod(y, 2) < 1))
                                clip(-1);
                        }
                    }
                    else if (a < 0.5)
                    {
                        if (frac(_Time.y) > 0.5)
                        {
                            if (fmod(x + y, 2) < 1)
                                clip(-1);
                        }
                        else
                        {
                            if (!fmod(x + y, 2) < 1)
                                clip(-1);
                        }

                    }
                    else if (a < 0.75)
                    {
                        if (frac(_Time.y) > 0.5)
                        {
                            if (fmod(x, 2) < 1 && fmod(y, 2) < 1)
                                clip(-1);
                        }
                        else
                        {
                            if (!(fmod(x, 2) < 1 && fmod(y, 2) < 1))
                                clip(-1);
                        }
                    }
                }
                else
                {
                    if (a < 0.25)
                    {
                        if (fmod(x, 2) < 1 || fmod(y, 2) < 1)
                            clip(-1);
                    }
                    else if (a < 0.5)
                    {
                        if (fmod(x + y, 2) < 1)
                            clip(-1);
                    }
                    else if (a < 0.75)
                    {
                        if (fmod(x, 2) < 1 && fmod(y, 2) < 1)
                            clip(-1);
                    }
                }
            }

            v2f vert (appdata_t v)  
            {  
                v2f o;  
                o.pos = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.pos);    
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);  

                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                float3 i = o.worldPos -_WorldSpaceCameraPos.xyz;
                float3 n = mul((float3x3)unity_ObjectToWorld, v.normal);
                n = normalize(n);
                o.r = Reflect(i, n);

                TRANSFER_SHADOW(o);
                return o;  
            }  

            fixed4 frag (v2f i) : SV_Target  
            {
                float2 xy;

                if (_State == 2)
                {
                    xy = float2(i.screenPos.x * _ScreenParams.x, i.screenPos.y * _ScreenParams.y);
                    PixelJitter(xy, _CellDensity, false);
                }
                else if (_State == 3)
                {
                    xy = float2(i.screenPos.x * _ScreenParams.x, i.screenPos.y * _ScreenParams.y);
                    PixelJitter(xy, _CellDensity, true);
                }

                fixed3 worldNormal = normalize(i.worldNormal);
                fixed3 worldLightDir = normalize(_WorldSpaceLightPos0.xyz);
                
                fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.xyz;

                fixed3 diffuse = _LightColor0.rgb * _Diffuse.rgb * max(0, dot(worldNormal, worldLightDir));

                fixed3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos.xyz);
                fixed3 halfDir = normalize(worldLightDir + viewDir);
                fixed3 specular = _LightColor0.rgb * _Specular.rgb * pow(max(0, dot(worldNormal, halfDir)), _Gloss);

                fixed atten = 1.0;

                fixed shadow = SHADOW_ATTENUATION(i);

                fixed4 col = tex2D(_MainTex, i.texcoord) * _MainTint;
                float4 reflectiveColor = texCUBE(_Environment, i.r);

                col.rgb = pow(col.rgb, 1 - _SpecPower);
                col = col * fixed4(ambient + (diffuse + specular) * atten * shadow, 1.0);

                // 隐身
                if (_State == 4)
                    return reflectiveColor * _Reflectivity;
                else
                    return col;
            }
            ENDCG 
        }

        Pass {
            // Pass for other pixel lights
            Tags { "LightMode"="ForwardAdd" }
            
            Cull Back
            Blend One One
        
            CGPROGRAM
            
            // Apparently need to add this declaration
            #pragma multi_compile_fwdadd
            // Use the line below to add shadows for point and spot lights
//          #pragma multi_compile_fwdadd_fullshadows
            
            #pragma vertex vert
            #pragma fragment frag

            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            fixed4 _Diffuse;
            fixed4 _Specular;
            float _Gloss;

            struct a2v {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f {
                float4 position : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert(a2v v) {
                v2f o;
                o.position = UnityObjectToClipPos(v.vertex);
                
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                fixed3 worldNormal = normalize(i.worldNormal);
                #ifdef USING_DIRECTIONAL_LIGHT
                    fixed3 worldLightDir = normalize(_WorldSpaceLightPos0.xyz);
                #else
                    fixed3 worldLightDir = normalize(_WorldSpaceLightPos0.xyz - i.worldPos.xyz);
                #endif

                fixed3 diffuse = _LightColor0.rgb * _Diffuse.rgb * max(0, dot(worldNormal, worldLightDir));

                fixed3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos.xyz);
                fixed3 halfDir = normalize(worldLightDir + viewDir);
                fixed3 specular = _LightColor0.rgb * _Specular.rgb * pow(max(0, dot(worldNormal, halfDir)), _Gloss);
                
                #ifdef USING_DIRECTIONAL_LIGHT
                    fixed atten = 1.0;
                #else
                    #if defined (POINT)
                        float3 lightCoord = mul(unity_WorldToLight, float4(i.worldPos, 1)).xyz;
                        fixed atten = tex2D(_LightTexture0, dot(lightCoord, lightCoord).rr).UNITY_ATTEN_CHANNEL;
                    #elif defined (SPOT)
                        float4 lightCoord = mul(unity_WorldToLight, float4(i.worldPos, 1));
                        fixed atten = (lightCoord.z > 0) * tex2D(_LightTexture0, lightCoord.xy / lightCoord.w + 0.5).w * tex2D(_LightTextureB0, dot(lightCoord, lightCoord).rr).UNITY_ATTEN_CHANNEL;
                    #else
                        fixed atten = 1.0;
                    #endif
                #endif

                return fixed4((diffuse + specular) * atten, 1.0);
            }

            ENDCG
        }
    }
    FallBack "Specular" 
}  
