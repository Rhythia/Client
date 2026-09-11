using System;

public class BindableEventArgs<T>(T value, T oldValue) : EventArgs
{
    public T Value = value;

    public T OldValue = oldValue;
}
