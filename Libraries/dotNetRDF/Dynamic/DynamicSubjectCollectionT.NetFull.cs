﻿/*
// <copyright>
// dotNetRDF is free and open source software licensed under the MIT License
// -------------------------------------------------------------------------
// 
// Copyright (c) 2009-2017 dotNetRDF Project (http://dotnetrdf.org/)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is furnished
// to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
*/

namespace VDS.RDF.Dynamic
{
    using System.Collections.Generic;
    using System.Linq;

    public class DynamicSubjectCollection<T> : DynamicSubjectCollection, ICollection<T> where T : INode
    {
        public DynamicSubjectCollection(string predicate, DynamicNode subject) :
            base(
                DynamicHelper.ConvertPredicate(
                    DynamicHelper.ConvertPredicate(
                        predicate,
                        subject.Graph),
                    subject.Graph,
                    subject.BaseUri),
                subject)
        { }

        public void Add(T item)
        {
            base.Add(item);
        }

        public bool Contains(T item)
        {
            return base.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            this.Subjects.Select(Convert).ToArray().CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return base.Remove(item);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return this.Subjects.Select(Convert).GetEnumerator();
        }

        private T Convert(INode value)
        {
            var type = typeof(T);

            if (type.IsSubclassOf(typeof(DynamicNode)))
            {
                var ctor = type.GetConstructor(new[] { typeof(INode) });
                value = ctor.Invoke(new[] { value }) as DynamicNode;
            }

            return (T)value;
        }
    }
}
