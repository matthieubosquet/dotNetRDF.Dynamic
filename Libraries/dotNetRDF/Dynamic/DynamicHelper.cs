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
    using System;
    using System.Text.RegularExpressions;
    using VDS.RDF.Nodes;

    internal static class DynamicHelper
    {
        internal static object ConvertNode(INode node, Uri baseUri)
        {
            switch (node.AsValuedNode())
            {
                case IUriNode uriNode:
                case IBlankNode blankNode:
                    return new DynamicNode(node, baseUri);

                case DoubleNode doubleNode:
                    return doubleNode.AsDouble();

                case FloatNode floatNode:
                    return floatNode.AsFloat();

                case DecimalNode decimalNode:
                    return decimalNode.AsDecimal();

                case BooleanNode booleanNode:
                    return booleanNode.AsBoolean();

                case DateTimeNode dateTimeNode:
                    return dateTimeNode.AsDateTimeOffset();

                case TimeSpanNode timeSpanNode:
                    return timeSpanNode.AsTimeSpan();

                case NumericNode numericNode:
                    return numericNode.AsInteger();

                case StringNode stringNode when stringNode.DataType is null && string.IsNullOrEmpty(stringNode.Language):
                    return stringNode.AsString();

                default:
                    return node;
            }
        }

        // TODO: Rename, not just predicates
        internal static Uri ConvertPredicate(string key, IGraph graph)
        {
            if (!DynamicHelper.TryResolveQName(key, graph, out var uri))
            {
                if (!Uri.TryCreate(key, UriKind.RelativeOrAbsolute, out uri))
                {
                    throw new FormatException("Illegal Uri.");
                }
            }

            return uri;
        }

        // TODO: Rename, not just predicates
        internal static INode ConvertPredicate(Uri key, IGraph graph, Uri baseUri)
        {
            if (!key.IsAbsoluteUri)
            {
                if (baseUri is null)
                {
                    throw new InvalidOperationException("Can't use relative uri without baseUri.");
                }

                if (baseUri.AbsoluteUri.EndsWith("#"))
                {
                    var builder = new UriBuilder(baseUri) { Fragment = key.ToString() };

                    key = builder.Uri;
                }
                else
                {
                    key = new Uri(baseUri, key);
                }
            }

            return graph.CreateUriNode(key);
        }

        internal static INode ConvertObject(object value, IGraph graph)
        {
            switch (value)
            {
                case INode nodeValue:
                    return nodeValue.CopyNode(graph);

                case Uri uriValue:
                    return graph.CreateUriNode(uriValue);

                case bool boolValue:
                    return new BooleanNode(graph, boolValue);

                case byte byteValue:
                    return new ByteNode(graph, byteValue);

                case DateTime dateTimeValue:
                    return new DateTimeNode(graph, dateTimeValue);

                case DateTimeOffset dateTimeOffsetValue:
                    return new DateTimeNode(graph, dateTimeOffsetValue);

                case decimal decimalValue:
                    return new DecimalNode(graph, decimalValue);

                case double doubleValue:
                    return new DoubleNode(graph, doubleValue);

                case float floatValue:
                    return new FloatNode(graph, floatValue);

                case long longValue:
                    return new LongNode(graph, longValue);

                case int intValue:
                    return new LongNode(graph, intValue);

                case string stringValue:
                    return new StringNode(graph, stringValue);

                case char charValue:
                    return new StringNode(graph, charValue.ToString());

                case TimeSpan timeSpanValue:
                    return new TimeSpanNode(graph, timeSpanValue);

                default:
                    throw new InvalidOperationException($"Can't convert type {value.GetType()}");
            }
        }

        internal static string ConvertToName(IUriNode node, Uri baseUri)
        {
            var nodeUri = node.Uri;

            if (node.Graph.NamespaceMap.ReduceToQName(nodeUri.AbsoluteUri, out var qname))
            {
                return qname;
            }

            if (baseUri is null)
            {
                return nodeUri.AbsoluteUri;
            }

            if (baseUri.AbsoluteUri.EndsWith("#"))
            {
                return nodeUri.Fragment.TrimStart('#');
            }

            return baseUri.MakeRelativeUri(nodeUri).ToString();
        }

        private static bool TryResolveQName(string index, IGraph graph, out Uri indexUri)
        {
            // TODO: This is naive
            if (index.StartsWith("urn:") || !Regex.IsMatch(index, @"^\w*:\w+$"))
            {
                indexUri = null;
                return false;
            }

            indexUri = graph.ResolveQName(index);
            return true;
        }
    }
}
