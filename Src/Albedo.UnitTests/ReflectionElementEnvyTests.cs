﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Moq;
using Xunit;

namespace Albedo.UnitTests
{
    public class ReflectionElementEnvyTests
    {
        [Fact]
        public void GetProperMethodsWithBindingFlagsThrowsOnNullType()
        {
            var e = Assert.Throws<ArgumentNullException>(() =>
                ReflectionElementEnvy.GetProperMethods(null, BindingFlags.Default));

            Assert.Equal("type", e.ParamName);
        }

        [Fact]
        public void GetProperMethodsThrowsOnNullType()
        {
            var e = Assert.Throws<ArgumentNullException>(() =>
                ReflectionElementEnvy.GetProperMethods(null));

            Assert.Equal("type", e.ParamName);
        }

        [Fact]
        public void GetProperMethodsReturnsPublicStaticAndPublicInstanceProperMethods()
        {
            // Fixture setup
            Func<MethodInfoElement, string> orderBy = mie => mie.MethodInfo.ToString();

            var type = typeof(TypeWithStaticAndInstanceMembers<int>);
            var methods = new Methods<TypeWithStaticAndInstanceMembers<int>>();
            var expected = new IReflectionElement[]
            {
                new MethodInfoElement(methods.Select(t => t.PublicMethod())),
                new MethodInfoElement(type.GetMethod("PublicStaticVoidMethod")),
                new MethodInfoElement(type.GetMethod("ToString")),
                new MethodInfoElement(type.GetMethod("Equals")),
                new MethodInfoElement(type.GetMethod("GetHashCode")),
                new MethodInfoElement(type.GetMethod("GetType")),
            };

            // Exercise system
            var actual = type.GetProperMethods();

            // Verify outcome
            AssertUnorderedElementsEqual(
                expected,
                actual,
                orderBy);

            // Fixture teardown
        }

        [Theory, ClassData(typeof(GetProperMethodsTestCases))]
        public void GetProperMethodsWithBindingFlagsReturnsMethodsExcludingPropertyAccessors(
            Type type, BindingFlags bindingAttr, IEnumerable<IReflectionElement> expectedElements)
        {
            // Fixture setup
            Func<MethodInfoElement, string> orderBy = mie => mie.MethodInfo.ToString();

            // Exercise system
            var actual = type.GetProperMethods(bindingAttr);

            // Verify outcome
            AssertUnorderedElementsEqual(
                expectedElements,
                actual,
                orderBy);

            // Fixture teardown
        }

        [Fact]
        public void AcceptThrowsOnNullElements()
        {
            var e = Assert.Throws<ArgumentNullException>(() =>
                ReflectionElementEnvy.Accept(null, new DelegatingReflectionVisitor<object>()));

            Assert.Equal("elements", e.ParamName);
        }

        [Fact]
        public void AcceptAcceptsVisitorForAllElements()
        {
            // Fixture setup
            var v1 = new Mock<IReflectionVisitor<int>>().Object;
            var v2 = new Mock<IReflectionVisitor<int>>().Object;
            var v3 = new Mock<IReflectionVisitor<int>>().Object;
            var v4 = new Mock<IReflectionVisitor<int>>().Object;
            var e1 = new Mock<IReflectionElement>();
            var e2 = new Mock<IReflectionElement>();
            var e3 = new Mock<IReflectionElement>();
            e1.Setup(x => x.Accept(v1)).Returns(v2);
            e2.Setup(x => x.Accept(v2)).Returns(v3);
            e3.Setup(x => x.Accept(v3)).Returns(v4);

            var elements = new[] { e1.Object, e2.Object, e3.Object };

            // Exercise system
            var actual = elements.Accept(v1);

            // Verify outcome
            var expected = v4;
            Assert.Equal(expected, actual);

            // Teardown
        }

        [Fact]
        public void GetPropertiesAndFieldsThrowsOnNullType()
        {
            var e = Assert.Throws<ArgumentNullException>(() =>
                ReflectionElementEnvy.GetPropertiesAndFields(null, BindingFlags.Default));
            Assert.Equal("type", e.ParamName);
        }

