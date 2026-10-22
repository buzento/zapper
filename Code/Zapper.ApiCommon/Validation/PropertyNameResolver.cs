using System.Linq.Expressions;
using System.Reflection;

namespace Zapper.ApiCommon.Validation;

public static class PropertyNameResolver
{
    public static string Resolve(Type type, MemberInfo? memberInfo, LambdaExpression? expression)
    {
        var name = memberInfo?.Name;

        return name is null or { Length: 0 }
            ? name ?? string.Empty
            : char.ToLowerInvariant(name[0]) + name[1..];
    }
}
