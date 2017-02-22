// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Rename;

namespace Microsoft.DotNet.CodeFormatting.Rules
{
    [GlobalSemanticRule(PrivateFieldNamingRule.Name, PrivateFieldNamingRule.Description, GlobalSemanticRuleOrder.PrivateFieldNamingRule)]
    internal partial class PrivateFieldNamingRule : IGlobalSemanticFormattingRule
    {
        internal const string Name = "FieldNames";
        internal const string Description = "Postfix private fields with _";

        #region CommonRule

        private abstract class CommonRule
        {
            protected abstract SyntaxNode AddPrivateFieldAnnotations(SyntaxNode syntaxNode, out int count);

            /// <summary>
            /// This method exists to work around DevDiv 1086632 in Roslyn.  The Rename action is
            /// leaving a set of annotations in the tree.  These annotations slow down further processing
            /// and eventually make the rename operation unusable.  As a temporary work around we manually
            /// remove these from the tree.
            /// </summary>
            protected abstract SyntaxNode RemoveRenameAnnotations(SyntaxNode syntaxNode);

            public async Task<Solution> ProcessAsync(Document document, SyntaxNode syntaxRoot, CancellationToken cancellationToken)
            {
                int count;
                var newSyntaxRoot = AddPrivateFieldAnnotations(syntaxRoot, out count);

                if (count == 0)
                {
                    return document.Project.Solution;
                }

                var documentId = document.Id;
                var solution = document.Project.Solution;
                solution = solution.WithDocumentSyntaxRoot(documentId, newSyntaxRoot);
                solution = await RenameFields(solution, documentId, count, cancellationToken);
                return solution;
            }

            private async Task<Solution> RenameFields(Solution solution, DocumentId documentId, int count, CancellationToken cancellationToken)
            {
                Solution oldSolution = null;
                for (int i = 0; i < count; i++)
                {
                    oldSolution = solution;

                    var semanticModel = await solution.GetDocument(documentId).GetSemanticModelAsync(cancellationToken);
                    var root = await semanticModel.SyntaxTree.GetRootAsync(cancellationToken);
                    var declaration = root.GetAnnotatedNodes(s_markerAnnotation).ElementAt(i);

                    bool isConstant = declaration.HasAnnotation(s_constantAnnotation);

                    // Make note, VB represents "fields" marked as "WithEvents" as properties, so don't be
                    // tempted to treat this as a IFieldSymbol. We only need the name, so ISymbol is enough.
                    var fieldSymbol = semanticModel.GetDeclaredSymbol(declaration, cancellationToken);
                    var newName = GetNewFieldName(fieldSymbol, isConstant);

                    // Can happen with pathologically bad field names like _
                    if (newName == fieldSymbol.Name)
                    {
                        continue;
                    }

                    solution = await Renamer.RenameSymbolAsync(solution, fieldSymbol, newName, solution.Workspace.Options, cancellationToken).ConfigureAwait(false);
                    solution = await CleanSolutionAsync(solution, oldSolution, cancellationToken);
                }

                return solution;
            }

            private static string GetNewFieldName(ISymbol fieldSymbol, bool isConstant)
            {
                string camelCaseName = NamingUtil.ConvertToCamelCase(fieldSymbol.Name);
                if (isConstant)
                {
                    return "k" + camelCaseName.Captialized();
                }
                else
                {
                    return camelCaseName + "_";
                }
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
        }

        #endregion

        private const string s_renameAnnotationName = "Rename";

        private readonly static SyntaxAnnotation s_markerAnnotation = new SyntaxAnnotation("PrivateFieldToRename");
        private readonly static SyntaxAnnotation s_constantAnnotation = new SyntaxAnnotation("ConstantField");

        // Used to avoid the array allocation on calls to WithAdditionalAnnotations
        private readonly static SyntaxAnnotation[] s_markerAnnotationArray;

        static PrivateFieldNamingRule()
        {
            s_markerAnnotationArray = new[] { s_markerAnnotation };
        }

        private readonly CSharpRule _csharpRule = new CSharpRule();
        private readonly VisualBasicRule _visualBasicRule = new VisualBasicRule();

        public bool SupportsLanguage(string languageName)
        {
            return
                languageName == LanguageNames.CSharp ||
                languageName == LanguageNames.VisualBasic;
        }

        public Task<Solution> ProcessAsync(Document document, SyntaxNode syntaxRoot, CancellationToken cancellationToken)
        {
            switch (document.Project.Language)
            {
                case LanguageNames.CSharp:
                    return _csharpRule.ProcessAsync(document, syntaxRoot, cancellationToken);
                case LanguageNames.VisualBasic:
                    return _visualBasicRule.ProcessAsync(document, syntaxRoot, cancellationToken);
                default:
                    throw new NotSupportedException();
            }
        }

        private static bool IsGoodPrivateFieldName(string name, bool isConstant)
        {
            if (name.Length <= 0)
            {
                return false;
            }

            string trimmedName = name.Trim('_');
            if (trimmedName.Contains("_"))
            {
                return false;
            }

            if (isConstant)
            {
                bool startsWithK = name[0] == 'k';
                if (!startsWithK)
                {
                    return false;
                }
            } else {
                bool endsWithUnderscore = name[name.Length - 1] == '_';
                if (!endsWithUnderscore)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
