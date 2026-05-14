namespace Squash.Lib;

public class ResettableValue<T>
{
    protected T Value { get; set; }
    
    private readonly T _initialValue;

    public ResettableValue(T value)
    {
        Value = value;
        
        _initialValue = value;
    }

    public void SetValue(T value)
    {
        Value = value;
    }

    public void Reset()
    {
        Value = _initialValue;
    }
}
