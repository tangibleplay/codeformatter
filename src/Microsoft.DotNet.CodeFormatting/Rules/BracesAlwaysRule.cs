// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.CodeFormatting.Rules
{
    [SyntaxRule(BracesAlwaysRule.Name, BracesAlwaysRule.Description, SyntaxRuleOrder.BracesAlwaysRule)]
    internal sealed class BracesAlwaysRule : CSharpOnlyFormattingRule, ISyntaxFormattingRule
    {
        internal const string Name = "BracesAlwaysRule";
        internal const string Description = "Ensure all blocks are enclosed by braces";

        private static readonly HashSet<Type> kDontReplaceSyntaxTypes = new HashSet<Type>() {
            typeof(BlockSyntax),
        };

        public SyntaxNode Process(SyntaxNode syntaxNode, string languageName)
        {
            while (true) {
                bool replaced = false;
                replaced = replaced || ReplaceIfStatement(ref syntaxNode);
                replaced = replaced || ReplaceForStatement(ref syntaxNode);
                replaced = replaced || ReplaceForEachStatement(ref syntaxNode);

                if (!replaced) {
                    break;
                }
            }

            return syntaxNode;
        }

        private static bool ReplaceIfStatement(ref SyntaxNode syntaxNode) {
            foreach (var ifStatement in syntaxNode.DescendantNodes().OfType<IfStatementSyntax>()) {
                var type = ifStatement.Statement.GetType();
                if (!kDontReplaceSyntaxTypes.Contains(ifStatement.Statement.GetType())) {
                    syntaxNode = syntaxNode.ReplaceNode(ifStatement.Statement, SyntaxFactory.Block(ifStatement.Statement));
                    return true;
                }

                if (ifStatement.Else != null && !kDontReplaceSyntaxTypes.Contains(ifStatement.Else.Statement.GetType())) {
                    syntaxNode = syntaxNode.ReplaceNode(ifStatement.Else.Statement, SyntaxFactory.Block(ifStatement.Else.Statement));
                    return true;
                }
            }

            return false;
        }

        private static bool ReplaceForStatement(ref SyntaxNode syntaxNode) {
            foreach (var forStatement in syntaxNode.DescendantNodes().OfType<ForStatementSyntax>()) {
                if (!kDontReplaceSyntaxTypes.Contains(forStatement.Statement.GetType())) {
                    syntaxNode = syntaxNode.ReplaceNode(forStatement.Statement, SyntaxFactory.Block(forStatement.Statement));
                    return true;
                }
            }

            return false;
        }

        private static bool ReplaceForEachStatement(ref SyntaxNode syntaxNode) {
            foreach (var forEachStatement in syntaxNode.DescendantNodes().OfType<ForEachStatementSyntax>()) {
                if (!kDontReplaceSyntaxTypes.Contains(forEachStatement.Statement.GetType())) {
                    syntaxNode = syntaxNode.ReplaceNode(forEachStatement.Statement, SyntaxFactory.Block(forEachStatement.Statement));
                    return true;
                }
            }

            return false;
        }
    }
}