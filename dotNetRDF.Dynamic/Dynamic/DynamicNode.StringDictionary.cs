﻿namespace Dynamic
{
    using System.Collections.Generic;
    using System.Linq;
    using VDS.RDF;

    public partial class DynamicNode : IDictionary<string, object>
    {
        public object this[string key]
        {
            get => this[DynamicHelper.Convert(key, this.Graph)];
            set => this[DynamicHelper.Convert(key, this.Graph)] = value;
        }

        public ICollection<string> Keys =>
                // TODO: Shorten string as much as possible
                this.Graph.GetTriplesWithSubject(this).Select(t => t.Predicate).Distinct().Select(p => DynamicHelper.ConvertToName(p as IUriNode, this.BaseUri)).ToArray();

        public void Add(string key, object value) => this.Add(DynamicHelper.Convert(key, this.Graph), value);

        public void Add(KeyValuePair<string, object> item) => this.Add(item.Key, item.Value);

        public bool Contains(string key, object value) => this.Contains(DynamicHelper.Convert(key, this.Graph), value);

        public bool Contains(KeyValuePair<string, object> item) => this.Contains(item.Key, item.Value);

        public bool ContainsKey(string key) => this.ContainsKey(DynamicHelper.Convert(key, this.Graph));

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) => (this as IEnumerable<KeyValuePair<string, object>>).ToArray().CopyTo(array, arrayIndex);

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => this.Graph.GetTriplesWithSubject(this).Select(t => t.Predicate.ToString()).Distinct().ToDictionary(p => p, p => this[p]).GetEnumerator();

        public bool Remove(string key) => this.Remove(DynamicHelper.Convert(key, this.Graph));

        public bool Remove(string key, object value) => this.Remove(DynamicHelper.Convert(key, this.Graph), value);

        public bool Remove(KeyValuePair<string, object> item) => this.Remove(item.Key, item.Value);

        public bool TryGetValue(string key, out object value) => this.TryGetValue(DynamicHelper.Convert(key, this.Graph), out value);
    }
}
