﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;

#if !UNITY_2018_3_OR_NEWER
using System.Collections.Concurrent;
#endif

#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters
{
#if !UNITY_2018_3_OR_NEWER

    // unfortunately, can't use IDictionary<KVP> because supports IReadOnlyDictionary.
    public abstract class DictionaryFormatterBase<TKey, TValue, TIntermediate, TEnumerator, TDictionary> : IMessagePackFormatter<TDictionary>
        where TDictionary : IEnumerable<KeyValuePair<TKey, TValue>>
        where TEnumerator : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        public void Serialize(ref MessagePackWriter writer, TDictionary value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                IFormatterResolver resolver = options.Resolver;
                IMessagePackFormatter<TKey> keyFormatter = resolver.GetFormatterWithVerify<TKey>();
                IMessagePackFormatter<TValue> valueFormatter = resolver.GetFormatterWithVerify<TValue>();

                int count;
                {
                    var col = value as ICollection<KeyValuePair<TKey, TValue>>;
                    if (col != null)
                    {
                        count = col.Count;
                    }
                    else
                    {
                        var col2 = value as IReadOnlyCollection<KeyValuePair<TKey, TValue>>;
                        if (col2 != null)
                        {
                            count = col2.Count;
                        }
                        else
                        {
                            throw new InvalidOperationException("DictionaryFormatterBase's TDictionary supports only ICollection<KVP> or IReadOnlyCollection<KVP>");
                        }
                    }
                }

                writer.WriteMapHeader(count);

                TEnumerator e = this.GetSourceEnumerator(value);
                try
                {
                    while (e.MoveNext())
                    {
                        KeyValuePair<TKey, TValue> item = e.Current;
                        keyFormatter.Serialize(ref writer, item.Key, options);
                        valueFormatter.Serialize(ref writer, item.Value, options);
                    }
                }
                finally
                {
                    e.Dispose();
                }
            }
        }

        public TDictionary Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default(TDictionary);
            }
            else
            {
                IFormatterResolver resolver = options.Resolver;
                IMessagePackFormatter<TKey> keyFormatter = resolver.GetFormatterWithVerify<TKey>();
                IMessagePackFormatter<TValue> valueFormatter = resolver.GetFormatterWithVerify<TValue>();

                var len = reader.ReadMapHeader();

                TIntermediate dict = this.Create(len);
                for (int i = 0; i < len; i++)
                {
                    TKey key = keyFormatter.Deserialize(ref reader, options);

                    TValue value = valueFormatter.Deserialize(ref reader, options);

                    this.Add(dict, i, key, value);
                }

                return this.Complete(dict);
            }
        }

        // abstraction for serialize

        // Some collections can use struct iterator, this is optimization path
        protected abstract TEnumerator GetSourceEnumerator(TDictionary source);

        // abstraction for deserialize
        protected abstract TIntermediate Create(int count);

        protected abstract void Add(TIntermediate collection, int index, TKey key, TValue value);

        protected abstract TDictionary Complete(TIntermediate intermediateCollection);
    }

    public abstract class DictionaryFormatterBase<TKey, TValue, TIntermediate, TDictionary> : DictionaryFormatterBase<TKey, TValue, TIntermediate, IEnumerator<KeyValuePair<TKey, TValue>>, TDictionary>
        where TDictionary : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        protected override IEnumerator<KeyValuePair<TKey, TValue>> GetSourceEnumerator(TDictionary source)
        {
            return source.GetEnumerator();
        }
    }

    public abstract class DictionaryFormatterBase<TKey, TValue, TDictionary> : DictionaryFormatterBase<TKey, TValue, TDictionary, TDictionary>
        where TDictionary : IDictionary<TKey, TValue>
    {
        protected override TDictionary Complete(TDictionary intermediateCollection)
        {
            return intermediateCollection;
        }
    }

    public sealed class DictionaryFormatter<TKey, TValue> : DictionaryFormatterBase<TKey, TValue, Dictionary<TKey, TValue>, Dictionary<TKey, TValue>.Enumerator, Dictionary<TKey, TValue>>
    {
        protected override void Add(Dictionary<TKey, TValue> collection, int index, TKey key, TValue value)
        {
            collection.Add(key, value);
        }

        protected override Dictionary<TKey, TValue> Complete(Dictionary<TKey, TValue> intermediateCollection)
        {
            return intermediateCollection;
        }

        protected override Dictionary<TKey, TValue> Create(int count)
        {
            return new Dictionary<TKey, TValue>(count);
        }

        protected override Dictionary<TKey, TValue>.Enumerator GetSourceEnumerator(Dictionary<TKey, TValue> source)
        {
            return source.GetEnumerator();
        }
    }

    public sealed class GenericDictionaryFormatter<TKey, TValue, TDictionary> : DictionaryFormatterBase<TKey, TValue, TDictionary>
        where TDictionary : IDictionary<TKey, TValue>, new()
    {
        protected override void Add(TDictionary collection, int index, TKey key, TValue value)
        {
            collection.Add(key, value);
        }

        protected override TDictionary Create(int count)
        {
            return new TDictionary();
        }
    }

    public sealed class InterfaceDictionaryFormatter<TKey, TValue> : DictionaryFormatterBase<TKey, TValue, Dictionary<TKey, TValue>, IDictionary<TKey, TValue>>
    {
        protected override void Add(Dictionary<TKey, TValue> collection, int index, TKey key, TValue value)
        {
            collection.Add(key, value);
        }

        protected override Dictionary<TKey, TValue> Create(int count)
        {
            return new Dictionary<TKey, TValue>(count);
        }

        protected override IDictionary<TKey, TValue> Complete(Dictionary<TKey, TValue> intermediateCollection)
        {
            return intermediateCollection;
        }
    }

    public sealed class SortedListFormatter<TKey, TValue> : DictionaryFormatterBase<TKey, TValue, SortedList<TKey, TValue>>
    {
        protected override void Add(SortedList<TKey, TValue> collection, int index, TKey key, TValue value)
        {
            collection.Add(key, value);
        }

        protected override SortedList<TKey, TValue> Create(int count)
        {
            return new SortedList<TKey, TValue>(count);
        }
    }

    public sealed class SortedDictionaryFormatter<TKey, TValue> : DictionaryFormatterBase<TKey, TValue, SortedDictionary<TKey, TValue>, SortedDictionary<TKey, TValue>.Enumerator, SortedDictionary<TKey, TValue>>
    {
        protected override void Add(SortedDictionary<TKey, TValue> collection, int index, TKey key, TValue value)
        {
            collection.Add(key, value);
        }

        protected override SortedDictionary<TKey, TValue> Complete(SortedDictionary<TKey, TValue> intermediateCollection)
        {
            return intermediateCollection;
        }

        protected override SortedDictionary<TKey, TValue> Create(int count)
        {
            return new SortedDictionary<TKey, TValue>();
        }

        protected override SortedDictionary<TKey, TValue>.Enumerator GetSourceEnumerator(SortedDictionary<TKey, TValue> source)
        {
            return source.GetEnumerator();
        }
    }

    public sealed class ReadOnlyDictionaryFormatter<TKey, TValue> : DictionaryFormatterBase<TKey, TValue, Dictionary<TKey, TValue>, ReadOnlyDictionary<TKey, TValue>>
    {
        protected override void Add(Dictionary<TKey, TValue> collection, int index, TKey key, TValue value)
        {
            collection.Add(key, value);
        }

        protected override ReadOnlyDictionary<TKey, TValue> Complete(Dictionary<TKey, TValue> intermediateCollection)
        {
            return new ReadOnlyDictionary<TKey, TValue>(intermediateCollection);
        }

        protected override Dictionary<TKey, TValue> Create(int count)
        {
            return new Dictionary<TKey, TValue>(count);
        }
    }

    public sealed class InterfaceReadOnlyDictionaryFormatter<TKey, TValue> : DictionaryFormatterBase<TKey, TValue, Dictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>>
    {
        protected override void Add(Dictionary<TKey, TValue> collection, int index, TKey key, TValue value)
        {
            collection.Add(key, value);
        }

        protected override IReadOnlyDictionary<TKey, TValue> Complete(Dictionary<TKey, TValue> intermediateCollection)
        {
            return intermediateCollection;
        }

        protected override Dictionary<TKey, TValue> Create(int count)
        {
            return new Dictionary<TKey, TValue>(count);
        }
    }

    public sealed class ConcurrentDictionaryFormatter<TKey, TValue> : DictionaryFormatterBase<TKey, TValue, System.Collections.Concurrent.ConcurrentDictionary<TKey, TValue>>
    {
        protected override void Add(ConcurrentDictionary<TKey, TValue> collection, int index, TKey key, TValue value)
        {
            collection.TryAdd(key, value);
        }

        protected override ConcurrentDictionary<TKey, TValue> Create(int count)
        {
            // concurrent dictionary can't access defaultConcurrecyLevel so does not use count overload.
            return new ConcurrentDictionary<TKey, TValue>();
        }
    }

