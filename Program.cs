// See https://aka.ms/new-console-template for more information
using System.Linq.Expressions;
using System.Reflection;


var propertyFromDeclaredType = typeof(Parent).GetProperty("Value")!;
var propertyFromDescendantType = typeof(Child).GetProperty("Value")!;

// This is the main reason why it fails.
// because private static PropertyInfo GetProperty(MethodInfo mi, string? paramName, int index = -1) of Expression class
// uses Type? type = mi.DeclaringType; instead of ReflectedType.
// See https://github.com/dotnet/runtime/blob/fc7a8e62c4f0351477fd7423e67e7a541e9c7ca9/src/libraries/System.Linq.Expressions/src/System/Linq/Expressions/MemberExpression.cs#L310
Console.WriteLine($"Metohds equal: {propertyFromDeclaredType.GetMethod!.Equals(propertyFromDescendantType.GetMethod)}");

// This works
propertyFromDeclaredType.GetCompiledGetter();

// This throws
propertyFromDescendantType.GetCompiledGetter();

public class Parent
{
    public int Value { get; set; }
}

public class Child : Parent
{
}

public static class ReflectionExtensions
{
    /// <summary>
    /// Returns compiled getter for readable instance property.
    /// </summary>
    /// <exception cref="ArgumentException">Throws when property is indexer, non-readable, or static.</exception>
    public static Func<object, object> GetCompiledGetter(this PropertyInfo propertyInfo)
    {
        ArgumentNullException.ThrowIfNull(propertyInfo);

        if (!propertyInfo.CanRead)
        {
            throw new ArgumentException($"Property '{propertyInfo.Name}' is not readable.", nameof(propertyInfo));
        }

        if (propertyInfo.GetMethod!.IsStatic)
        {
            throw new ArgumentException($"Property '{propertyInfo.Name}' is static. Must be an instance property.", nameof(propertyInfo));
        }

        if (propertyInfo.GetIndexParameters().Length != 0)
        {
            throw new ArgumentException($"Property '{propertyInfo.Name}' is index property. Must be a regular one.", nameof(propertyInfo));
        }

        var instance = Expression.Parameter(typeof(object), "instance");
        var castedInstance = Expression.Convert(instance, propertyInfo.DeclaringType!);
        var propertyValue = Expression.Property(castedInstance, propertyInfo.GetMethod);
        var propertyValueAsObj = Expression.Convert(propertyValue, typeof(object));
        var lambda = Expression.Lambda<Func<object, object>>(propertyValueAsObj, instance);

        var result = lambda.Compile();

        return result;
    }
}