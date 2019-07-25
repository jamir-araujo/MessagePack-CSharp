using MessagePack.Resolvers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Xunit;

namespace MessagePack.Tests
{
    public class TypelessContractlessStandardResolverAllowPrivateTests
    {
        public class Address
        {
            public string Street { get; set; }
        }

        public class Person
        {
            public string Name { get; set; }
            public object[] /*Address*/ Addresses { get; set; }
        }

        public class ForTypelessObj
        {
            public object Obj { get; set; }
        }

        [DataContract]
        public class Contact
        {
            public string Name { get; set; }
            public string Phone { get; set; }
        }

        [DataContract]
        public class TransferCostDto
        {
            public ulong UploadQty { get; private set; }
            public ulong DownloadQty { get; private set; }

            private NestedClass Nested { get; set; }

            private ulong _UploadQty;
            private ulong _DownloadQty;

            public ulong GetUploadQty()
            {
                return _UploadQty;
            }

            public ulong GetDownloadQty()
            {
                return _DownloadQty;
            }

            public virtual void AddUploadQty(ulong bytes)
            {
                _UploadQty += bytes;
                UploadQty += bytes;
            }

            public virtual void AddDownloadQty(ulong bytes)
            {
                _DownloadQty += bytes;
                DownloadQty += bytes;
            }

            public void CreateNested(int one, int two)
            {
                Nested = new NestedClass(one, two);
            }

            class NestedClass
            {
                private readonly int _one;
                private readonly int _two;

                public NestedClass()
                {
                }

                public NestedClass(int one, int two)
                {
                    _one = one;
                    _two = two;
                }
            }
        }

        [Fact]
        public void Should_Serialize_PrivateProperties()
        {
            var value = new TransferCostDto();
            value.AddDownloadQty(3);
            value.AddUploadQty(10);
            value.CreateNested(1, 2);

            var bytes = MessagePackSerializer.Serialize(value, TypelessContractlessStandardResolverAllowPrivate.Instance);
            var result = MessagePackSerializer.Deserialize<TransferCostDto>(bytes, TypelessContractlessStandardResolverAllowPrivate.Instance);

            value.IsStructuralEqual(result);

            MessagePackSerializer.ToJson(bytes).Is(@"{""UploadQty"":10,""DownloadQty"":3,""Nested"":{""_one"":1,""_two"":2},""_UploadQty"":10,""_DownloadQty"":3}");
        }

        [Fact]
        public void Should_Serialize_DataContractWithoutDataMembers()
        {
            var c = new Contact
            {
                Name = "baka",
                Phone = "55555555555"
            };

            var result = MessagePackSerializer.Serialize(c, TypelessContractlessStandardResolverAllowPrivate.Instance);
            var p2 = MessagePackSerializer.Deserialize<Contact>(result, TypelessContractlessStandardResolverAllowPrivate.Instance);

            c.IsStructuralEqual(p2);

            MessagePackSerializer.ToJson(result).Is(@"{""Name"":""baka"",""Phone"":""55555555555""}");
        }

        [Fact]
        public void AnonymousTypeTest()
        {
            var p = new Person
            {
                Name = "John",
                Addresses = new[]
                {
                        new { Street = "St." },
                        new { Street = "Ave." }
                    }
            };

            var result = MessagePackSerializer.Serialize(p, TypelessContractlessStandardResolverAllowPrivate.Instance);

            MessagePackSerializer.ToJson(result).Is(@"{""Name"":""John"",""Addresses"":[{""Street"":""St.""},{""Street"":""Ave.""}]}");

            var p2 = MessagePackSerializer.Deserialize<Person>(result, TypelessContractlessStandardResolverAllowPrivate.Instance);
            p2.Name.Is("John");
            var addresses = p2.Addresses as IList;
            var d1 = addresses[0] as IDictionary;
            var d2 = addresses[1] as IDictionary;
            (d1["Street"] as string).Is("St.");
            (d2["Street"] as string).Is("Ave.");
        }

