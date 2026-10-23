using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;

namespace Mecesoft.Result.Tests.Api;

/// <summary>
/// Produces a stable, human-readable description of the public API surface.
/// The format is intentionally deterministic so it can be checked into source control
/// and compared across releases.
/// </summary>
internal static class PublicApiDump
{
    public static string Dump(Assembly assembly)
    {
        var builder = new StringBuilder();

        foreach (var type in assembly.GetExportedTypes()
                     .OrderBy(type => type.FullName, StringComparer.Ordinal))
        {
            AppendType(builder, type);
        }

        return builder.ToString().Replace("\r\n", "\n").TrimEnd() + "\n";
    }

    public static string Normalize(string text)
    {
        var lines = text.Replace("\r\n", "\n")
            .Split('\n')
            .Where(line => !line.StartsWith("#", StringComparison.Ordinal))
            .Select(line => line.TrimEnd());

        return string.Join("\n", lines).TrimEnd() + "\n";
    }

    private static void AppendType(StringBuilder builder, Type type)
    {
        var kind = type.IsInterface
            ? "interface"
            : type.IsEnum
                ? "enum"
                : type.IsValueType
                    ? "struct"
                    : type.IsAbstract && type.IsSealed
                        ? "static class"
                        : type.IsAbstract
                            ? "abstract class"
                            : type.IsSealed
                                ? "sealed class"
                                : "class";

        builder.Append("TYPE ").Append(TypeName(type)).Append(" [").Append(kind).Append(']');

        if (type.BaseType is not null && type.BaseType != typeof(object))
        {
            builder.Append(" : ").Append(TypeName(type.BaseType));
        }

        var interfaces = type.GetInterfaces()
            .Select(TypeName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        if (interfaces.Count > 0)
        {
            builder.Append(" implements ").Append(string.Join(", ", interfaces));
        }

        builder.AppendLine();

        foreach (var genericParameter in type.GetGenericArguments()
                     .Where(argument => argument.IsGenericParameter))
        {
            builder.Append("  GENERIC ").Append(genericParameter.Name);
            AppendConstraints(builder, genericParameter);
            builder.AppendLine();
        }

        foreach (var constructor in type.GetConstructors(
                         BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                         BindingFlags.DeclaredOnly)
                     .OrderBy(FormatMethodForSort, StringComparer.Ordinal))
        {
            builder.Append("  CTOR (").Append(FormatParameters(constructor.GetParameters())).Append(')');
            builder.AppendLine();
        }

        foreach (var property in type.GetProperties(
                         BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                         BindingFlags.DeclaredOnly)
                     .OrderBy(property => property.Name, StringComparer.Ordinal))
        {
            var setter = property.SetMethod;
            var isInit = setter?.ReturnParameter.GetRequiredCustomModifiers()
                .Any(modifier => modifier.FullName == "System.Runtime.CompilerServices.IsExternalInit") == true;
            var isJsonIgnored = property.GetCustomAttributesData()
                .Any(attribute => attribute.AttributeType == typeof(JsonIgnoreAttribute));

            builder.Append("  PROP ");
            if (property.GetMethod?.IsStatic == true)
            {
                builder.Append("static ");
            }

            builder.Append(TypeName(property.PropertyType)).Append(' ').Append(property.Name);
            builder.Append(" { get; ").Append(isInit ? "init" : "set").Append("; }");
            if (isJsonIgnored)
            {
                builder.Append(" [JsonIgnore]");
            }

            builder.AppendLine();
        }

        foreach (var method in type.GetMethods(
                         BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                         BindingFlags.DeclaredOnly)
                     .Where(method => !method.IsSpecialName ||
                                      method.Name.StartsWith("op_", StringComparison.Ordinal))
                     .OrderBy(FormatMethodForSort, StringComparer.Ordinal))
        {
            builder.Append("  METHOD ");
            if (method.IsStatic)
            {
                builder.Append("static ");
            }

            if (method.GetCustomAttribute<ExtensionAttribute>() is not null)
            {
                builder.Append("[extension] ");
            }

            builder.Append(TypeName(method.ReturnType)).Append(' ').Append(method.Name);
            if (method.IsGenericMethodDefinition)
            {
                builder.Append('<')
                    .Append(string.Join(", ", method.GetGenericArguments().Select(argument => argument.Name)))
                    .Append('>');
            }

            builder.Append('(').Append(FormatParameters(method.GetParameters())).Append(')');

            foreach (var genericParameter in method.GetGenericArguments()
                         .Where(argument => argument.IsGenericParameter))
            {
                builder.Append(" where ").Append(genericParameter.Name);
                AppendConstraints(builder, genericParameter);
            }

            builder.AppendLine();
        }

        foreach (var field in type.GetFields(
                         BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                         BindingFlags.DeclaredOnly)
                     .OrderBy(field => field.Name, StringComparer.Ordinal))
        {
            builder.Append("  FIELD ");
            if (field.IsStatic)
            {
                builder.Append("static ");
            }

            if (field.IsInitOnly)
            {
                builder.Append("readonly ");
            }

            if (field.IsLiteral)
            {
                builder.Append("const ");
            }

            builder.Append(TypeName(field.FieldType)).Append(' ').Append(field.Name);
            if (field.IsLiteral)
            {
                builder.Append(" = ").Append(field.GetRawConstantValue());
            }

            builder.AppendLine();
        }

        foreach (var eventInfo in type.GetEvents(
                         BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                         BindingFlags.DeclaredOnly)
                     .OrderBy(eventInfo => eventInfo.Name, StringComparer.Ordinal))
        {
            builder.Append("  EVENT ").Append(TypeName(eventInfo.EventHandlerType!)).Append(' ')
                .Append(eventInfo.Name).AppendLine();
        }
    }

    private static void AppendConstraints(StringBuilder builder, Type genericParameter)
    {
        var constraints = new List<string>();
        var attributes = genericParameter.GenericParameterAttributes;

        if ((attributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0)
        {
            constraints.Add("class");
        }
        else if ((attributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
        {
            constraints.Add("struct");
        }

        constraints.AddRange(genericParameter.GetGenericParameterConstraints()
            .Select(TypeName)
            .OrderBy(name => name, StringComparer.Ordinal));

        if ((attributes & GenericParameterAttributes.DefaultConstructorConstraint) != 0 &&
            (attributes & GenericParameterAttributes.NotNullableValueTypeConstraint) == 0)
        {
            constraints.Add("new()");
        }

        if (constraints.Count > 0)
        {
            builder.Append(" : ").Append(string.Join(", ", constraints));
        }
    }

    private static string FormatMethodForSort(MethodBase method)
    {
        var parameters = string.Join(", ", method.GetParameters().Select(parameter => TypeName(parameter.ParameterType)));

        if (method is ConstructorInfo)
        {
            return $".ctor({parameters})";
        }

        var methodInfo = (MethodInfo)method;
        return $"{methodInfo.Name}({parameters})";
    }

    private static string FormatParameters(IReadOnlyList<ParameterInfo> parameters)
    {
        return string.Join(", ", parameters.Select(FormatParameter));
    }

    private static string FormatParameter(ParameterInfo parameter)
    {
        var parameterType = parameter.ParameterType;
        var prefix = string.Empty;

        if (parameterType.IsByRef)
        {
            prefix = parameter.IsOut ? "out " : parameter.IsIn ? "in " : "ref ";
            parameterType = parameterType.GetElementType()!;
        }

        if (parameter.GetCustomAttribute<ParamArrayAttribute>() is not null)
        {
            prefix += "params ";
        }

        var optional = parameter.IsOptional ? " = " + FormatDefaultValue(parameter.DefaultValue) : string.Empty;

        return $"{prefix}{TypeName(parameterType)} {parameter.Name}{optional}";
    }

    private static string FormatDefaultValue(object? value)
    {
        if (value is null || value == DBNull.Value || value == Type.Missing)
        {
            return "default";
        }

        if (value is string text)
        {
            return "\"" + text + "\"";
        }

        return value.ToString() ?? "default";
    }

    private static string TypeName(Type type)
    {
        if (type.IsGenericParameter)
        {
            return type.Name;
        }

        if (type.IsArray)
        {
            return TypeName(type.GetElementType()!) + "[]";
        }

        if (type.IsByRef)
        {
            return TypeName(type.GetElementType()!) + "&";
        }

        if (!type.IsGenericType)
        {
            return type.FullName ?? type.Name;
        }

        var definitionName = type.GetGenericTypeDefinition().FullName ?? type.Name;
        var tickIndex = definitionName.IndexOf('`');
        if (tickIndex >= 0)
        {
            definitionName = definitionName[..tickIndex];
        }

        return definitionName + "<" +
               string.Join(", ", type.GetGenericArguments().Select(TypeName)) + ">";
    }
}
