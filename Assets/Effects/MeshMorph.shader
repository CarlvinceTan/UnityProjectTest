// Shader "CustomShaders/MeshMorph"
// {
//     Properties
//     {
//         _MainTex ("Main Texture", 2D) = "white" {}
//         _DisplacementMap ("Displacement Map", 2D) = "black" {}
//         _DisplacementScale ("Displacement Scale", Range(0, 5)) = 1.0
//         _MaxBulge ("Max Bulge", Range(0, 3)) = 1.0
        
//         // Color settings
//         _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
//         _BulgeColor ("Bulge Color", Color) = (1, 0, 0, 1)
//         _BulgeColorIntensity ("Bulge Color Intensity", Range(0, 2)) = 1.0
//         _ColorTransition ("Color Transition Sharpness", Range(0.1, 5)) = 2.0
//     }

//     SubShader
//     {
//         Tags { "RenderType"="Opaque" }
//         LOD 200

//         Pass
//         {
//             CGPROGRAM
//             #pragma vertex vert
//             #pragma fragment frag
//             #pragma target 3.0
            
//             #include "UnityCG.cginc"

//             // Properties
//             sampler2D _MainTex;
//             float4 _MainTex_ST;
//             sampler2D _DisplacementMap;
//             float _DisplacementScale;
//             float _MaxBulge;
//             float4 _BaseColor;
//             float4 _BulgeColor;
//             float _BulgeColorIntensity;
//             float _ColorTransition;

//             struct appdata
//             {
//                 float4 vertex : POSITION;
//                 float3 normal : NORMAL;
//                 float2 uv : TEXCOORD0;
//             };

//             struct v2f
//             {
//                 float4 pos : SV_POSITION;
//                 float2 uv : TEXCOORD0;
//                 float bulgeAmount : TEXCOORD1;
//                 float3 worldNormal : TEXCOORD2;
//             };

//             v2f vert(appdata v)
//             {
//                 v2f o;
                
//                 // Sample bulge field
//                 float bulge = tex2Dlod(_DisplacementMap, float4(v.uv, 0, 0)).r;
                
//                 // Normalize bulge for smooth visual effect
//                 float normalizedBulge = saturate(bulge / _MaxBulge);
                
//                 // Apply smooth displacement along normal
//                 // Use a power function for more natural-looking bulging
//                 float displacement = pow(normalizedBulge, 0.7) * _DisplacementScale;
                
//                 // Displace vertex along its normal
//                 float3 displacedPos = v.vertex.xyz + v.normal * displacement;
                
//                 // Transform to clip space
//                 o.pos = UnityObjectToClipPos(float4(displacedPos, 1.0));
//                 o.uv = TRANSFORM_TEX(v.uv, _MainTex);
//                 o.bulgeAmount = normalizedBulge;
//                 o.worldNormal = UnityObjectToWorldNormal(v.normal);
                
//                 return o;
//             }

//             float4 frag(v2f i) : SV_Target
//             {
//                 // Sample base texture
//                 float4 baseColor = tex2D(_MainTex, i.uv) * _BaseColor;
                
//                 // Calculate bulge intensity for coloring
//                 // Apply power function for sharper transition
//                 float bulgeIntensity = pow(saturate(i.bulgeAmount), _ColorTransition);
                
//                 // Blend between base color and red bulge color
//                 float4 bulgeColor = lerp(baseColor, _BulgeColor, bulgeIntensity * _BulgeColorIntensity);
                
//                 // Add subtle rim lighting effect on highly bulged areas
//                 float3 viewDir = normalize(_WorldSpaceCameraPos - mul(unity_ObjectToWorld, float4(0,0,0,1)).xyz);
//                 float rimLight = 1.0 - saturate(dot(i.worldNormal, viewDir));
//                 rimLight = pow(rimLight, 3.0) * bulgeIntensity * 0.3;
                
//                 bulgeColor.rgb += rimLight * _BulgeColor.rgb;
                
//                 return bulgeColor;
//             }
//             ENDCG
//         }
//     }
    
//     FallBack "Diffuse"
// }
