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
    [GlobalSemanticRule(PropertyNamingRule.Name, PropertyNamingRule.Description, GlobalSemanticRuleOrder.PropertyNamingRule)]
    internal class PropertyNamingRule : CSharpNamingRule
    {
        internal const string Name = "PropertyNames";
        internal const string Description = "Private properties are named like PropertyName_";

        protected override SyntaxNode AddAnnotations(SyntaxNode syntaxNode, out int count)
        {
            return CSharpPropertyAnnotationsRewriter.AddAnnotations(document_, cancellationToken_, syntaxNode, out count);
        }

        protected override string GetNewNameFor(string name)
        {
            // NOTE (darren): don't rename things that are named like a constant already
            // this is to allow naming static readonly fields like a constant
            bool isNamedLikeAConstant = name.Length >= 2 && name[0] == 'k' && char.IsUpper(name[1]);
            if (isNamedLikeAConstant)
            {
                return name;
            }

            string camelCaseName = NamingUtil.ConvertToCamelCase(name);
            return camelCaseName.Captialized() + "_";
        }

        /// <summary>
        /// This will add an annotation to any private field that needs to be renamed.
        /// </summary>
        internal sealed class CSharpPropertyAnnotationsRewriter : CSharpSyntaxRewriter
        {
            private int _count;

			private Document document_;
			private CancellationToken cancellationToken_;

			private SemanticModel semanticModel_;

            internal static SyntaxNode AddAnnotations(Document document, CancellationToken cancellationToken, SyntaxNode node, out int count)
            {
                var rewriter = new CSharpPropertyAnnotationsRewriter(document, cancellationToken);
                var newNode = rewriter.Visit(node);
                count = rewriter._count;
                return newNode;
            }

			public CSharpPropertyAnnotationsRewriter(Document document, CancellationToken cancellationToken) {
				document_ = document;
				cancellationToken_ = cancellationToken;
			}

            public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
            {
                if (node.Modifiers.Any(m => m.Kind() == SyntaxKind.PublicKeyword))
                {
                    return node;
                }

				if (semanticModel_ == null)
				{
					semanticModel_ = document_.GetSemanticModelAsync(cancellationToken_).Result;
				}

				var symbol = semanticModel_.GetDeclaredSymbol(node, cancellationToken_);
				if (symbol != null)
				{
					var containingSymbol = symbol.ContainingSymbol as ITypeSymbol;
					if (containingSymbol != null)
					{
						// don't rename if not declared inside a class
						if (containingSymbol != null && containingSymbol.TypeKind != TypeKind.Class)
						{
							return node;
						}
					}
				}

                if (IsSerializedField(node))
                {
                    return node;
                }

                node = node.WithAdditionalAnnotations(s_markerAnnotation);
                _count++;
                return node;
            }

            private static bool IsSerializedField(PropertyDeclarationSyntax node) {
                foreach (AttributeListSyntax attributeList in node.AttributeLists)
                {
                    if (attributeList.DescendantNodes().OfType<AttributeSyntax>().Any(a => a.Name.ToString() == "SerializeField"))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
