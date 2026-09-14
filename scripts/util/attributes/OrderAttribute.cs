using System;
using System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class OrderAttribute([CallerLineNumber] int order = 0) : Attribute
{
    public int Order { get; } = order;
}
