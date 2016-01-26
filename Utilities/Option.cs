using System;

namespace ShaderBaker.Utilities
{

public struct Option<T> where T : class
{
    public static Option<T> Some(T value)
    {
        if (value == null)
        {
            throw new NullReferenceException("Cannot make an Option from a null value");
        }

        return new Option<T>(value);
    }
    
    public static Option<T> None()
    {
        return new Option<T>(null);
    }
    
    private readonly T value;
    public T Value
    {
        get
        {
            if (IsNone)
            {
                throw new NullReferenceException("Cannot get value from an Option of type None");
            }
            return value;
        }
    }

    public bool IsSome => value != null;

    public bool IsNone => value == null;
    
    private Option(T value)
    {
        this.value = value;
    }
}

}
