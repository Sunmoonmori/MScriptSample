// this is not a good implementation
// this is only to show that a custom edit-time JSON serializer and runtime deserializer is needed

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MScript;
using MScriptWrapperRegistry;
using SampleScene.SampleCode.UI;
using UnityEngine;

namespace SampleScene.SampleCode.Script
{
    public static class MScriptImageSerialization
    {
        public static object Deserialize(Dictionary<string, object> obj)
        {
            return new MScriptImage(((List<object>)obj["IndexedNodeNames"]).Select(v => (string)v).ToArray(),
                NodeImageRegistry.GetNodeImage,
                RuntimeSerializer.ConvertValue<Connection[]>(obj["ControlFlowConnections"]),
                RuntimeSerializer.ConvertValue<Connection[]>(obj["AsyncControlFlowConnections"]),
                RuntimeSerializer.ConvertValue<Connection[]>(obj["DataFlowConnections"]));
        }
    }

    public class RuntimeSerializer : INodeValueDeserializer
    {
        private static readonly Dictionary<Type, Func<object, object>> CustomDeserializers = new();
        
        static RuntimeSerializer()
        {
            CustomDeserializers.Add(typeof(MScriptImage),
                obj => MScriptImageSerialization.Deserialize((Dictionary<string, object>)obj));
        }
        
        private readonly Action<Exception> _logException;

        public RuntimeSerializer(Action<Exception> logException)
        {
            _logException = logException;
        }
        
        public static T ConvertValue<T>(object obj)
        {
            return (T)ConvertValue(obj, typeof(T));
        }
        
        private static object ConvertValue(object obj, Type type)
        {
            if (CustomDeserializers.TryGetValue(type, out var deserializer))
            {
                return deserializer(obj);
            }
            
            if (type == typeof(string) || type == typeof(bool))
            {
                return obj;
            }

            if (type == typeof(char))
            {
                return Convert.ChangeType(((string)obj)[0], type);
            }

            if (type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(sbyte) ||
                type == typeof(byte) || type == typeof(short) || type == typeof(ushort) || type == typeof(ulong))
            {
                return Convert.ChangeType(obj, type);
            }

            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            {
                return Convert.ChangeType(obj, type);
            }

            if (typeof(IList).IsAssignableFrom(type) ||
                type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IList<>))
            {
                var elementType = EditorSerializer.GetListElementType(type);
                if (elementType == null)
                {
                    return default;
                }

                var list = (List<object>)obj;
                if (type.IsArray)
                {
                    var result = Array.CreateInstance(elementType, list.Count);
                    for (int i = 0, e = list.Count; i < e; i++)
                    {
                        result.SetValue(ConvertValue(list[i], elementType), i);
                    }

                    return result;
                }
                else
                {
                    var result =
                        (IList)Activator.CreateInstance(type.IsInterface
                            ? typeof(List<>).MakeGenericType(elementType)
                            : type);
                    for (int i = 0, e = list.Count; i < e; i++)
                    {
                        result.Add(ConvertValue(list[i], elementType));
                    }

                    return result;
                }
            }

            if (typeof(IDictionary).IsAssignableFrom(type) ||
                type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            {
                var (keyType, valueType) = EditorSerializer.GetDictionaryTypes(type);
                if (keyType == null || valueType == null)
                {
                    return null;
                }

                var dict = (Dictionary<string, object>)obj;
                var result = (IDictionary)Activator.CreateInstance(type.IsInterface ? typeof(Dictionary<,>).MakeGenericType(keyType, valueType) : type);
                foreach (var (k, v) in dict)
                {
                    result[Convert.ChangeType(k, keyType)] = ConvertValue(v, valueType);
                }
                return result;
            }

            {
                var dict = (Dictionary<string, object>)obj;
                var result = Activator.CreateInstance(type);
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (dict.TryGetValue(field.Name, out var value))
                    {
                        field.SetValue(result, ConvertValue(value, field.FieldType));
                    }
                }

                foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (dict.TryGetValue(property.Name, out var value))
                    {
                        property.SetValue(result, ConvertValue(value, property.PropertyType));
                    }
                }

