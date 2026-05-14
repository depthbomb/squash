namespace Squash.Lib;

public sealed class Flag : ResettableValue<bool>
{
    public Flag(bool value) : base(value) {}

    public bool IsTrue() => Value;

    public bool IsFalse() => !Value;

    public void SetTrue() => SetValue(true);

    public void SetFalse() => SetValue(false);
    
    public void Toggle() => SetValue(!Value);
    
    public static implicit operator bool(Flag flag) => flag.Value;
}
