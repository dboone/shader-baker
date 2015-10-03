namespace Svg2Ico
{

class IcoSize
{
    public readonly int Value;

    public static bool TryGetInstance(int value, out IcoSize size)
    {
        if (value < 0 || value > 256)
        {
            size = null;
            return false;
        } else
        {
            size = new IcoSize(value);
            return true;
        }
    }

    private IcoSize(int value)
    {
        Value = value;
    }
}

}
