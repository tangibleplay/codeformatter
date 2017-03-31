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
    [GlobalSemanticRule(LocalVariableNamingRule.Name, LocalVariableNamingRule.Description, GlobalSemanticRuleOrder.LocalVariableNamingRule)]
    internal class LocalVariableNamingRule : CSharpNamingRule
    {
        internal const string Name = "LocalVariables";
        internal const string Description = "Local variables should not have _ in their name";

        protected override SyntaxNode AddAnnotations(SyntaxNode syntaxNode, out int count)
        {
            return CSharpLocalVariableAnnotationsRewriter.AddAnnotations(syntaxNode, out count);
        }

        protected override string GetNewNameFor(string name)
        {
            return NamingUtil.ConvertToCamelCase(name);
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

            public override SyntaxNode VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
            {
                if (NeedsRewrite(node)) {
                    var list = new List<VariableDeclaratorSyntax>(node.Declaration.Variables.Count);
                    foreach (var v in node.Declaration.Variables)
                    {
                        if (IsGoodLocalVariableName(v.Identifier.Text))
                        {
                            list.Add(v);
                        }
                        else
                        {
                            list.Add(v.WithAdditionalAnnotations(s_markerAnnotation));
                            _count++;
                        }
                    }

                    var declaration = node.Declaration.WithVariables(SyntaxFactory.SeparatedList(list));
                    node = node.WithDeclaration(declaration);
                }

                return node;
            }

            public override SyntaxNode VisitParameter(ParameterSyntax node)
            {
                if (!IsGoodLocalVariableName(node.Identifier.Text)) {
                    node = node.WithAdditionalAnnotations(s_markerAnnotation);
                    _count++;
                }

                return node;
            }

            private static bool NeedsRewrite(LocalDeclarationStatementSyntax localVariableSyntax)
            {
                foreach (var v in localVariableSyntax.Declaration.Variables)
                {
                    if (!IsGoodLocalVariableName(v.Identifier.ValueText))
                    {
                        return true;
                    }
                }

                return false;
            }

            private static bool IsGoodLocalVariableName(string name) {
                if (name.Contains("_"))
                {
                    return false;
                }

                return true;
            }
        }
    }
}
