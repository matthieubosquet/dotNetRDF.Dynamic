﻿namespace VDS.RDF.Dynamic
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using VDS.RDF;
    using Xunit;

    public class DynamicNodeNodeDictionary
    {
        [Fact]
        public void Get_index_requires_key()
        {
            var g = new Graph();
            var s = g.CreateBlankNode();
            var d = new DynamicNode(s);

            Assert.Throws<ArgumentNullException>(() =>
                d[null as INode]
            );
        }

        [Fact]
        public void Get_index_returns_dynamic_collection()
        {
            var g = new Graph();
            g.LoadFromString(@"
<urn:s> <urn:p> <urn:o> .
");

            var s = g.CreateUriNode(UriFactory.Create("urn:s"));
            var p = g.CreateUriNode(UriFactory.Create("urn:p"));
            var d = new DynamicNode(s);

            var actual = d[p];

            Assert.IsType<DynamicObjectCollection>(actual);
        }

        [Fact]
        public void Set_index_requires_key()
        {
            var g = new Graph();
            var s = g.CreateBlankNode();
            var d = new DynamicNode(s);

            Assert.Throws<ArgumentNullException>(() =>
                d[null as INode] = null
            );
        }

        [Fact]
        public void Set_index_with_null_value_retracts_by_subject_and_predicate()
        {
            var expected = new Graph();
            expected.LoadFromString(@"
<urn:s1> <urn:p2> ""o3"" .
<urn:s2> <urn:s1> ""o6"" .
<urn:s2> <urn:p3> <urn:s1> .
");

            var actual = new Graph();
            actual.LoadFromString(@"
<urn:s1> <urn:p1> ""o1"" .
<urn:s1> <urn:p1> ""o2"" .
<urn:s1> <urn:p2> ""o3"" .
<urn:s2> <urn:s1> ""o6"" .
<urn:s2> <urn:p3> <urn:s1> .
");

            var s = actual.CreateUriNode(UriFactory.Create("urn:s1"));
            var p = actual.CreateUriNode(UriFactory.Create("urn:p1"));
            var d = new DynamicNode(s);

            d[p] = null;

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Set_index_overwrites_by_subject()
        {
            var expected = new Graph();
            expected.LoadFromString(@"
<urn:s1> <urn:p1> ""o"" .
<urn:s1> <urn:p2> ""o3"" .
<urn:s2> <urn:s1> ""o6"" .
<urn:s2> <urn:p3> <urn:s1> .
");

            var actual = new Graph();
            actual.LoadFromString(@"
<urn:s1> <urn:p1> ""o1"" .
<urn:s1> <urn:p1> ""o2"" .
<urn:s1> <urn:p2> ""o3"" .
<urn:s2> <urn:s1> ""o6"" .
<urn:s2> <urn:p3> <urn:s1> .
");

            var s = actual.CreateUriNode(UriFactory.Create("urn:s1"));
            var p = actual.CreateUriNode(UriFactory.Create("urn:p1"));
            var d = new DynamicNode(s);

            d[p] = "o";

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Keys_are_predicate_nodes()
        {
            var g = new Graph();
            g.LoadFromString(@"
<urn:s1> <urn:p1> <urn:o1> .
<urn:s1> <urn:p1> <urn:o2> .
<urn:s1> <urn:p2> <urn:o3> .
<urn:s2> <urn:s1> <urn:o5> .
<urn:s3> <urn:p3> <urn:s1> .
");

            var s = g.CreateUriNode(UriFactory.Create("urn:s1"));
            var p = g.CreateUriNode(UriFactory.Create("urn:p1"));
            var d = new DynamicNode(s);

            var actual = ((IDictionary<INode, object>)d).Keys;
            var expected = g.GetTriplesWithSubject(s).Select(triple => triple.Predicate).Distinct();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Add_requires_key()
        {
            var g = new Graph();
            var s = g.CreateBlankNode();
            var d = new DynamicNode(s);

            Assert.Throws<ArgumentNullException>(() =>
            {
                d.Add(null as INode, null);
            });
        }

        [Fact]
        public void Add_requires_value()
        {
            var g = new Graph();
            var s = g.CreateBlankNode();
            var p = g.CreateBlankNode();
            var d = new DynamicNode(s);

            Assert.Throws<ArgumentNullException>(() =>
            {
                d.Add(p, null);
            });
        }

        [Fact]
        public void Add_rejects_existing_key()
        {
            var g = new Graph();
            g.LoadFromString(@"
<urn:s> <urn:p> <urn:o> .
");

            var s = g.CreateUriNode(UriFactory.Create("urn:s"));
            var p = g.CreateUriNode(UriFactory.Create("urn:p"));
            var d = new DynamicNode(s);

            Assert.Throws<ArgumentException>(() =>
            {
                d.Add(p, 0);
            });
        }

        [Fact]
        public void Add_handles_enumerables()
        {
            var expected = new Graph();
            expected.LoadFromString(@"
<urn:s> <urn:p> ""o1"" .
<urn:s> <urn:p> ""o2"" .
");

            var g = new Graph();
            var s = g.CreateUriNode(UriFactory.Create("urn:s"));
            var p = g.CreateUriNode(UriFactory.Create("urn:p"));
            var d = new DynamicNode(s);

            d.Add(p, new[] { "o1", "o2" });

            Assert.Equal(expected, g);
        }

        [Fact]
        public void Add_rdf_collections_are_not_enumerables()
        {
            var unexpected = new Graph();
            unexpected.LoadFromString(@"
<urn:s> <urn:p> ""a"" .
<urn:s> <urn:p> ""b"" .
<urn:s> <urn:p> ""c"" .
");

            var expected = new Graph();
            expected.LoadFromString(@"
@prefix : <urn:> .

<urn:s> <urn:p> (""a"" ""b"" ""c"") .
");

            var g = new Graph();
            var s = g.CreateUriNode(UriFactory.Create("urn:s"));
            var p = g.CreateUriNode(UriFactory.Create("urn:p"));
            var d = new DynamicNode(s);

            d.Add(p, new RdfCollection(new[] { "a", "b", "c" }));

            Assert.NotEqual(unexpected, g);
            Assert.Equal(expected, g);
        }

        [Fact]
        public void Add_strings_are_not_enumerables()
        {
            var unexpected = new Graph();
            unexpected.LoadFromString(@"
<urn:s> <urn:p> ""a"" .
<urn:s> <urn:p> ""b"" .
<urn:s> <urn:p> ""c"" .
");

            var expected = new Graph();
            expected.LoadFromString(@"
<urn:s> <urn:p> ""abc"" .
");

            var g = new Graph();
            var s = g.CreateUriNode(UriFactory.Create("urn:s"));
            var p = g.CreateUriNode(UriFactory.Create("urn:p"));
            var d = new DynamicNode(s);

            d.Add(p, "abc");

            Assert.NotEqual(unexpected, g);
            Assert.Equal(expected, g);
        }

        [Fact]
        public void Add_handles_key_value_pairs()
        {
            var expected = new Graph();
            expected.LoadFromString(@"
<urn:s> <urn:p> ""o"" .
");

            var g = new Graph();
            var s = g.CreateUriNode(UriFactory.Create("urn:s"));
            var p = g.CreateUriNode(UriFactory.Create("urn:p"));
            var d = new DynamicNode(s);

            ((IDictionary<INode, object>)d).Add(new KeyValuePair<INode, object>(p, "o"));

            Assert.Equal(expected, g);
        }

        [Fact]
        public void Contains_rejects_null_key()
        {
            var g = new Graph();
            var s = g.CreateBlankNode();
            var d = new DynamicNode(s);

            Assert.DoesNotContain(new KeyValuePair<INode, object>(null, null), d);
        }

        [Fact]
        public void Contains_rejects_null_value()
        {
            var g = new Graph();
            var s = g.CreateBlankNode();
            var p = g.CreateBlankNode();
            var d = new DynamicNode(s);

            Assert.DoesNotContain(new KeyValuePair<INode, object>(p, null), d);
        }

        [Fact]
        public void Contains_rejects_missing_key()
        {
            var g = new Graph();
            var s = g.CreateBlankNode();
            var p = g.CreateBlankNode();
            var o = g.CreateBlankNode();
            var d = new DynamicNode(s);

            Assert.DoesNotContain(new KeyValuePair<INode, object>(p, o), d);
        }

        [Fact]
        public void Contains_searches_objects_by_predicate()
        {
            var g = new Graph();
            g.LoadFromString(@"
<urn:s> <urn:p> <urn:o> .
");

            var s = g.CreateUriNode(UriFactory.Create("urn:s"));
            var p = g.CreateUriNode(UriFactory.Create("urn:p"));
            var o = g.CreateUriNode(UriFactory.Create("urn:o"));
            var d = new DynamicNode(s);

            Assert.Contains(new KeyValuePair<INode, object>(p, o), d);
            Assert.DoesNotContain(new KeyValuePair<INode, object>(p, s), d);
        }

        [Fact]
        public void ContainsKey_requires_key()
        {
            var g = new Graph();
            var s = g.CreateBlankNode();
            var d = new DynamicNode(s);

            Assert.Throws<ArgumentNullException>(() =>
            {
                d.ContainsKey(null as INode);
            });
        }

        [Fact]
        public void ContainsKey_searches_predicates_by_subject()
        {
            var g = new Graph();
            g.LoadFromString(@"
<urn:s> <urn:p> <urn:o> .
");

            var s = g.CreateUriNode(UriFactory.Create("urn:s"));
            var p = g.CreateUriNode(UriFactory.Create("urn:p"));
            var o = g.CreateUriNode(UriFactory.Create("urn:o"));
            var d = new DynamicNode(s);

            Assert.False(d.ContainsKey(s));
            Assert.True(d.ContainsKey(p));
            Assert.False(d.ContainsKey(o));
        }

        [Fact]
        public void Copies_pairs_with_dynamic_object_collection_values()
        {
            var g = new Graph();
            g.LoadFromString(@"
<urn:s> <urn:p> <urn:o> .
");

            var s = g.CreateUriNode(UriFactory.Create("urn:s"));
            var p = g.CreateUriNode(UriFactory.Create("urn:p"));
            var o = g.CreateUriNode(UriFactory.Create("urn:o"));
            var d = new DynamicNode(s);
            
            var array = new KeyValuePair<INode, object>[1];

            (d as IDictionary<INode, object>).CopyTo(array, 0);
            var pair = array.Single();
            var objects = pair.Value as DynamicObjectCollection;

            Assert.Equal(pair.Key, p);
            Assert.Equal(o, objects.Single());
        }
    }
}