        [Fact]
        public void StrongTypeTest()
        {
            var p = new Person
            {
                Name = "John",
                Addresses = new object[]
                {
                    new Address { Street = "St." },
                    new Address { Street = "Ave." }
                }
            };

            var result = MessagePackSerializer.Serialize(p, TypelessContractlessStandardResolverAllowPrivate.Instance);

            var p2 = MessagePackSerializer.Deserialize<Person>(result, TypelessContractlessStandardResolverAllowPrivate.Instance);
            p.IsStructuralEqual(p2);

            MessagePackSerializer.ToJson(result).Is(@"{""Name"":""John"",""Addresses"":[{""$type"":""MessagePack.Tests.TypelessContractlessStandardResolverAllowPrivateTests+Address, MessagePack.Tests"",""Street"":""St.""},{""$type"":""MessagePack.Tests.TypelessContractlessStandardResolverAllowPrivateTests+Address, MessagePack.Tests"",""Street"":""Ave.""}]}");
        }

        [Fact]
        public void ObjectRuntimeTypeTest()
        {
            var p = new Person
            {
                Name = "John",
                Addresses = new object[]
                {
                    new object(),
                    new Address { Street = "Ave." }
                }
            };

            var result = MessagePackSerializer.Serialize(p, TypelessContractlessStandardResolverAllowPrivate.Instance);

            var p2 = MessagePackSerializer.Deserialize<Person>(result, TypelessContractlessStandardResolverAllowPrivate.Instance);
            p.IsStructuralEqual(p2);

            MessagePackSerializer.ToJson(result).Is(@"{""Name"":""John"",""Addresses"":[{""$type"":""""System.Object, mscorlib""},{""$type"":""MessagePack.Tests.TypelessContractlessStandardResolverAllowPrivateTests+Address, MessagePack.Tests"",""Street"":""Ave.""}]}");
        }

        public class A { public int Id; }
        public class B { public A Nested; }

        [Fact]
        public void TypelessContractlessTest()
        {
            object obj = new B() { Nested = new A() { Id = 1 } };
            var result = MessagePackSerializer.Serialize(obj, TypelessContractlessStandardResolverAllowPrivate.Instance);
            MessagePackSerializer.ToJson(result).Is(@"{""$type"":""MessagePack.Tests.TypelessContractlessStandardResolverAllowPrivateTests+B, MessagePack.Tests"",""Nested"":{""Id"":1}}");
        }

        [MessagePackObject]
        public class AC {[Key(0)] public int Id; }
        [MessagePackObject]
        public class BC {[Key(0)] public AC Nested;[Key(1)] public string Name; }

        [Fact]
        public void TypelessAttributedTest()
        {
            object obj = new BC() { Nested = new AC() { Id = 1 }, Name = "Zed" };
            var result = MessagePackSerializer.Serialize(obj, TypelessContractlessStandardResolverAllowPrivate.Instance);
            MessagePackSerializer.ToJson(result).Is(@"[""MessagePack.Tests.TypelessContractlessStandardResolverAllowPrivateTests+BC, MessagePack.Tests"",[1],""Zed""]");
        }

        [Fact]
        public void PreservingTimezoneInTypelessCollectionsTest()
        {
            var arr = new Dictionary<object, object>()
            {
                { (byte)1, "a"},
                { (byte)2, new object[] { "level2", new object[] { "level3", new Person() { Name = "Peter", Addresses = new object[] { new Address() { Street = "St." }, new DateTime(2017,6,26,14,58,0) } } } } }
            };
            var result = MessagePackSerializer.Serialize(arr, TypelessContractlessStandardResolverAllowPrivate.Instance);

            var deser = MessagePackSerializer.Deserialize<Dictionary<object, object>>(result, TypelessContractlessStandardResolverAllowPrivate.Instance);
            deser.IsStructuralEqual(arr);

            MessagePackSerializer.ToJson(result).Is(@"{""1"":""a"",""2"":[""System.Object[], mscorlib"",""level2"",[""System.Object[], mscorlib"",""level3"",{""$type"":""MessagePack.Tests.TypelessContractlessStandardResolverAllowPrivateTests+Person, MessagePack.Tests"",""Name"":""Peter"",""Addresses"":[{""$type"":""MessagePack.Tests.TypelessContractlessStandardResolverAllowPrivateTests+Address, MessagePack.Tests"",""Street"":""St.""},{""$type"":""System.DateTime, mscorlib"",636340858800000000}]}]]}");
        }

        [Fact]
        public void PreservingCollectionTypeTest()
        {
            var arr = new object[] { (byte)1, new object[] { (byte)2, new LinkedList<object>(new object[] { "a", (byte)42 }) } };
            var result = MessagePackSerializer.Serialize(arr, TypelessContractlessStandardResolverAllowPrivate.Instance);
            var deser = MessagePackSerializer.Deserialize<object[]>(result, TypelessContractlessStandardResolverAllowPrivate.Instance);
            deser.IsStructuralEqual(arr);

            MessagePackSerializer.ToJson(result).Is(@"[1,[""System.Object[], mscorlib"",2,[""System.Collections.Generic.LinkedList`1[[System.Object, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"",""a"",42]]]");
        }

        [Theory]
        [InlineData((sbyte)0)]
        [InlineData((short)0)]
        [InlineData((int)0)]
        [InlineData((long)0)]
        [InlineData((byte)0)]
        [InlineData((ushort)0)]
        [InlineData((uint)0)]
        [InlineData((ulong)0)]
        [InlineData((char)'a')]
        public void TypelessPrimitive<T>(T p)
        {
            var v = new ForTypelessObj() { Obj = p };

            var bin = MessagePackSerializer.Typeless.Serialize(v);
            var o = (ForTypelessObj)MessagePackSerializer.Typeless.Deserialize(bin);

            o.Obj.GetType().Is(typeof(T));
        }

        [Fact]
        public void TypelessPrimitive2()
        {
            {
                var now = DateTime.Now;
                var v = new ForTypelessObj() { Obj = now };

                var bin = MessagePackSerializer.Typeless.Serialize(v);
                var o = (ForTypelessObj)MessagePackSerializer.Typeless.Deserialize(bin);

                o.Obj.GetType().Is(typeof(DateTime));
                ((DateTime)o.Obj).Is(now);
            }
            {
                var now = DateTimeOffset.Now;
                var v = new ForTypelessObj() { Obj = now };

                var bin = MessagePackSerializer.Typeless.Serialize(v);
                var o = (ForTypelessObj)MessagePackSerializer.Typeless.Deserialize(bin);

                o.Obj.GetType().Is(typeof(DateTimeOffset));
                ((DateTimeOffset)o.Obj).Is(now);
            }
        }

        [Fact]
        public void TypelessEnum()
        {
            var e = MessagePackSerializer.Typeless.Serialize(GlobalMyEnum.Apple);
            var b = MessagePackSerializer.Typeless.Deserialize(e);
            b.GetType().Is(typeof(GlobalMyEnum));
        }

        [Fact]
        public void MyTestMethod()
        {
            var sampleMessage = new InternalSampleMessageType
            {
                DateProp = new DateTime(2016, 10, 8, 1, 2, 3, DateTimeKind.Utc),
                GuidProp = Guid.NewGuid(),
                IntProp = 123,
                StringProp = "Hello World"
            };

            {
                var serializedMessage = MessagePackSerializer.Typeless.Serialize(sampleMessage);
                var r2 = (InternalSampleMessageType)MessagePackSerializer.Typeless.Deserialize(serializedMessage);
                r2.DateProp.Is(sampleMessage.DateProp);
                r2.GuidProp.Is(sampleMessage.GuidProp);
                r2.IntProp.Is(sampleMessage.IntProp);
                r2.StringProp.Is(sampleMessage.StringProp);
            }

            {
                var serializedMessage = LZ4MessagePackSerializer.Typeless.Serialize(sampleMessage);
                var r2 = (InternalSampleMessageType)LZ4MessagePackSerializer.Typeless.Deserialize(serializedMessage);
                r2.DateProp.Is(sampleMessage.DateProp);
                r2.GuidProp.Is(sampleMessage.GuidProp);
                r2.IntProp.Is(sampleMessage.IntProp);
                r2.StringProp.Is(sampleMessage.StringProp);
            }
        }

        [Fact]
        public void SaveArrayType()
        {
            {
                string[] array = new[] { "test1", "test2" };
                byte[] bytes = MessagePackSerializer.Typeless.Serialize(array);
                object obj = MessagePackSerializer.Typeless.Deserialize(bytes);

                var obj2 = obj as string[];
                obj2.Is("test1", "test2");
            }
            {
                var objRaw = new SomeClass
                {
                    Obj = new string[] { "asd", "asd" }
                };

                var objSer = MessagePackSerializer.Serialize(objRaw, TypelessContractlessStandardResolverAllowPrivate.Instance);

                var objDes = MessagePackSerializer.Deserialize<SomeClass>(objSer, TypelessContractlessStandardResolverAllowPrivate.Instance);

                var expectedTrue = objDes.Obj is string[];
                expectedTrue.IsTrue();
            }
        }
    }
}
