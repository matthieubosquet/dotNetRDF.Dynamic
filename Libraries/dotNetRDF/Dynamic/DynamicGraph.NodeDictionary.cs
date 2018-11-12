﻿namespace VDS.RDF.Dynamic
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public partial class DynamicGraph : IDictionary<INode, object>
    {
        private IEnumerable<IUriNode> UriSubjectNodes
        {
            get
            {
                return Triples
                    .SubjectNodes
                    .UriNodes();
            }
        }

        private IEnumerable<KeyValuePair<INode, object>> NodePairs
        {
            get
            {
                return UriSubjectNodes.ToDictionary(
                    node => node as INode,
                    node => this[node]);
            }
        }

        public object this[INode key]
        {
            get
            {
                if (key is null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                if (!TryGetValue(key, out var node))
                {
                    throw new KeyNotFoundException();
                }

                return node;
            }

            set
            {
                if (key is null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                Remove(key);

                if (value is null)
                {
                    return;
                }

                Add(key, value);
            }
        }

        ICollection<INode> IDictionary<INode, object>.Keys
        {
            get
            {
                return UriSubjectNodes.ToArray();
            }
        }

        public void Add(INode key, object value)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (ContainsKey(key))
            {
                throw new ArgumentException("An item with the same key has already been added.", nameof(key));
            }

            // Make a copy of the key node local to this graph
            // so dynamic references are resolved correctly
            // (they depend on node's graph)
            var targetNode = new DynamicNode(key.CopyNode(this._g), PredicateBaseUri);

            foreach (DictionaryEntry entry in DynamicGraph.ConvertToDictionary(value))
            {
                targetNode.Add(entry.Key, entry.Value);
            }
        }

        void ICollection<KeyValuePair<INode, object>>.Add(KeyValuePair<INode, object> item)
        {
            Add(item.Key, item.Value);
        }

        bool ICollection<KeyValuePair<INode, object>>.Contains(KeyValuePair<INode, object> item)
        {
            if (item.Key is null)
            {
                // All statements have subject
                return false;
            }

            if (item.Value is null)
            {
                // All statements have object
                return false;
            }

            if (!TryGetValue(item.Key, out var value))
            {
                return false;
            }

            var node = (DynamicNode)value;

            foreach (DictionaryEntry entry in DynamicGraph.ConvertToDictionary(item.Value))
            {
                if (!node.Contains(entry.Key, entry.Value))
                {
                    return false;
                }
            }

            return true;
        }

        public bool ContainsKey(INode key)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return UriSubjectNodes.Contains(key);
        }

        public void CopyTo(KeyValuePair<INode, object>[] array, int arrayIndex)
        {
            NodePairs.ToArray().CopyTo(array, arrayIndex);
        }

        IEnumerator<KeyValuePair<INode, object>> IEnumerable<KeyValuePair<INode, object>>.GetEnumerator()
        {
            return NodePairs.GetEnumerator();
        }

        public bool Remove(INode key)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return Retract(GetTriplesWithSubject(key).ToArray());
        }

        bool ICollection<KeyValuePair<INode, object>>.Remove(KeyValuePair<INode, object> item)
        {
            if (!NodeDictionary.Contains(item))
            {
                return false;
            }

            var node = (DynamicNode)this[item.Key];

            foreach (DictionaryEntry entry in DynamicGraph.ConvertToDictionary(item.Value))
            {
                node.Remove(entry.Key, entry.Value);
            }

            return true;
        }

        public bool TryGetValue(INode key, out object value)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            value = UriSubjectNodes
                .Where(node => node.Equals(key))
                .Select(node => new DynamicNode(node.CopyNode(this._g), this.PredicateBaseUri))
                .SingleOrDefault();

            return value != null;
        }

        private static IDictionary ConvertToDictionary(object value)
        {
            if (value is IDictionary valueDictionary)
            {
                return valueDictionary;
            }

            return DynamicGraph.GetProperties(value).ToDictionary(p => p.Name, p => p.GetValue(value, null));
        }

        private static IEnumerable<PropertyInfo> GetProperties(object value)
        {
            return value
                .GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => !p.GetIndexParameters().Any());
        }
    }
}
