#define PS_PROFILE ps_3_0

float2 CameraPosition;
float Zoom;
float PPU;
float2 ViewportSize;
float CellSize;
float4 ColorA;
float4 ColorB;

struct VertexShaderOutput
{
    float4 Position : SV_Position;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
    // Screen coordinate centered at (0,0)
    float2 screenPos = input.TextureCoordinates * ViewportSize - (ViewportSize * 0.5f);

    // World position calculation (Cartesian inverted Y)
    float2 worldPos;
    worldPos.x = CameraPosition.x + (screenPos.x / (PPU * Zoom));
    worldPos.y = CameraPosition.y - (screenPos.y / (PPU * Zoom));

    // Board parity
    float2 cell = floor(worldPos / CellSize);
    float pattern = fmod(abs(cell.x + cell.y), 2.0);

    return (pattern < 1.0) ? ColorA : ColorB;
}

technique Board
{
    pass Pass0
    {
        PixelShader = compile PS_PROFILE MainPS();
    }
};