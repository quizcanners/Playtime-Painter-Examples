Shader "Playtime Painter/Geometry/Bevel/Simple" {
	Properties{
		_MainTex("Base texture", 2D) = "white" {}
		_BackColor("Back Color", Color) = (0.5,0.5,0.5,0)
		_EdgeColor("Edge Color", Color) = (1,1,1,0)
	}

	Category{
		SubShader{

			Tags{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"LightMode" = "ForwardBase"
			//"DisableBatching" = "True"
			"Solution" = "Bevel"
			}

			Blend SrcAlpha OneMinusSrcAlpha

			Pass{

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_instancing
				#include "Assets/The-Fire-Below/Common/Shaders/quizcanners_cg.cginc"

				sampler2D _MainTex;
				float4 _MainTex_TexelSize;
				float4 _MainTex_ST;

				float4 _BackColor;
				float4 _EdgeColor;

				struct v2f {
					float4 pos : SV_POSITION;
					float4 vcol : COLOR0;
					float3 worldPos : TEXCOORD0;
					float3 normal : TEXCOORD1;
					float2 texcoord : TEXCOORD2;
					float4 edge : TEXCOORD3;
					float3 snormal: TEXCOORD4;
					//SHADOW_COORDS(5)
					float3 viewDir: TEXCOORD6;
					float3 edgeNorm0 : TEXCOORD7;
					float3 edgeNorm1 : TEXCOORD8;
					float3 edgeNorm2 : TEXCOORD9;

				};

				v2f vert(appdata_full_qc v) {
					v2f o;
					   UNITY_SETUP_INSTANCE_ID(v);
					o.pos = UnityObjectToClipPos(v.vertex);
					o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
					o.normal.xyz = UnityObjectToWorldNormal(v.normal);

					o.texcoord.xy = v.texcoord.xy;
					o.vcol = v.color;
					o.edge = float4(v.texcoord1.w, v.texcoord2.w, v.texcoord3.w, v.texcoord.w); //v.texcoord1;
					o.viewDir.xyz = WorldSpaceViewDir(v.vertex);

					float3 deEdge = 1 - o.edge.xyz;

					o.edgeNorm0 = UnityObjectToWorldNormal(v.texcoord1.xyz);
					o.edgeNorm1 = UnityObjectToWorldNormal(v.texcoord2.xyz);
					o.edgeNorm2 = UnityObjectToWorldNormal(v.texcoord3.xyz);

					o.snormal.xyz = normalize(o.edgeNorm0*deEdge.x + o.edgeNorm1*deEdge.y + o.edgeNorm2*deEdge.z);

					//TRANSFER_SHADOW(o);

					return o;
				}


				float4 frag(v2f i) : SV_Target {

					i.viewDir.xyz = normalize(i.viewDir.xyz);

					float4 col = tex2D(_MainTex, TRANSFORM_TEX(i.texcoord, _MainTex));
					col.a = 1;

					// Making Smooth Normal
					float weight;
					float3 normal = DetectSmoothEdge(i.edge, i.normal.xyz, i.snormal.xyz, i.edgeNorm0, i.edgeNorm1, i.edgeNorm2, weight); 

					// Adding Color to edges if Edge Color alpha>0
					weight *= _EdgeColor.a;
					float deWeight = 1 - weight;
					col.rgb = col.rgb*deWeight + _EdgeColor.rgb*weight;

					// Adding Color to sides
					float fresnel = saturate(dot(i.viewDir.xyz, normal));
					col = col * fresnel + _BackColor * (1-fresnel);

					return col;

				}
				ENDCG
			}
			UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
		}
	}
}