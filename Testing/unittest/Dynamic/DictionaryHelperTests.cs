﻿namespace VDS.RDF.Dynamic
{
    using System;
    using VDS.RDF;
    using Xunit;

    public class DictionaryHelperTests
    {
        [Fact]
        public void Converts_objects_to_native_datatypes()
        {
            var g = new Graph();
            g.LoadFromString(@"
@prefix : <urn:> .
@prefix xsd: <http://www.w3.org/2001/XMLSchema#> .

:s
    :p
        :x ,
        _:blank ,
        0E0 ,
        ""0""^^xsd:float ,
        0.0 ,
        false ,
        ""1900-01-01""^^xsd:dateTime ,
        ""P1D""^^xsd:duration ,
        0 ,
        """" ,
        """"^^:datatype ,
        """"@en ,
        (0 1) .
");

            var s = g.CreateUriNode(":s");
            var p = g.CreateUriNode(":p");
            var d = new DynamicNode(s);
            var objects = new DynamicObjectCollection(d, p);

            Assert.Collection(
                objects,
                item => Assert.IsType<DynamicNode>(item),
                item => Assert.IsType<DynamicNode>(item),
                item => Assert.IsType<double>(item),
                item => Assert.IsType<float>(item),
                item => Assert.IsType<decimal>(item),
                item => Assert.IsType<bool>(item),
                item => Assert.IsType<DateTimeOffset>(item),
                item => Assert.IsType<TimeSpan>(item),
                item => Assert.IsType<long>(item),
                item => Assert.IsType<string>(item),
                item => Assert.IsAssignableFrom<ILiteralNode>(item),
                item => Assert.IsAssignableFrom<ILiteralNode>(item),
                item => Assert.IsType<DynamicCollectionList>(item)
            );
        }

        [Fact]
        public void Ignores_URNs_for_QName_parsing()
        {
            var d = new DynamicGraph();
            d.LoadFromString(@"
<urn:s> <urn:p> <urn:o> .
");

            var s = d.CreateUriNode(UriFactory.Create("urn:s"));

            Assert.Equal(s, d["urn:s"]);
        }

        [Fact]
        public void Ignores_URIs_for_QName_parsing()
        {
            var d = new DynamicGraph();
            d.LoadFromString(@"
<http://example.com/s> <http://example.com/p> <http://example.com/o> .
");

            var s = d.CreateUriNode(UriFactory.Create("http://example.com/s"));

            Assert.Equal(s, d["http://example.com/s"]);
        }

        [Fact]
        public void Expands_QNames()
        {
            var d = new DynamicGraph();
            d.LoadFromString(@"
@prefix u: <urn:> .

u:s u:p u:o .
");

            var s = d.CreateUriNode("u:s");

            Assert.Equal(s, d["u:s"]);
        }

        [Fact]
        public void Rejects_invalid_URIs()
        {
            var d = new DynamicGraph();

            Assert.Throws<FormatException>(() =>
                d["http:///"]
            );
        }

        [Fact]
        public void Rejects_relative_URI_without_base()
        {
            var d = new DynamicGraph();
            d.LoadFromString(@"
<urn:s> <urn:p> <urn:o> .
");

            var s = d.CreateUriNode(UriFactory.Create("urn:s"));

            Assert.Throws<InvalidOperationException>(() =>
                Assert.Equal(s, d["/s"])
            );
        }

        [Fact]
        public void Expands_hash_URI_base()
        {
            var d = new DynamicGraph { BaseUri = UriFactory.Create("http://example.com/#") };
            d.LoadFromString(@"
<http://example.com/#s> <http://example.com/#p> <http://example.com/#o> .
");

            var s = d.CreateUriNode(UriFactory.Create("http://example.com/#s"));

            Assert.Equal(s, d["s"]);
        }

        [Fact]
        public void Expands_slash_URI_base()
        {
            var d = new DynamicGraph { BaseUri = UriFactory.Create("http://example.com/") };
            d.LoadFromString(@"
<http://example.com/s> <http://example.com/p> <http://example.com/o> .
");

            var s = d.CreateUriNode(UriFactory.Create("http://example.com/s"));

            Assert.Equal(s, d["s"]);
        }

        [Fact]
        public void Converts_native_data_types()
        {
            var expected = new Graph();
            expected.LoadFromString(@"
@prefix xsd: <http://www.w3.org/2001/XMLSchema#> .
@prefix : <urn:> .

:s
    :p
        _:o ,
        :o ,
        true ,
        ""255""^^xsd:unsignedByte ,
        ""9999-12-31T23:59:59.999999""^^xsd:dateTime ,
        ""9999-12-31T23:59:59.999999+00:00""^^xsd:dateTime ,
        ""79228162514264337593543950335""^^xsd:decimal ,
        ""1.79769313486232E+308""^^xsd:double ,
        ""3.402823E+38""^^xsd:float ,
        ""9223372036854775807""^^xsd:integer ,
        ""2147483647""^^xsd:integer ,
        """" ,
        ""\uFFFF"" ,
        ""P10675199DT2H48M5.4775807S""^^xsd:duration ,
        (0 1) .
");

            var d = new DynamicGraph { BaseUri = UriFactory.Create("urn:") };

            d["s"] = new
            {
                p = new object[] {
                    new NodeFactory().CreateBlankNode(),
                    UriFactory.Create("urn:o"),
                    true,
                    byte.MaxValue,
                    DateTime.MaxValue,
                    DateTimeOffset.MaxValue,
                    decimal.MaxValue,
                    double.MaxValue,
                    float.MaxValue,
                    long.MaxValue,
                    int.MaxValue,
                    string.Empty,
                    char.MaxValue,
                    TimeSpan.MaxValue,
                    new RdfCollection(0, 1)
                }
            };

            Assert.Equal(expected as IGraph, d as IGraph);
        }

        [Fact]
        public void Rejects_unknown_data_types()
        {
            var d = new DynamicGraph { BaseUri = UriFactory.Create("urn:") };

            Assert.Throws<InvalidOperationException>(() =>
                d["s"] = new
                {
                    p = new { }
                }
            );
        }

        [Fact]
        public void Reduces_QNames()
        {
            var d = new DynamicGraph();
            d.LoadFromString(@"
@prefix : <urn:> .

:s :p :o .
");

            Assert.Equal(new[] { ":s", ":o" }, d.Keys);
        }

        [Fact]
        public void Leaves_absoulte_URIs_without_base()
        {
            var d = new DynamicGraph();
            d.LoadFromString(@"
<urn:s> <urn:p> <urn:o> .
");

            Assert.Equal(new[] { "urn:s", "urn:o" }, d.Keys);
        }

        [Fact]
        public void Reduces_hash_base_URIs()
        {
            var d = new DynamicGraph { BaseUri = UriFactory.Create("http://example.com/#") };
            d.LoadFromString(@"
<http://example.com/#s> <http://example.com/#p> <http://example.com/#o> .
");

            Assert.Equal(new[] { "s", "o" }, d.Keys);
        }

        [Fact]
        public void Reduces_base_URIs()
        {
            var d = new DynamicGraph { BaseUri = UriFactory.Create("urn:") };
            d.LoadFromString(@"
<urn:s> <urn:p> <urn:o> .
");

            Assert.Equal(new[] { "s", "o" }, d.Keys);
        }
    }
}
