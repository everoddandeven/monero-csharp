using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Monero.Common;

namespace Monero.UnitTests;

/// <summary>
/// The two ways to read and write the models: with reflection, and with the code that the compiler generates.
/// </summary>
public enum Engine
{
    Reflection,
    SourceGenerated
}

/// <summary>
/// What the tests of the models share: the comparison of JSON, the properties of a model that are in its JSON, and the values that fill a
/// model.
/// </summary>
internal static class ModelTestSupport
{
    /// <summary>
    /// Compare two JSON documents by value: the order of the properties does not matter, the order of an array does.
    /// </summary>
    /// <returns>where the two differ, or <c>null</c> if they are the same.</returns>
    internal static string? Diff(JsonNode? expected, JsonNode? actual, string path = "$")
    {
        switch (expected)
        {
            case JsonObject expectedObject:
            {
                if (actual is not JsonObject actualObject) return $"{path}: expected an object, but it is {Show(actual)}";
                foreach (var (key, value) in expectedObject)
                {
                    if (!actualObject.ContainsKey(key)) return $"{path}.{key}: missing, expected {Show(value)}";
                    var difference = Diff(value, actualObject[key], $"{path}.{key}");
                    if (difference != null) return difference;
                }

                foreach (var (key, value) in actualObject)
                {
                    if (!expectedObject.ContainsKey(key)) return $"{path}.{key}: not expected, but it is {Show(value)}";
                }

                return null;
            }
            case JsonArray expectedArray:
            {
                if (actual is not JsonArray actualArray) return $"{path}: expected an array, but it is {Show(actual)}";
                if (expectedArray.Count != actualArray.Count) return $"{path}: expected {expectedArray.Count} items, but there are {actualArray.Count}";
                for (var i = 0; i < expectedArray.Count; i++)
                {
                    var difference = Diff(expectedArray[i], actualArray[i], $"{path}[{i}]");
                    if (difference != null) return difference;
                }

                return null;
            }
            default:
                return JsonNode.DeepEquals(expected, actual) ? null : $"{path}: expected {Show(expected)}, but it is {Show(actual)}";
        }
    }

    internal static string Show(JsonNode? node)
    {
        var text = node?.ToJsonString() ?? "null";
        return text.Length <= 80 ? text : text[..77] + "...";
    }

    internal static void AssertSameJson(string expected, string actual)
    {
        Assert.That(Diff(JsonNode.Parse(expected), JsonNode.Parse(actual)), Is.Null);
    }

    /// <summary>
    /// The properties of a model that are in its JSON.
    /// </summary>
    internal static IEnumerable<PropertyInfo> JsonProperties(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetCustomAttribute<JsonIgnoreAttribute>()?.Condition != JsonIgnoreCondition.Always);
    }

    /// <summary>
    /// Fills a model with a value that is different for every property, so a property that is copied or mapped from another one shows.
    /// </summary>
    internal sealed class Sampler
    {
        // the queries have queries of their own, of the same classes, so the depth of the models is bounded
        private const int MaxDepth = 6;

        private readonly Func<PropertyInfo, Type?>? _elementTypeOf;
        private int _n;
        private int _depth;

        /// <summary>
        /// Initialize a new sampler.
        /// </summary>
        /// <param name="elementTypeOf">gives the class of the elements of a list property that holds a base class, such as the inputs of the
        /// transaction of a wallet, which are wallet outputs. It gives <c>null</c> for the others.</param>
        public Sampler(Func<PropertyInfo, Type?>? elementTypeOf = null)
        {
            _elementTypeOf = elementTypeOf;
        }

        public T Populate<T>() where T : new()
        {
            return (T)Populate(new T());
        }

        public object Populate(object model)
        {
            _depth++;
            try
            {
                foreach (var property in JsonProperties(model.GetType()))
                {
                    var elementType = _elementTypeOf?.Invoke(property);
                    property.SetValue(model, elementType != null ? MakeList(property.PropertyType, elementType) : Make(property.PropertyType, property.Name));
                }
            }
            finally
            {
                _depth--;
            }

            return model;
        }

        private object MakeList(Type listType, Type elementType)
        {
            var list = CreateInstanceAs<IList>(listType);
            list.Add(Populate(Activator.CreateInstance(elementType)!));
            list.Add(Populate(Activator.CreateInstance(elementType)!));
            return list;
        }

        private object Make(Type type, string name)
        {
            var t = Nullable.GetUnderlyingType(type) ?? type;
            if (t == typeof(string)) return $"{name}-{++_n}";
            if (t == typeof(bool)) return (++_n & 1) == 0;
            if (t == typeof(byte)) return (byte)(255 - ++_n % 200);
            if (t == typeof(int)) return int.MaxValue - ++_n;
            if (t == typeof(uint)) return uint.MaxValue - (uint)++_n;
            if (t == typeof(ulong)) return ulong.MaxValue - (ulong)++_n;
            if (t.IsEnum)
            {
                var values = Enum.GetValues(t);
                return values.GetValue(++_n % values.Length)!;
            }

            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
            {
                var elementClass = t.GetGenericArguments()[0];
                if (elementClass.IsClass && elementClass != typeof(string) && _depth >= MaxDepth) return null!;
                var list = CreateInstanceAs<IList>(t);
                list.Add(Make(t.GetGenericArguments()[0], name + "Item"));
                list.Add(Make(t.GetGenericArguments()[0], name + "Item"));
                return list;
            }

            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var dictionary = CreateInstanceAs<IDictionary>(t);
                var arguments = t.GetGenericArguments();
                dictionary.Add(Make(arguments[0], name + "Key"), Make(arguments[1], name + "Value"));
                return dictionary;
            }

            if (t.IsClass) return _depth >= MaxDepth ? null! : Populate(Activator.CreateInstance(t)!);
            throw new NotSupportedException($"No sample for {type} of {name}");
        }

        internal static T CreateInstanceAs<T>(Type type) where T : class
        {
            return Activator.CreateInstance(type) as T ?? throw new InvalidOperationException($"{type} is not a {typeof(T).Name}");
        }
    }

    internal static string FixturePath(string folder, string name)
    {
        return Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", folder, name + ".json");
    }

    internal static string Fixture(string folder, string name)
    {
        return File.ReadAllText(FixturePath(folder, name));
    }

    internal static object ReadWith(Engine engine, string json, Type type, JsonSerializerContext context)
    {
        var model = engine == Engine.Reflection
            ? JsonSerializer.Deserialize(json, type, MoneroJson.Options)
            : JsonSerializer.Deserialize(json, type, context);
        Assert.That(model, Is.Not.Null, $"{type.Name} was read as null");
        return model!;
    }

    internal static string WriteWith(Engine engine, object model, Type type, JsonSerializerContext context)
    {
        return engine == Engine.Reflection
            ? JsonSerializer.Serialize(model, type, MoneroJson.Options)
            : JsonSerializer.Serialize(model, type, context);
    }
}
