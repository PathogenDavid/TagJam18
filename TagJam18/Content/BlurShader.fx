// Based on http://developer.download.nvidia.com/shaderlibrary/webpages/hlsl_shaders.html#post_radialBlur
// (I couldn't use it directly due to infinite issues getting txfxc to take the shader.)

#include "Structures.fxh"

#define NumSamples 16

float BlurStart = 1.f;
float BlurWidth = -0.2f;
float2 Center = float2(0.5f, 0.5f);

Texture2D RenderTargetTexture : register(t0);
SamplerState TextureSampler : register(s0);

// Copy+pasted from SharpDX BasicEffect.fx
VSOutputTx VSMain(VSInputTx vin)
{
    VSOutputTx vout;

    vout.PositionPS = vin.Position;
    vout.TexCoord = vin.TexCoord - Center;
    vout.Diffuse = 0.f;
    vout.Specular = 0.f;

    return vout;
}

float4 PSMain(PSInputTx input) : SV_TARGET
{
    float4 ret = 0.f;

    for (int i = 0; i < NumSamples; i++)
    {
        float scale = BlurStart + BlurWidth * (i / (float)(NumSamples - 1.0));
        ret += RenderTargetTexture.Sample(TextureSampler, input.TexCoord * scale + Center);
    }

    ret /= NumSamples;
    ret.a = 1.f;
    return ret;
}

technique
{
    pass
    {
        Profile = 10.0;
        VertexShader = VSMain;
        PixelShader = PSMain;
    }
}
