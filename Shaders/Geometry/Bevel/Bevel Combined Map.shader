Shader "Playtime Painter/Geometry/Bevel/Combined Map" {
	Properties{
		_MainTex("Base texture", 2D) = "white" {}
		[KeywordEnum(None, Regular, Combined)] _BUMP("Combined Map", Float) = 0
		_Map("Bump/Combined Map (or None)", 2D) = "gray" {}
	}

	Category{
		SubShader{

			Tags{
			"Queue" = "Geometry"
			"IgnoreProjector" = "True"
			"RenderType" = "Opaque"
			"LightMode" = "ForwardBase"
			"DisableBatching" = "True"
			"Solution" = "Bevel"
			}

			Pass{

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fwdbase
				#pragma target 3.0
				#include "UnityLightingCommon.cginc" 
				#include "Lighting.cginc"
				#include "UnityCG.cginc"
				#include "AutoLight.cginc"
				#include "Assets/The-Fire-Below/Common/Shaders/quizcanners_built_in.cginc"

				#pragma shader_feature_local  ___ _BUMP_NONE  _BUMP_COMBINED 

				sampler2D _MainTex;
				float4 _MainTex_TexelSize;
				float4 _MainTex_ST;
				sampler2D _Map;
				float4 _Map_ST;

				struct v2f {
					float4 pos : SV_POSITION;
					float4 vcol : COLOR0;
					float3 worldPos : TEXCOORD0;
					float3 normal : TEXCOORD1;
					float2 texcoord : TEXCOORD2;
					float4 edge : TEXCOORD3;
					float3 snormal: TEXCOORD4;
					SHADOW_COORDS(5)
					float3 viewDir: TEXCOORD6;
					float3 edgeNorm0 : TEXCOORD7;
					float3 edgeNorm1 : TEXCOORD8;
					float3 edgeNorm2 : TEXCOORD9;
					float4 wTangent : TEXCOORD10;

				};

				v2f vert(appdata_full v) {
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);
					o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
					o.normal.xyz = UnityObjectToWorldNormal(v.normal);

					o.wTangent.xyz = UnityObjectToWorldDir(v.tangent.xyz);
					o.wTangent.w = v.tangent.w * unity_WorldTransformParams.w;

					o.texcoord = v.texcoord.xy;
					o.vcol = v.color;
					o.edge = float4(v.texcoord1.w, v.texcoord2.w, v.texcoord3.w, v.texcoord.w); //v.texcoord1;
					o.viewDir.xyz = WorldSpaceViewDir(v.vertex);

					float3 deEdge = 1 - o.edge.xyz;

					o.edgeNorm0 = UnityObjectToWorldNormal(v.texcoord1.xyz);
					o.edgeNorm1 = UnityObjectToWorldNormal(v.texcoord2.xyz);
					o.edgeNorm2 = UnityObjectToWorldNormal(v.texcoord3.xyz);

					o.snormal.xyz = normalize(o.edgeNorm0*deEdge.x + o.edgeNorm1*deEdge.y + o.edgeNorm2*deEdge.z);

					TRANSFER_SHADOW(o);

					return o;
				}


				float4 frag(v2f i) : SV_Target {

					i.viewDir.xyz = normalize(i.viewDir.xyz);

					float2 uv = TRANSFORM_TEX(i.texcoord, _MainTex);

					float4 col = tex2D(_MainTex, uv);

					float4 colMip = tex2Dlod(_MainTex, float4(uv, 0 , 12));
					
					float weight;
					float3 normal = DetectSmoothEdge(i.edge, i.normal.xyz, i.snormal.xyz, i.edgeNorm0, i.edgeNorm1, i.edgeNorm2, weight); 

					float edgeColorVisibility = smoothstep(0.9, 1, i.vcol.a) * weight;

					col.rgb = col.rgb*(1-edgeColorVisibility) + colMip.rgb * edgeColorVisibility; //i.vcol.rgb*edgeColorVisibility;

					float3 preNorm = normal;

					#if _BUMP_NONE
						float4 bumpMap = DEFAULT_COMBINED_MAP;
						float4 bumpMapMip = DEFAULT_COMBINED_MAP;
					#else
						float4 bumpMap = tex2D(_Map, i.texcoord.xy);
						float4 bumpMapMip = tex2Dlod(_Map, float4(i.texcoord.xy, 0 ,12));
						float3 tnormal;
						#if _BUMP_REGULAR
							tnormal = UnpackNormal(bumpMap);
							bumpMap = DEFAULT_COMBINED_MAP;
						#else
							bumpMap.rg = (bumpMap.rg - 0.5) * 2;
							tnormal = float3(bumpMap.r, bumpMap.g, 1);
						#endif

						ApplyTangent (normal, tnormal,  i.wTangent);
						
						bumpMap = bumpMap*(1-edgeColorVisibility) + bumpMapMip * edgeColorVisibility; //i.vcol.rgb*edgeColorVisibility;	
					#endif

					float deWeight = 1 - weight;

					normal = normalize(normal*deWeight + preNorm*weight);

					float shadow = SHADOW_ATTENUATION(i);

					float ambient = bumpMap.a;

					float smoothness =  bumpMap.b ;

					Combined_Light(col, ambient, smoothness, normal, i.viewDir.xyz, shadow);

					//BleedAndBrightness(col, 1, i.texcoord.xy);
				
					return saturate(col);

				}
				ENDCG
			}
			UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
		}
	}
}