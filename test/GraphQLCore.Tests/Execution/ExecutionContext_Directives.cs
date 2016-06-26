﻿namespace GraphQLCore.Tests.Execution
{
    using GraphQLCore.Type;
    using Microsoft.CSharp.RuntimeBinder;
    using NUnit.Framework;

    [TestFixture]
    public class ExecutionContext_Directives
    {
        private GraphQLSchema schema;

        [Test]
        public void Execute_FragmentWithFalsyInclude_DoesntPrintFragment()
        {
            dynamic result = this.schema.Execute(@"
            query fetch {
                nested {
                    ...frag
                }
            }

            fragment frag on NestedQueryType @include(if : false) {
                a
                b
            }
            ");

            Assert.Throws<RuntimeBinderException>(new TestDelegate(() => { string a = result.nested.a; }));
            Assert.Throws<RuntimeBinderException>(new TestDelegate(() => { string b = result.nested.b; }));
        }

        [Test]
        public void Execute_FragmentWithFalsySkip_PrintsFragment()
        {
            dynamic result = this.schema.Execute(@"
            query fetch {
                nested {
                    ...frag
                }
            }

            fragment frag on NestedQueryType @skip(if : false) {
                a
                b
            }
            ");

            Assert.AreEqual("1", result.nested.a);
            Assert.AreEqual("2", result.nested.b);
        }

        [Test]
        public void Execute_FragmentWithTruthyInclude_PrintsFragment()
        {
            dynamic result = this.schema.Execute(@"
            query fetch {
                nested {
                    ...frag
                }
            }

            fragment frag on NestedQueryType @include(if : true) {
                a
                b
            }
            ");

            Assert.AreEqual("1", result.nested.a);
            Assert.AreEqual("2", result.nested.b);
        }

        [Test]
        public void Execute_FragmentWithTruthySkip_DoesntPrintFragment()
        {
            dynamic result = this.schema.Execute(@"
            query fetch {
                nested {
                    ...frag
                }
            }

            fragment frag on NestedQueryType @skip(if : true) {
                a
                b
            }
            ");

            Assert.Throws<RuntimeBinderException>(new TestDelegate(() => { string a = result.nested.a; }));
            Assert.Throws<RuntimeBinderException>(new TestDelegate(() => { string b = result.nested.b; }));
        }

        [Test]
        public void Execute_ScalarBasedFalsyIncludeAndFalsySkipDirective_PrintsTheField()
        {
            dynamic result = this.schema.Execute("{ a, b @include(if: false) @skip(if: false) }");

            Assert.AreEqual("world", result.a);
            Assert.Throws<RuntimeBinderException>(new TestDelegate(() => { string b = result.b; }));
        }

        [Test]
        public void Execute_ScalarBasedFalsyIncludeAndTruthySkipDirective_DoesntPrintTheField()
        {
            dynamic result = this.schema.Execute("{ a, b @include(if: false) @skip(if: true) }");

            Assert.AreEqual("world", result.a);
            Assert.Throws<RuntimeBinderException>(new TestDelegate(() => { string b = result.b; }));
        }

        [Test]
        public void Execute_ScalarBasedFalsyIncludeDirective_DoesntPrintTheField()
        {
            dynamic result = this.schema.Execute("{ a, b @include(if: false) }");

            Assert.AreEqual("world", result.a);
            Assert.Throws<RuntimeBinderException>(new TestDelegate(() => { string b = result.b; }));
        }

        [Test]
        public void Execute_ScalarBasedFalsySkipDirective_PrintsTheField()
        {
            dynamic result = this.schema.Execute("{ a, b @skip(if: false) }");

            Assert.AreEqual("world", result.a);
            Assert.AreEqual("test", result.b);
        }

        [Test]
        public void Execute_ScalarBasedTruthyIncludeAndFalsySkipDirective_PrintsTheField()
        {
            dynamic result = this.schema.Execute("{ a, b @include(if: true) @skip(if: false) }");

            Assert.AreEqual("world", result.a);
            Assert.AreEqual("test", result.b);
        }

        [Test]
        public void Execute_ScalarBasedTruthyIncludeAndTruthySkipDirective_DoesntPrintTheField()
        {
            dynamic result = this.schema.Execute("{ a, b @include(if: true) @skip(if: true) }");

            Assert.AreEqual("world", result.a);
            Assert.Throws<RuntimeBinderException>(new TestDelegate(() => { string b = result.b; }));
        }

        [Test]
        public void Execute_ScalarBasedTruthyIncludeDirective_PrintsTheField()
        {
            dynamic result = this.schema.Execute("{ a, b @include(if: true) }");

            Assert.AreEqual("world", result.a);
            Assert.AreEqual("test", result.b);
        }

        [Test]
        public void Execute_ScalarBasedTruthySkipDirective_DoesntPrintTheField()
        {
            dynamic result = this.schema.Execute("{ a, b @skip(if: true) }");

            Assert.AreEqual("world", result.a);
            Assert.Throws<RuntimeBinderException>(new TestDelegate(() => { string b = result.b; }));
        }

        [SetUp]
        public void SetUp()
        {
            this.schema = new GraphQLSchema();

            var rootType = new GraphQLObjectType("RootQueryType", "", this.schema);
            rootType.Field("a", () => "world");
            rootType.Field("b", () => "test");

            var nestedType = new GraphQLObjectType("NestedQueryType", "", this.schema);
            nestedType.Field("a", () => "1");
            nestedType.Field("b", () => "2");

            rootType.Field("nested", () => nestedType);

            this.schema.SetRoot(rootType);
        }
    }
}