using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace ConsolidateConditionalDirective
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConsolidateConditionalDirectiveCodeFixProvider)), Shared]
    public class ReplaceLicenseHeaderCodeFixProvider : CodeFixProvider
    {
        private const string title = "Replace with correct License text.";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(ReplaceLicenseHeaderAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var existingHeaderSpan = diagnostic.Location.SourceSpan;


            var hasNoHeader = bool.Parse(diagnostic.Properties["hasNoHeader"]);
            string preserveTrivia = string.Empty;
            diagnostic.Properties.TryGetValue(nameof(preserveTrivia), out preserveTrivia);

            context.RegisterCodeFix(
                CodeAction.Create(title, c => EnsureLicenseHeaderAsLeadingTrivia(context.Document, existingHeaderSpan, preserveTrivia, hasNoHeader, c),
                                  equivalenceKey: title),
                                  diagnostic);
        }

        private async Task<Solution> EnsureLicenseHeaderAsLeadingTrivia(Document document, 
            TextSpan existingHeaderSpan, string preserveTrigger, bool hasNoHeader, CancellationToken cancellationToken)
        {
            var sourceText = await document.GetTextAsync(cancellationToken);
            Solution originalSolution = document.Project.Solution;
            Solution newSolution;

            if (hasNoHeader)
            {
                TextChange tChange = new TextChange(TextSpan.FromBounds(0, 0), ReplaceLicenseHeaderAnalyzer.LicenseHeader);
                sourceText = sourceText.WithChanges(tChange);
                newSolution = originalSolution.WithDocumentText(document.Id, sourceText);
            }
            else
            {
                sourceText = sourceText.Replace(TextSpan.FromBounds(0, existingHeaderSpan.End), ReplaceLicenseHeaderAnalyzer.LicenseHeader + preserveTrigger);
                newSolution = originalSolution.WithDocumentText(document.Id, sourceText);
            }

            return newSolution;
        }
    }
}
