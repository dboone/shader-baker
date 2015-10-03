namespace Svg2Ico
{

struct IcoResolution
{
    public readonly int Width;

    public byte IcoHeaderWidth
    {
        get
        {
            return (byte) Width;
        }
    }

    public readonly int Height;

    public byte IcoHeaderHeight
    {
        get
        {
            return (byte) Height;
        }
    }
    
    public IcoResolution(IcoSize size)
    {
        Width = size.Value;
        Height = size.Value;
    }
    
    public IcoResolution(IcoSize width, IcoSize height)
    {
        Width = width.Value;
        Height = height.Value;
    }
}

}
