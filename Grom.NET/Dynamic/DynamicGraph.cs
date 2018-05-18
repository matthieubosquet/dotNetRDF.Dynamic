﻿namespace Dynamic
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using VDS.RDF;

    public class DynamicGraph : WrapperGraph, IDynamicMetaObjectProvider
    {
        internal readonly Uri subjectBaseUri;
        internal readonly Uri predicateBaseUri;
        internal readonly bool collapseSingularArrays;

        public DynamicGraph(IGraph graph, Uri subjectBaseUri = null, Uri predicateBaseUri = null, bool collapseSingularArrays = false) : base(graph)
        {
            this.subjectBaseUri = subjectBaseUri ?? this.BaseUri;
            this.predicateBaseUri = predicateBaseUri ?? this.subjectBaseUri;
            this.collapseSingularArrays = collapseSingularArrays;
        }

        public DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new DynamicGraphMetaObject(parameter, this);
        }
    }

    public class DynamicGraphMetaObject : DynamicMetaObject
    {
        private readonly DynamicGraph d;

        public DynamicGraphMetaObject(Expression expression, DynamicGraph d) : base(expression, BindingRestrictions.Empty, d)
        {
            this.d = d;
        }

        public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
        {
            if (indexes.Length != 1)
            {
                throw new ArgumentException("Only one index", "indexes");
            }

            var subjectIndex = indexes[0].Value;

            if (subjectIndex == null)
            {
                throw new ArgumentNullException("Can't work with null index", "indexes");
            }

            var result = GetIndex(subjectIndex);

            if (result == null)
            {
                // TODO: What type should this be?
                throw new Exception();
            }

            return new DynamicMetaObject(Expression.Constant(result), BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            if (this.d.subjectBaseUri == null)
            {
                throw new InvalidOperationException("Can't get member without baseUri.");
            }

            var result = this.GetIndex(binder.Name);

            return new DynamicMetaObject(Expression.Constant(result), BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }

        public override DynamicMetaObject BindSetIndex(SetIndexBinder binder, DynamicMetaObject[] indexes, DynamicMetaObject dvalue)
        {
            if (indexes.Length != 1)
            {
                throw new ArgumentException("Only one index", "indexes");
            }

            SetIndex(indexes[0].Value, dvalue.Value);

            return new DynamicMetaObject(Expression.Constant(dvalue.Value), BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }

        public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
        {
            if (this.d.subjectBaseUri == null)
            {
                throw new InvalidOperationException("Can't set member without baseUri.");
            }

            this.SetIndex(binder.Name, value.Value);

            return new DynamicMetaObject(Expression.Constant(value.Value), BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }

        private DynamicNode GetIndex(object subject)
        {
            var subjectNode = DynamicHelper.ConvertToNode(subject, this.d, this.d.subjectBaseUri);

            return this.d.Triples.SubjectNodes
                .UriNodes()
                .Where(node => node.Equals(subjectNode))
                .Select(node => new DynamicNode(node, this.d.predicateBaseUri, this.d.collapseSingularArrays))
                .SingleOrDefault();
        }

        private void SetIndex(object subjectIndex, object value)
        {
            var result = this.GetIndex(subjectIndex);
            if (result == null)
            {
                var subjectNode = DynamicHelper.ConvertToNode(subjectIndex, this.d, this.d.subjectBaseUri);
                result = new DynamicNode(subjectNode, this.d.predicateBaseUri, this.d.collapseSingularArrays);
            }

            var subjectWrapper = result as DynamicNode;

            if (value == null)
            {
                this.d.Retract(this.d.GetTriplesWithSubject(subjectWrapper.graphNode).ToArray());
            }
            else
            {
                if (!(value is IDictionary valueDictionary))
                {
                    valueDictionary = new Dictionary<object, object>();

                    var properties = value.GetType()
                        .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty)
                        .Where(p => p.GetIndexParameters().Count() == 0);

                    if (!properties.Any())
                    {
                        throw new ArgumentException($"Value type {value.GetType()} for subject {subjectIndex} lacks readable public instance properties.", "value");
                    }

                    foreach (var property in properties)
                    {
                        valueDictionary[property.Name] = property.GetValue(value);
                    }
                }

                foreach (var key in valueDictionary.Keys)
                {
                    subjectWrapper.TrySetIndex(null, new[] { key }, valueDictionary[key]);
                }
            }
        }
    }

    //class a : BaseNode { }
    public class aDynamicGraph : DynamicObject
    {
        private readonly IGraph graph;
        private readonly Uri subjectBaseUri;
        private readonly Uri predicateBaseUri;
        private readonly bool collapseSingularArrays;

        public aDynamicGraph(IGraph graph, Uri subjectBaseUri = null, Uri predicateBaseUri = null, bool collapseSingularArrays = false)
        {
            this.graph = graph ?? throw new ArgumentNullException(nameof(graph));
            this.subjectBaseUri = subjectBaseUri ?? this.graph.BaseUri;
            this.predicateBaseUri = predicateBaseUri ?? this.subjectBaseUri;
            this.collapseSingularArrays = collapseSingularArrays;
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            if (indexes.Length != 1)
            {
                throw new ArgumentException("Only one index", "indexes");
            }

            var subjectIndex = indexes[0];

            if (subjectIndex == null)
            {
                throw new ArgumentNullException("Can't work with null index", "indexes");
            }

            var subjectNode = DynamicHelper.ConvertToNode(subjectIndex, this.graph, this.subjectBaseUri);

            result = this.graph.Triples.SubjectNodes
                .UriNodes()
                .Where(node => node.Equals(subjectNode))
                .Select(node => new DynamicNode(node, this.predicateBaseUri, this.collapseSingularArrays))
                .SingleOrDefault();

            return result != null;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (this.subjectBaseUri == null)
            {
                throw new InvalidOperationException("Can't get member without baseUri.");
            }

            return this.TryGetIndex(null, new[] { binder.Name }, out result);
        }

        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            if (indexes.Length != 1)
            {
                throw new ArgumentException("Only one index", "indexes");
            }

            var subjectIndex = indexes[0];

            if (!this.TryGetIndex(null, indexes, out object result))
            {
                var subjectNode = DynamicHelper.ConvertToNode(subjectIndex, this.graph, this.subjectBaseUri);
                result = new DynamicNode(subjectNode, this.predicateBaseUri, this.collapseSingularArrays);
            }

            var subjectWrapper = result as DynamicNode;

            if (value == null)
            {
                this.graph.Retract(this.graph.GetTriplesWithSubject(subjectWrapper.graphNode).ToArray());

                return true;
            }

            if (!(value is IDictionary valueDictionary))
            {
                valueDictionary = new Dictionary<object, object>();

                var properties = value.GetType()
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty)
                    .Where(p => p.GetIndexParameters().Count() == 0);

                if (!properties.Any())
                {
                    throw new ArgumentException($"Value type {value.GetType()} for subject {subjectIndex} lacks readable public instance properties.", "value");
                }

                foreach (var property in properties)
                {
                    valueDictionary[property.Name] = property.GetValue(value);
                }
            }

            foreach (var key in valueDictionary.Keys)
            {
                subjectWrapper.TrySetIndex(null, new[] { key }, valueDictionary[key]);
            }

            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (this.subjectBaseUri == null)
            {
                throw new InvalidOperationException("Can't set member without baseUri.");
            }

            return this.TrySetIndex(null, new[] { binder.Name }, value);
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            var subjects = this.graph
                .Triples
                .Select(triple => triple.Subject)
                .UriNodes()
                .Distinct();

            return DynamicHelper.ConvertToNames(subjects, this.subjectBaseUri);
        }

        public IEnumerable<DynamicNode> BlankNodes()
        {
            return this.graph
                .Triples
                .Select(t => t.Subject)
                .BlankNodes()
                .Select(n => new DynamicNode(n, this.predicateBaseUri, this.collapseSingularArrays));
        }
    }
}
