namespace BBTAG_IM.ImprovementMod.Graphics;

internal static class ScreenStates
{
    private const uint Off = 0;
    private const uint On = 1;
    private const uint SolidFill = 3;
    private const uint NoCulling = 1;
    private const uint SourceAlpha = 5;
    private const uint InverseSourceAlpha = 6;
    private const uint AddBlend = 1;
    private const uint AllColorChannels = 0xF;
    private const uint SelectFirstArgument = 2;
    private const uint Modulate = 4;
    private const uint DiffuseArgument = 0;
    private const uint TextureArgument = 2;
    private const uint DisableStage = 1;
    private const uint LinearFilter = 2;

    public static void PrepareColored(Direct3DDevice device, uint vertexFormat)
    {
        PrepareCommon(device, vertexFormat);
        device.SetTexture(0, 0);
        device.SetRenderState(RenderState.ScissorTestEnable, Off);
        device.SetTextureStageState(0, TextureStageState.ColorOperation, SelectFirstArgument);
        device.SetTextureStageState(0, TextureStageState.ColorArgument1, DiffuseArgument);
        device.SetTextureStageState(0, TextureStageState.AlphaOperation, SelectFirstArgument);
        device.SetTextureStageState(0, TextureStageState.AlphaArgument1, DiffuseArgument);
    }

    public static void PrepareTextured(Direct3DDevice device, uint vertexFormat)
    {
        PrepareCommon(device, vertexFormat);
        device.SetRenderState(RenderState.ScissorTestEnable, On);
        device.SetTextureStageState(0, TextureStageState.ColorOperation, Modulate);
        device.SetTextureStageState(0, TextureStageState.ColorArgument1, TextureArgument);
        device.SetTextureStageState(0, TextureStageState.ColorArgument2, DiffuseArgument);
        device.SetTextureStageState(0, TextureStageState.AlphaOperation, Modulate);
        device.SetTextureStageState(0, TextureStageState.AlphaArgument1, TextureArgument);
        device.SetTextureStageState(0, TextureStageState.AlphaArgument2, DiffuseArgument);
        device.SetSamplerState(0, SamplerState.MinificationFilter, LinearFilter);
        device.SetSamplerState(0, SamplerState.MagnificationFilter, LinearFilter);
    }

    private static void PrepareCommon(Direct3DDevice device, uint vertexFormat)
    {
        device.ClearShaders();
        device.SetVertexFormat(vertexFormat);
        device.SetRenderState(RenderState.ZEnable, Off);
        device.SetRenderState(RenderState.FillMode, SolidFill);
        device.SetRenderState(RenderState.ZWriteEnable, Off);
        device.SetRenderState(RenderState.AlphaTestEnable, Off);
        device.SetRenderState(RenderState.SourceBlend, SourceAlpha);
        device.SetRenderState(RenderState.DestinationBlend, InverseSourceAlpha);
        device.SetRenderState(RenderState.CullMode, NoCulling);
        device.SetRenderState(RenderState.AlphaBlendEnable, On);
        device.SetRenderState(RenderState.FogEnable, Off);
        device.SetRenderState(RenderState.StencilEnable, Off);
        device.SetRenderState(RenderState.Lighting, Off);
        device.SetRenderState(RenderState.ClipPlaneEnable, Off);
        device.SetRenderState(RenderState.ColorWriteEnable, AllColorChannels);
        device.SetRenderState(RenderState.BlendOperation, AddBlend);
        device.SetRenderState(RenderState.SrgbWriteEnable, Off);
        device.SetRenderState(RenderState.SeparateAlphaBlendEnable, Off);
        device.SetTextureStageState(1, TextureStageState.ColorOperation, DisableStage);
        device.SetTextureStageState(1, TextureStageState.AlphaOperation, DisableStage);
    }

    private static class RenderState
    {
        public const int ZEnable = 7;
        public const int FillMode = 8;
        public const int ZWriteEnable = 14;
        public const int AlphaTestEnable = 15;
        public const int SourceBlend = 19;
        public const int DestinationBlend = 20;
        public const int CullMode = 22;
        public const int AlphaBlendEnable = 27;
        public const int FogEnable = 28;
        public const int StencilEnable = 52;
        public const int Lighting = 137;
        public const int ClipPlaneEnable = 152;
        public const int ColorWriteEnable = 168;
        public const int BlendOperation = 171;
        public const int ScissorTestEnable = 174;
        public const int SrgbWriteEnable = 194;
        public const int SeparateAlphaBlendEnable = 206;
    }

    private static class TextureStageState
    {
        public const int ColorOperation = 1;
        public const int ColorArgument1 = 2;
        public const int ColorArgument2 = 3;
        public const int AlphaOperation = 4;
        public const int AlphaArgument1 = 5;
        public const int AlphaArgument2 = 6;
    }

    private static class SamplerState
    {
        public const int MagnificationFilter = 5;
        public const int MinificationFilter = 6;
    }
}
