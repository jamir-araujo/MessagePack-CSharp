using System;
using System.Collections.Generic;
using System.Text;
using MessagePack.Formatters;

namespace MessagePack.Resolvers
{
    public class TypelessContractlessStandardResolverAllowPrivate : IFormatterResolver
    {
        public static readonly IFormatterResolver Instance = new TypelessContractlessStandardResolverAllowPrivate();

        static readonly IFormatterResolver[] resolvers = new[]
        {
            NativeDateTimeResolver.Instance, // Native c# DateTime format, preserving timezone
            BuiltinResolver.Instance, // Try Builtin
            AttributeFormatterResolver.Instance, // Try use [MessagePackFormatter]
#if !ENABLE_IL2CPP
            DynamicEnumResolver.Instance, // Try Enum
            DynamicGenericResolver.Instance, // Try Array, Tuple, Collection
            DynamicUnionResolver.Instance, // Try Union(Interface)
#endif
            DynamicKeylessContractlessObjectResolverAllowPrivate.Instance, // Serializes keys as strings
            TypelessObjectResolver.Instance
        };

        TypelessContractlessStandardResolverAllowPrivate()
        {
        }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.formatter;
        }

        static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> formatter;

            static FormatterCache()
            {
                foreach (var item in resolvers)
                {
                    var f = item.GetFormatter<T>();
                    if (f != null)
                    {
                        formatter = f;
                        return;
                    }
                }
            }
        }
    }
}