        [Theory, ClassData(typeof(GetPropertiesAndFieldsTestCases))]
        public void GetPropertiesAndFieldsReturnsTheCorrectResults(
            Type type, BindingFlags bindingAttr, IEnumerable<IReflectionElement> expectedElements)
        {
            var actual = type.GetPropertiesAndFields(bindingAttr);
            Assert.Equal(expectedElements, actual);
        }

        static void AssertUnorderedElementsEqual<TConcreteElement, TKey>(
            IEnumerable<IReflectionElement> expected,
            IEnumerable<IReflectionElement> actual,
            Func<TConcreteElement, TKey> orderBy)
        {
            Assert.Equal(
                expected.Cast<TConcreteElement>().OrderBy(orderBy),
                actual.Cast<TConcreteElement>().OrderBy(orderBy));
        }

        private class GetProperMethodsTestCases : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                var type = typeof(TypeWithStaticAndInstanceMembers<int>);
                var methods = new Methods<TypeWithStaticAndInstanceMembers<int>>(); 

                yield return new object[]
                {
                    type,
                    BindingFlags.Public | BindingFlags.Instance,
                    new IReflectionElement[]
                    {
                        new MethodInfoElement(methods.Select(t => t.PublicMethod())),
                        new MethodInfoElement(type.GetMethod("ToString")),
                        new MethodInfoElement(type.GetMethod("Equals")),
                        new MethodInfoElement(type.GetMethod("GetHashCode")),
                        new MethodInfoElement(type.GetMethod("GetType")),
                    }
                };

                yield return new object[]
                {
                    type,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static,
                    new IReflectionElement[]
                    {
                        new MethodInfoElement(methods.Select(t => t.PublicMethod())),
                        new MethodInfoElement(type.GetMethod("PublicStaticVoidMethod")),
                        new MethodInfoElement(type.GetMethod("ToString")),
                        new MethodInfoElement(type.GetMethod("Equals")),
                        new MethodInfoElement(type.GetMethod("GetHashCode")),
                        new MethodInfoElement(type.GetMethod("GetType")),
                    }
                };
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class GetPropertiesAndFieldsTestCases : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                var type = typeof (TypeWithStaticAndInstanceMembers<int>);
                var properties = new Properties<TypeWithStaticAndInstanceMembers<int>>();
                var fields = new Fields<TypeWithStaticAndInstanceMembers<int>>();
                yield return new object[]
                {
                    typeof (TypeWithStaticAndInstanceMembers<int>),
                    BindingFlags.Public | BindingFlags.Instance,
                    new IReflectionElement[]
                    {
                        new PropertyInfoElement(properties.Select(i => i.PublicReadOnlyProperty)),
                        new PropertyInfoElement(properties.Select(i => i.PublicProperty)),
                        new FieldInfoElement(fields.Select(i => i.PublicReadOnlyField)),
                        new FieldInfoElement(fields.Select(i => i.PublicField)), 
                    }
                };

                yield return new object[]
                {
                    typeof(TypeWithStaticAndInstanceMembers<int>),
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static,
                    new IReflectionElement[]
                    {
                        new PropertyInfoElement(type.GetProperty("PublicStaticReadOnlyProperty")),
                        new PropertyInfoElement(type.GetProperty("PublicStaticProperty")),
                        new PropertyInfoElement(properties.Select(i => i.PublicReadOnlyProperty)),
                        new PropertyInfoElement(properties.Select(i => i.PublicProperty)),
                        new FieldInfoElement(fields.Select(i => i.PublicReadOnlyField)),
                        new FieldInfoElement(fields.Select(i => i.PublicField)),
                        new FieldInfoElement(type.GetField("PublicStaticField")),
                        new FieldInfoElement(type.GetField("PublicStaticReadOnlyFieldWithDefault")),
                    }
                };
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        [Fact]
        public void GetPublicPropertiesAndFieldsThrowsOnNullType()
        {
            var e = Assert.Throws<ArgumentNullException>(() =>
                ReflectionElementEnvy.GetPublicPropertiesAndFields(null));

            Assert.Equal("type", e.ParamName);
        }

