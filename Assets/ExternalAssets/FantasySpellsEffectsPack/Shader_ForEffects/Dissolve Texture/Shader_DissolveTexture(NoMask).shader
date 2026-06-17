Shader "GAPH Custom Shader/Dissolve Texture/Dissolve Texture(NoMask)"{
	Properties{
		[HDR]_TintColor("Tint Color",Color) = (0,0,0,0)
        _CutOut("CutOut",Range(0,1)) = 1
		_MainTex("Main Texture",2D) = "white"{}
		_NormalTex("Normal Texture", 2D) = "black"{}
		_Speed("Offset Speed",Float) = 1
        _ColorStrength("Color Strength",Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)]_BlendSrc("BlendSrc", float) = 1 //Blend mode start
		[Enum(UnityEngine.Rendering.BlendMode)]_BlendDst("BlendDst", float) = 1 //Blend mode end
	}

		Category{

			Tags{ "Queue" = "Transparent"  "IgnoreProjector" = "True"  "RenderType" = "Transparent" }
			Blend [_BlendSrc] [_BlendDst]
			Cull Off
			Lighting Off
			ZWrite Off

		SubShader{
				Pass{
				Name "BASE"

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				#include "UnityCG.cginc"

				sampler2D _MainTex;
				sampler2D _NormalTex;

				half4 _TintColor;
                half _CutOut;
                half _Speed;
                half _ColorStrength;

				struct appdata_t {
					float4 vertex : POSITION;
					half4 color : COLOR;
					float2 texcoord : TEXCOORD0;
                    float2 texcoord2 : TEXCOORD1;
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					half4 color : COLOR;
					float2 texcoord : TEXCOORD0;
                    float2 texcoord2 : TEXCOORD1;
					UNITY_FOG_COORDS(2)
				};

				half4 _MainTex_ST;
				half4 _Mask_ST;
				half4 _NormalTex_ST;

				v2f vert(appdata_t v)
				{
					v2f o;

					o.vertex = UnityObjectToClipPos(v.vertex);
					o.color = v.color;
					o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
					o.texcoord2 = TRANSFORM_TEX(v.texcoord, _NormalTex);

					UNITY_TRANSFER_FOG(o, o.vertex);
					return o;
				}

				half4 frag(v2f i) : Color
				{
					half2 distort = UnpackNormal(tex2D(_NormalTex, i.texcoord2)).rg; // Get distort info from normal texture.
                    half4 tex = tex2D(_MainTex, i.texcoord.xy + distort.xy * 1.5 + (_Speed * _Time/5));
					tex *= tex2D(_MainTex, i.texcoord.xy + distort.xy * 1.5 - (_Speed * _Time / 5));
					tex *= tex2D(_MainTex, i.texcoord.xy + distort.xy / 3 - (_Speed * _Time / 5) + float2(0.5f,0.5f));
                    tex = pow(tex,0.4);

					tex = saturate(tex - _CutOut);  

                    half4 res = _ColorStrength * i.color * _TintColor * tex; //Compose main r,g,b source using animate tex
                    half alpha = res.a * (_ColorStrength); //Alpha set
					res.a = saturate(pow(alpha, 1.25))

					UNITY_APPLY_FOG(i.fogCoord, col);
					return res;
				}
				ENDCG
			}
		}
	}
}
