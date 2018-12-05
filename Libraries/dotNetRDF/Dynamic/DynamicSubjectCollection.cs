﻿namespace VDS.RDF.Dynamic
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using System.Linq.Expressions;

    public class DynamicSubjectCollection : ICollection<INode>, IDynamicMetaObjectProvider
    {
        protected readonly DynamicNode @object;
        private readonly INode predicate;

        public DynamicSubjectCollection(INode predicate, DynamicNode @object)
        {
            if (predicate is null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            if (@object is null)
            {
                throw new ArgumentNullException(nameof(@object));
            }

            this.@object = @object;
            this.predicate = predicate;
        }

        protected IEnumerable<INode> Subjects
        {
            get
            {
                return
                    from triple
                    in @object.Graph.GetTriplesWithPredicateObject(predicate, @object)
                    select DynamicHelper.ConvertNode(triple.Subject, @object.BaseUri) as INode;
            }
        }

        public int Count => Subjects.Count();

        public bool IsReadOnly => false;

        public void Add(INode item)
        {
            @object.Graph.Assert(DynamicHelper.ConvertObject(item, @object.Graph), predicate, @object);
        }

        public void Clear()
        {
            @object.Graph.Retract(@object.Graph.GetTriplesWithPredicateObject(predicate, @object).ToList());
        }

        public bool Contains(INode item)
        {
            return Subjects.Contains(item);
        }

        public void CopyTo(INode[] array, int index) => Subjects.ToArray().CopyTo(array, index);

        public IEnumerator<INode> GetEnumerator() => Subjects.GetEnumerator();

        public bool Remove(INode item)
        {
            return @object.Graph.Retract(@object.Graph.GetTriplesWithPredicateObject(predicate, @object).WithSubject(DynamicHelper.ConvertObject(item, @object.Graph)).ToList());
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
        {
            return new EnumerableMetaObject(parameter, this);
        }
    }
}
