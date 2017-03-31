// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.CodeFormatting.Rules
{
    [LocalSemanticRule(ExplicitThisRule.Name, ExplicitThisRule.Description, LocalSemanticRuleOrder.RemoveExplicitThisRule)]
    internal sealed class ExplicitThisRule : CSharpOnlyFormattingRule, ILocalSemanticFormattingRule
    {
        internal const string Name = "ExplicitThis";
        internal const string Description = "Remove explicit this/Me prefixes on expressions except where necessary";

        private sealed class ExplicitThisRewriter : CSharpSyntaxRewriter
        {
            private readonly Document _document;
            private readonly CancellationToken _cancellationToken;
            private SemanticModel _semanticModel;
            private bool _addedAnnotations;

            internal bool AddedAnnotations
            {
                get { return _addedAnnotations; }
            }

            internal ExplicitThisRewriter(Document document, CancellationToken cancellationToken)
            {
                _document = document;
                _cancellationToken = cancellationToken;
            }

            public override SyntaxNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
            {
                node = (MemberAccessExpressionSyntax)base.VisitMemberAccessExpression(node);
                var name = node.Name.Identifier.ValueText;

                if (node.Expression != null)
                {
					bool shouldReduce = ReducableThisNode(node) || ReducableIdentifierNode(node);
					if (shouldReduce)
					{
	                    _addedAnnotations = true;
	                    return node.WithAdditionalAnnotations(Simplifier.Annotation);
					}
                }

                return node;
            }

			private bool ReducableThisNode(MemberAccessExpressionSyntax node)
			{
				if (node.Expression.Kind() != SyntaxKind.ThisExpression)
				{
					return false;
				}

				if (_semanticModel == null)
				{
					_semanticModel = _document.GetSemanticModelAsync(_cancellationToken).Result;
				}

				var symbol = _semanticModel.GetSymbolInfo(node, _cancellationToken).Symbol;
				if (symbol.Kind == SymbolKind.Field || symbol.Kind == SymbolKind.Property)
				{
	                var name = node.Name.Identifier.ValueText;
					if (name.Length <= 0)
					{
						return false;
					}

					// HACK (darren): because we don't explicit this.privateVariable_
					// but we want to allow for this.gameObject, we shouldn't reduce
					// variables which don't end with _
					if (name[name.Length - 1] != '_')
					{
						return false;
					}
				}

				return true;
			}

			private bool ReducableIdentifierNode(MemberAccessExpressionSyntax node)
			{
				if (node.Expression.Kind() != SyntaxKind.IdentifierName)
				{
					return false;
				}

				if (_semanticModel == null)
				{
					_semanticModel = _document.GetSemanticModelAsync(_cancellationToken).Result;
				}

				var symbol = _semanticModel.GetSymbolInfo(node, _cancellationToken).Symbol;
				var containingSymbol = symbol != null ? symbol.ContainingSymbol : null;
				if (containingSymbol != null)
				{
					foreach (var ancestorNode in node.Ancestors()) {
						ISymbol ancestorSymbol = _semanticModel.GetDeclaredSymbol(ancestorNode, _cancellationToken);
						if (ancestorSymbol == containingSymbol)
						{
							// if declared in same class - we can simplify
							return true;
						}
					}

					return false;
				}

				return false;
			}
        }

        public async Task<SyntaxNode> ProcessAsync(Document document, SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            var rewriter = new ExplicitThisRewriter(document, cancellationToken);
            var newNode = rewriter.Visit(syntaxNode);
            if (!rewriter.AddedAnnotations)
            {
                return syntaxNode;
            }

            document = await Simplifier.ReduceAsync(document.WithSyntaxRoot(newNode), cancellationToken: cancellationToken);
            return await document.GetSyntaxRootAsync(cancellationToken);
        }
    }
}
