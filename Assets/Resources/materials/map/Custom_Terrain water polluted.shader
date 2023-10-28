Shader "Custom/Terrain water polluted" {
	Properties {
		_MainTex ("Main texture", 2D) = "white" {}
		_RippleTex ("Ripple texture", 2D) = "white" {}
		_BurnTex ("Burn texture", 2D) = "white" {}
		_BurnColor ("BurnColor", Vector) = (1,1,1,1)
		_Color ("Color", Vector) = (1,1,1,1)
		_AlphaAddTex ("Alpha add texture", 2D) = "" {}
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200
		CGPROGRAM
#pragma surface surf Standard
#pragma target 3.0

		sampler2D _MainTex;
		fixed4 _Color;
		struct Input
		{
			float2 uv_MainTex;
		};
		
		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	}
}