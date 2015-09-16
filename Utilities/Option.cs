using System;

namespace ShaderBaker.Utilities
{

public class Option<T> where T : class
{
    public static Option<T> empty()
    {
        return new Option<T>(null);
    }

    public static Option<T> of(T value)
    {
        if (value == null)
        {
            throw new NullReferenceException("Cannot make an Option from a null value");
        }

        return new Option<T>(value);
    }
    
    private readonly T value;
    
    private Option(T value)
    {
        this.value = value;
    }

    public bool hasValue()
    {
        return this.value != null;
    }

    public T get()
    {
        if (value == null)
        {
            throw new NullReferenceException("Cannot get value from an empty Option");
        }
        return value;
    }
}

}