                return result;
            }
        }

        public bool Deserialize<T>(string value, out T result)
        {
            try
            {
                var obj = Json.Deserialize(value);
                result = ConvertValue<T>(obj);
                return true;
            }
            catch (Exception e)
            {
                _logException?.Invoke(e);
                result = default;
                return false;
            }
        }
    }

    public class EditorSerializer : IEditorNodeValueJsonSerializer
    {
        private static readonly Dictionary<Type, Func<EditorJsonSerializationInfo>> CustomGetTypeSerializationInfo = new();

        static EditorSerializer()
        {
            CustomGetTypeSerializationInfo.Add(typeof(MonoSampleUI), () => new EditorJsonSerializationInfo());
        }
        
        private readonly Dictionary<string, Type> _serializedTypeCache = new();

        public string Serialize<T>(T value) where T : struct
        {
            return Json.Serialize(value);
        }

        public string Serialize(object value)
        {
            return value is null ? string.Empty : Json.Serialize(value);
        }

        public SerializedNodeValues ConvertSerializedNodeValues(EditorJsonSerializedNodeValues debugSerializedNodeValues,
            Func<int, string, Type> nodeValueInPortTypeQuerier)
        {
            return new SerializedNodeValues
            {
                SerializedValues = debugSerializedNodeValues.SerializedValues,
            };
        }

        public EditorJsonSerializedNodeValues ConvertSerializedNodeValues(SerializedNodeValues serializedNodeValues,
            Func<int, string, Type> nodeValueInPortTypeQuerier)
        {
            return new EditorJsonSerializedNodeValues
            {
                SerializedValues = serializedNodeValues.SerializedValues,
            };
        }

        public static Type GetListElementType(Type type)
        {
            if (type.IsArray)
                return type.GetElementType()!;

            var iListType = type.GetInterfaces()
                .FirstOrDefault(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IList<>));

            if (iListType != null)
            {
                return iListType.GetGenericArguments()[0];
            }
            
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IList<>))
            {
                return type.GetGenericArguments()[0];
            }

            return null;
        }

        public static (Type KeyType, Type ValueType) GetDictionaryTypes(Type type)
        {
            var dictInterface = type.GetInterfaces()
                .FirstOrDefault(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IDictionary<,>));

            if (dictInterface != null)
            {
                var args = dictInterface.GetGenericArguments();
                return (args[0], args[1]);
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            {
                var args = type.GetGenericArguments();
                return (args[0], args[1]);
            }

            return (null, null);
        }

        public EditorJsonSerializationInfo GetTypeSerializationInfo(Type type)
        {
            try
            {

                if (EditorJsonSerializationInfo.IsPrimitive(type))
                {
                    return new EditorJsonSerializationInfo
                    {
                        SerializeType = EditorJsonSerializationInfo.SerializationType.Primitive,
                        PrimitiveType = type.Name
                    };
                }

                if (CustomGetTypeSerializationInfo.TryGetValue(type, out var getTypeSerializationInfo))
                {
                    return getTypeSerializationInfo();
                }

                if (typeof(IList).IsAssignableFrom(type) ||
                    type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IList<>))
                {
                    var elementType = GetListElementType(type);
                    if (elementType != null)
                    {
                        _serializedTypeCache[elementType.FullName!] = elementType;
                        return new EditorJsonSerializationInfo
                        {
                            SerializeType = EditorJsonSerializationInfo.SerializationType.List,
                            ListElementType = elementType.FullName
                        };
                    }

                    return new EditorJsonSerializationInfo();
                }

                if (typeof(IDictionary).IsAssignableFrom(type) ||
                    type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                {
                    var (keyType, valueType) = GetDictionaryTypes(type);
                    if (keyType != null && valueType != null)
                    {
                        _serializedTypeCache[keyType.FullName!] = keyType;
                        _serializedTypeCache[valueType.FullName!] = valueType;
                        return new EditorJsonSerializationInfo
                        {
                            SerializeType = EditorJsonSerializationInfo.SerializationType.Dictionary,
                            DictionaryKeyType = keyType.FullName,
                            DictionaryValueType = valueType.FullName
                        };
                    }

                    return new EditorJsonSerializationInfo();
                }

                var map = new Dictionary<string, string>();
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
                {
                    _serializedTypeCache[field.FieldType.FullName!] = field.FieldType;
                    map.Add(field.Name, field.FieldType.FullName);
                }

                foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    _serializedTypeCache[property.PropertyType.FullName!] = property.PropertyType;
                    map.Add(property.Name, property.PropertyType.FullName);
                }

                if (map.Count == 0)
                {
                    return new EditorJsonSerializationInfo();
                }

                return new EditorJsonSerializationInfo
                {
                    SerializeType = EditorJsonSerializationInfo.SerializationType.Custom,
                    SerializableFieldType = map
                };
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
            }
        }

        public Type GetSerializedType(string typeName)
        {
            return _serializedTypeCache[typeName];
        }
    }
}