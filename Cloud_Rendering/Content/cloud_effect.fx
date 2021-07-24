#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_5_0
	#define PS_SHADERMODEL ps_5_0
#endif

matrix WorldViewProjection;
matrix World;
float3 EyePosition;
float3 LightDir = float3(0, 1, 0);
float3 bgcolor;
float currenttime;

float3 b_max;
float3 b_min;

Texture3D perlintex;
SamplerState perlinsamp
{
	Texture = (perlintex);
	MAGFILTER = Anisotropic;
	MINFILTER = Anisotropic;
	MIPFILTER = Anisotropic;
	AddressU = Wrap;
	AddressV = Wrap;
};

struct VertexShaderInput
{
	float4 Position : SV_POSITION;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float3 WorldPosition : TEXCOORD0;
};

float2 DistInRectangle(float3 b_min, float3 b_max, float3 r_dir_inv, float3 r_ori)
{
	float t1 = (b_min.x - r_ori.x)*r_dir_inv.x;
	float t2 = (b_max.x - r_ori.x)*r_dir_inv.x;

	float tmin = min(t1, t2);
	float tmax = max(t1, t2);

	t1 = (b_min.y - r_ori.y)*r_dir_inv.y;
	t2 = (b_max.y - r_ori.y)*r_dir_inv.y;

	tmin = max(tmin, min(t1, t2));
	tmax = min(tmax, max(t1, t2));

	t1 = (b_min.z - r_ori.z)*r_dir_inv.z;
	t2 = (b_max.z - r_ori.z)*r_dir_inv.z;

	tmin = max(tmin, min(t1, t2));
	tmax = min(tmax, max(t1, t2));

	tmin = max(tmin, 0);

	return float2(tmax - tmin, tmin);
}
inline float speedperlin3D2(float x, float y, float z)
{
	return perlintex.SampleLevel(perlinsamp, float3(x, y, z) * 0.003922f, 0);
}
float fade(float t)
{
	// Fade function as defined by Ken Perlin.  This eases coordinate values
	// so that they will "ease" towards integral values.  This ends up smoothing
	// the final output.
	return (t * t * t * (t * (t * 6 - 15) + 10));         // 6t^5 - 15t^4 + 10t^3
}

float hg(float a, float g) {
	float g2 = g*g;
	return (1 - g2) / (4 * 3.1415*pow(1 + g2 - 2 * g*(a), 1.5));
}
float phase(float a) {
	float4 phaseParams = float4(0.83f, 0.3f, 1.0, 1.488f);
	float blend = 0.5;
	float hgBlend = hg(a, phaseParams.x) * (1 - blend) + hg(a, -phaseParams.y) * blend;
	return phaseParams.z + hgBlend*phaseParams.w;
}
inline float speedOctavePerlin3D(float x, float y, float z, int octaves, float persistence)
{
	float total = 0;
	float total2 = 0;
	float maxValue = 0;
	float frequency = 0.003f;
	float amplitude = 1;
	//float biome = (speedperlin3D2(x * 0.015f + 100.0f - currenttime * 0.02f, y * 0.015f + 100.0f + currenttime * 0.02f, z * 0.015f + 100.0f + currenttime * 0.2f) + 1) * 0.5f;
	//float columbusstrength = speedperlin3D2(100.0f + 0.2f * (x + currenttime), 100.0f + 0.2f * (y - currenttime), 100.0f + 0.2f * (z + currenttime)) * 1.6f - 0.5f;
	//float bigcloudstrength = speedperlin3D2(x * 0.05f + 100.0f + currenttime * 0.2f, y * 0.05f + 100.0f - currenttime * 0.2f, z * 0.05f + 100.0f + currenttime * 0.2f) * 1.4f - 0.3f;
	amplitude *= 0.75f;
	for (int i = 0; i < octaves; ++i)
	{
		total += speedperlin3D2((x + currenttime) * frequency + currenttime * 0.01f, (y + currenttime * 0.05f) * frequency, (z + currenttime) * frequency + currenttime * 0.01f) * amplitude;
		total2 += amplitude;
		amplitude *= persistence;
		frequency += frequency;
	}
	return total * fade(clamp((b_max.y - y) * 0.00075f, 0, 1) * clamp((y - b_min.y) * 0.005f, 0, 1));
}

VertexShaderOutput MainVS(in VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;

	output.Position = mul(input.Position, WorldViewProjection);
	output.WorldPosition = mul(input.Position, World);

	return output;
}

float march2light(float3 pos)
{
	float2 distBox_Info = DistInRectangle(b_min, b_max, 1 / LightDir, pos);
	float distInBox = distBox_Info.x;
	float distToBox = distBox_Info.y;



	int samples = 20;

	float dens = 0.0f;
	float incr_dist = min(max(distInBox, 600), 6000) / samples;
	float3 curpos = pos + LightDir * incr_dist * 0.5f;
	for (int i = 0; i < samples; ++i)
	{
		float cur_dens = max((speedOctavePerlin3D(curpos.x, curpos.y, curpos.z, 5, 0.4) - 0.15f) * 0.9f, 0);
		dens += cur_dens * incr_dist;
		curpos += LightDir * incr_dist;
	}
	return 0.15f + exp(-dens * 0.15f) * 0.85f;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	float3 dir = normalize(input.WorldPosition - EyePosition);
	float3 dir_inv = 1 / dir;
	float dist = length(dir);
	float2 distBox_Info = DistInRectangle(b_min, b_max, dir_inv, EyePosition);
	float distInBox = distBox_Info.x;
	float distToBox = distBox_Info.y;

	//int samples = 200;
	float cosAngle = dot(dir, LightDir);
	float phaseVal = phase(cosAngle);

	float3 curpos = EyePosition + dir * distToBox;
	float curdist = 0, finaldens = 0, trans = 1.0, finallight = 0.0f;
	//float3 incr_dir = (dir * distInBox) / samples;
	float incr_dist = 8 * (max(distToBox, 2000) / 2000.0f);// (distInBox) / samples;
	if(EyePosition.x > b_min.x && EyePosition.x < b_max.x && EyePosition.y > b_min.y && EyePosition.y < b_max.y && EyePosition.z > b_min.z && EyePosition.z < b_max.z)
		incr_dist = 6 * (max(distToBox, 2000) / 2000.0f);
	int count = 0;
	while(1)
	{
		float cur_dens = (speedOctavePerlin3D(curpos.x, curpos.y, curpos.z, 6, 0.45) - 0.15f) * 0.9f;
		[branch]if (cur_dens > 0)
		{
			float lighttrans = march2light(curpos);
			trans *= exp(-cur_dens * incr_dist * 0.85f);
			finallight += cur_dens * trans * lighttrans * incr_dist * phaseVal * 0.7;
			//finaldens += cur_dens * incr_dist * 0.01f;
			curdist += incr_dist;
			curpos += dir * incr_dist;
		}
		else
		{
			curdist += incr_dist * (1 + (-cur_dens * 45));
			curpos += dir * incr_dist * (1 + (-cur_dens * 45));
		}
		count++;
		if (curdist > distInBox || trans < 0.04f || count > 150)
			break;
	}

	float sun = saturate(hg(dot(LightDir, dir), .999)) * trans;

	float3 LightCol = float3(1, 1, 1);
	float3 col = bgcolor * trans + LightCol * finallight;
	col = col * (1 - sun) + LightCol * sun;
	//finaltrans = exp(-finaldens);
	//float finalval = 1 - finaltrans;
	return float4(col, 1); // count * 0.005, count * 0.005, count * 0.005
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};