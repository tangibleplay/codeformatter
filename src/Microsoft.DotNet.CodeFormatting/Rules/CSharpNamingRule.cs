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
    abstract class CSharpNamingRule : CSharpOnlyFormattingRule, IGlobalSemanticFormattingRule
    {
        private const string s_renameAnnotationName = "RenameAnnotation";
        protected readonly static SyntaxAnnotation s_markerAnnotation = new SyntaxAnnotation("RenameMarkerAnnotation");

		protected Document document_;
		protected CancellationToken cancellationToken_;

        public async Task<Solution> ProcessAsync(Document document, SyntaxNode syntaxRoot, CancellationToken cancellationToken)
        {
			document_ = document;
			cancellationToken_ = cancellationToken;

            int count;
            var newSyntaxRoot = AddAnnotations(syntaxRoot, out count);

            if (count == 0)
            {
                return document.Project.Solution;
            }

            var documentId = document.Id;
            var solution = document.Project.Solution;
            solution = solution.WithDocumentSyntaxRoot(documentId, newSyntaxRoot);
            solution = await Rename(solution, documentId, count, cancellationToken);
            return solution;
        }

        protected abstract SyntaxNode AddAnnotations(SyntaxNode syntaxNode, out int count);

        protected abstract string GetNewNameFor(string name);

        private async Task<Solution> Rename(Solution solution, DocumentId documentId, int count, CancellationToken cancellationToken)
        {
            Solution oldSolution = null;
            for (int i = 0; i < count; i++)
            {
                oldSolution = solution;

                var semanticModel = await solution.GetDocument(documentId).GetSemanticModelAsync(cancellationToken);
                var root = await semanticModel.SyntaxTree.GetRootAsync(cancellationToken);
                var declaration = root.GetAnnotatedNodes(s_markerAnnotation).ElementAt(i);

                // Make note, VB represents "fields" marked as "WithEvents" as properties, so don't be
                // tempted to treat this as a IFieldSymbol. We only need the name, so ISymbol is enough.
                var symbol = semanticModel.GetDeclaredSymbol(declaration, cancellationToken);
                var newName = GetNewNameFor(symbol.Name);

                // Can happen with pathologically bad field names like _
                if (newName == symbol.Name)
                {
                    continue;
                }

                solution = await Renamer.RenameSymbolAsync(solution, symbol, newName, solution.Workspace.Options, cancellationToken).ConfigureAwait(false);
                solution = await CleanSolutionAsync(solution, oldSolution, cancellationToken);
            }

            return solution;
        }

        private async Task<Solution> CleanSolutionAsync(Solution newSolution, Solution oldSolution, CancellationToken cancellationToken)
        {
            var solution = newSolution;

            foreach (var projectChange in newSolution.GetChanges(oldSolution).GetProjectChanges())
            {
                foreach (var documentId in projectChange.GetChangedDocuments())
                {
                    solution = await CleanSolutionDocument(solution, documentId, cancellationToken);
                }
            }

            return solution;
        }

        private async Task<Solution> CleanSolutionDocument(Solution solution, DocumentId documentId, CancellationToken cancellationToken)
        {
            var document = solution.GetDocument(documentId);
            var syntaxNode = await document.GetSyntaxRootAsync(cancellationToken);
            if (syntaxNode == null)
            {
                return solution;
            }

            var newNode = RemoveRenameAnnotations(syntaxNode);
            return solution.WithDocumentSyntaxRoot(documentId, newNode);
        }

        protected SyntaxNode RemoveRenameAnnotations(SyntaxNode syntaxNode)
        {
            var rewriter = new CSharpRemoveRenameAnnotationsRewriter();
            return rewriter.Visit(syntaxNode);
        }

        /// <summary>
        /// This rewriter exists to work around DevDiv 1086632 in Roslyn.  The Rename action is
        /// leaving a set of annotations in the tree.  These annotations slow down further processing
        /// and eventually make the rename operation unusable.  As a temporary work around we manually
        /// remove these from the tree.
        /// </summary>
        private sealed class CSharpRemoveRenameAnnotationsRewriter : CSharpSyntaxRewriter
        {
            public override SyntaxNode Visit(SyntaxNode node)
            {
                node = base.Visit(node);
                if (node != null && node.ContainsAnnotations && node.GetAnnotations(s_renameAnnotationName).Any())
                {
                    node = node.WithoutAnnotations(s_renameAnnotationName);
                }

                return node;
            }

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                token = base.VisitToken(token);
                if (token.ContainsAnnotations && token.GetAnnotations(s_renameAnnotationName).Any())
                {
                    token = token.WithoutAnnotations(s_renameAnnotationName);
                }

                return token;
            }
        }
    }
}