        [Fact]
        public void GetPublicPropertiesAndFieldsYieldsStaticAndInstanceElements()
        {
            // Fixture setup
            var type = typeof(TypeWithStaticAndInstanceMembers<int>);
            var properties = new Properties<TypeWithStaticAndInstanceMembers<int>>();
            var fields = new Fields<TypeWithStaticAndInstanceMembers<int>>();
            var expectedElements = new IReflectionElement[]
            {
                new PropertyInfoElement(type.GetProperty("PublicStaticReadOnlyProperty")),
                new PropertyInfoElement(type.GetProperty("PublicStaticProperty")),
                new PropertyInfoElement(properties.Select(i => i.PublicReadOnlyProperty)),
                new PropertyInfoElement(properties.Select(i => i.PublicProperty)),
                new FieldInfoElement(fields.Select(i => i.PublicReadOnlyField)),
                new FieldInfoElement(fields.Select(i => i.PublicField)),
                new FieldInfoElement(type.GetField("PublicStaticField")),
                new FieldInfoElement(type.GetField("PublicStaticReadOnlyFieldWithDefault")),
            };

            // Exercise system
            var actualElements = type.GetPublicPropertiesAndFields();

            // Verify outcome
            Assert.Equal(expectedElements, actualElements);

            // Fixture teardown
        }

       public class TypeWithStaticAndInstanceMembers<TValue>
        {
            public static TValue PublicStaticVoidMethod()
            {
                return default(TValue);
            }

            internal static TValue InternalStaticMethod()
            {
                return default(TValue);
            }

            protected static TValue ProtectedStaticMethod()
            {
                return default(TValue);
            }

            public TValue PublicMethod()
            {
                return default(TValue);
            }

            protected internal TValue ProtectedInternalMethod()
            {
                return default(TValue);
            }

            protected virtual TValue ProtectedVirtualMethod()
            {
                return default(TValue);
            }

            internal virtual TValue InternalVirtualMethod()
            {
                return default(TValue);
            }

#pragma warning disable 414
            static TypeWithStaticAndInstanceMembers()
            {
                PublicStaticReadOnlyProperty = default(TValue);
                PrivateStaticReadOnlyFieldWithDefault = default(TValue);
                PrivateStaticField = default(TValue);
                _protectedInternalStaticProperty = default(TValue);
                ProtectedInternalStaticProperty = default(TValue);
            }

            // Static fields
            private static readonly TValue PrivateStaticReadOnlyFieldWithDefault;
            private static TValue PrivateStaticField;
            public static TValue PublicStaticField;
            public static readonly TValue PublicStaticReadOnlyFieldWithDefault = default(TValue);
            private static TValue _protectedInternalStaticProperty;

            // Static properties
            private static TValue PrivateStaticProperty { get; set; }
            internal static TValue InternalStaticProperty { get; set; }
            protected internal static TValue ProtectedInternalStaticProperty { get; set; }
            public static TValue PublicStaticReadOnlyProperty { get; private set; }
            public static TValue PublicStaticProperty { get; set; }

            // Instance fields
            public readonly TValue PublicReadOnlyField;
            public TValue PublicField = default(TValue);
            private TValue privateField;
            private readonly TValue privateReadOnlyField;

            // Instance Properties
            public TValue PublicReadOnlyProperty { get; private set; }
            public TValue PublicProperty { get; set; }
            internal TValue InternalProperty { get; set; }

            public TypeWithStaticAndInstanceMembers(
                TValue publicReadOnlyField,
                TValue privateField,
                TValue privateReadOnlyField,
                TValue publicReadOnlyProperty)
            {
                PublicReadOnlyField = publicReadOnlyField;
                PublicReadOnlyProperty = publicReadOnlyProperty;
                this.privateReadOnlyField = privateReadOnlyField;
                this.privateField = privateField;
            }
        }
#pragma warning restore 414

    }
}
