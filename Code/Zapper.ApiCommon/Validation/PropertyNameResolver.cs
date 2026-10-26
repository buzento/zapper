using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Zapper.ApiCommon.Validation;

public static class PropertyNameResolver
{
    public static string Resolve(Type type, MemberInfo? memberInfo, LambdaExpression? expression)
    {
        // a provider's notification names its fields as the provider sends them, BackendCode.md section 8
        var wireName = memberInfo?.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name;

        if (wireName is not null)
        {
            return wireName;
        }

        var name = memberInfo?.Name;

        return name is null or { Length: 0 }
            ? name ?? string.Empty
            : char.ToLowerInvariant(name[0]) + name[1..];
    }
}