#else

    public abstract class DictionaryFormatterBase<TKey, TValue, TIntermediate, TDictionary> : IMessagePackFormatter<TDictionary>
        where TDictionary : IDictionary<TKey, TValue>
    {
        public void Serialize(ref MessagePackWriter writer, TDictionary value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                var keyFormatter = options.Resolver.GetFormatterWithVerify<TKey>();
                var valueFormatter = options.Resolver.GetFormatterWithVerify<TValue>();

                var count = value.Count;

                writer.WriteMapHeader(count);

                var e = value.GetEnumerator();
                try
                {
                    while (e.MoveNext())
                    {
                        var item = e.Current;
                        keyFormatter.Serialize(ref writer, item.Key, options);
                        valueFormatter.Serialize(ref writer, item.Value, options);
                    }
                }
                finally
                {
                    e.Dispose();
                }
            }
        }

        public TDictionary Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default(TDictionary);
            }
            else
            {
                var keyFormatter = options.Resolver.GetFormatterWithVerify<TKey>();
                var valueFormatter = options.Resolver.GetFormatterWithVerify<TValue>();

                var len = reader.ReadMapHeader();

                var dict = Create(len);
                for (int i = 0; i < len; i++)
                {
                    var key = keyFormatter.Deserialize(ref reader, options);
                    var value = valueFormatter.Deserialize(ref reader, options);
                    Add(dict, i, key, value);
                }

                return Complete(dict);
            }
        }

        // abstraction for deserialize
        protected abstract TIntermediate Create(int count);
        protected abstract void Add(TIntermediate collection, int index, TKey key, TValue value);
        protected abstract TDictionary Complete(TIntermediate intermediateCollection);
    }

    public sealed class DictionaryFormatter<TKey, TValue> : DictionaryFormatterBase<TKey, TValue, Dictionary<TKey, TValue>, Dictionary<TKey, TValue>>
    {
        protected override void Add(Dictionary<TKey, TValue> collection, int index, TKey key, TValue value)
        {
            collection.Add(key, value);
        }

        protected override Dictionary<TKey, TValue> Complete(Dictionary<TKey, TValue> intermediateCollection)
        {
            return intermediateCollection;
        }

        protected override Dictionary<TKey, TValue> Create(int count)
        {
            return new Dictionary<TKey, TValue>(count);
        }
    }

    public sealed class GenericDictionaryFormatter<TKey, TValue, TDictionary> : DictionaryFormatterBase<TKey, TValue, TDictionary, TDictionary>
        where TDictionary : IDictionary<TKey, TValue>, new()
    {
        protected override void Add(TDictionary collection, int index, TKey key, TValue value)
        {
            collection.Add(key, value);
        }

        protected override TDictionary Complete(TDictionary intermediateCollection)
        {
            return intermediateCollection;
        }

        protected override TDictionary Create(int count)
        {
            return new TDictionary();
        }
    }

    public sealed class InterfaceDictionaryFormatter<TKey, TValue> : DictionaryFormatterBase<TKey, TValue, Dictionary<TKey, TValue>, IDictionary<TKey, TValue>>
    {
        protected override void Add(Dictionary<TKey, TValue> collection, int index, TKey key, TValue value)
        {
            collection.Add(key, value);
        }

        protected override Dictionary<TKey, TValue> Create(int count)
        {
            return new Dictionary<TKey, TValue>(count);
        }

        protected override IDictionary<TKey, TValue> Complete(Dictionary<TKey, TValue> intermediateCollection)
        {
            return intermediateCollection;
        }
    }

    public sealed class SortedListFormatter<TKey, TValue> : DictionaryFormatterBase<TKey, TValue, SortedList<TKey, TValue>,  SortedList<TKey, TValue>>
    {
        protected override void Add(SortedList<TKey, TValue> collection, int index, TKey key, TValue value)
        {
            collection.Add(key, value);
        }

        protected override SortedList<TKey, TValue> Complete(SortedList<TKey, TValue> intermediateCollection)
        {
            return intermediateCollection;
        }

        protected override SortedList<TKey, TValue> Create(int count)
        {
            return new SortedList<TKey, TValue>(count);
        }
    }

    public sealed class SortedDictionaryFormatter<TKey, TValue> : DictionaryFormatterBase<TKey, TValue, SortedDictionary<TKey, TValue>, SortedDictionary<TKey, TValue>>
    {
        protected override void Add(SortedDictionary<TKey, TValue> collection, int index, TKey key, TValue value)
        {
            collection.Add(key, value);
        }

        protected override SortedDictionary<TKey, TValue> Complete(SortedDictionary<TKey, TValue> intermediateCollection)
        {
            return intermediateCollection;
        }

        protected override SortedDictionary<TKey, TValue> Create(int count)
        {
            return new SortedDictionary<TKey, TValue>();
        }
    }

#endif

}
