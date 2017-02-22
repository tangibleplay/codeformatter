// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;

namespace Microsoft.DotNet.CodeFormatting.Rules
{
    [GlobalSemanticRule(MethodNamingRule.Name, MethodNamingRule.Description, GlobalSemanticRuleOrder.MethodNamingRule)]
    internal class MethodNamingRule : CSharpNamingRule
    {
        internal const string Name = "MethodNames";
        internal const string Description = "Ensure method names are capitalized";

        protected override SyntaxNode AddPrivateFieldAnnotations(SyntaxNode syntaxNode, out int count)
        {
            return CSharpLocalVariableAnnotationsRewriter.AddAnnotations(syntaxNode, out count);
        }

        protected override string GetNewNameFor(ISymbol symbol)
        {
            return symbol.Name.Captialized();
        }

        /// <summary>
        /// This will add an annotation to any private field that needs to be renamed.
        /// </summary>
        internal sealed class CSharpLocalVariableAnnotationsRewriter : CSharpSyntaxRewriter
        {
            private int _count;

            internal static SyntaxNode AddAnnotations(SyntaxNode node, out int count)
            {
                var rewriter = new CSharpLocalVariableAnnotationsRewriter();
                var newNode = rewriter.Visit(node);
                count = rewriter._count;
                return newNode;
            }

            public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                string methodName = node.Identifier.ToString();
                if (methodName.Length > 0 && char.IsLower(methodName[0])) {
                    node = node.WithAdditionalAnnotations(s_markerAnnotation);
                    _count++;
                }

                return node;
            }
        }
    }
}
