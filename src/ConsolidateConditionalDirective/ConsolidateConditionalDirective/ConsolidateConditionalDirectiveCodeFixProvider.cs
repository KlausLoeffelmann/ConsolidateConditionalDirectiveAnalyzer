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
    public class ConsolidateConditionalDirectiveCodeFixProvider : CodeFixProvider
    {
        private const string title = "Consolidate to active branch.";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(ConsolidateConditionalDirectiveAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var ifSpan = diagnostic.Location.SourceSpan;

            TextSpan? elseSpan;
            TextSpan endifSpan;

            if (diagnostic.AdditionalLocations.Count==1)
            {
                elseSpan = null;
                endifSpan = diagnostic.AdditionalLocations[0].SourceSpan;
            }
            else
            {
                elseSpan = diagnostic.AdditionalLocations[0].SourceSpan;
                endifSpan = diagnostic.AdditionalLocations[1].SourceSpan;
            }

            var isInvers = bool.Parse(diagnostic.Properties["isInvers"]);

            context.RegisterCodeFix(
                CodeAction.Create(title, c => ConsolidateToActiveConditionalIfBranchAsync(context.Document,
                                                                                          ifSpan,
                                                                                          elseSpan,
                                                                                          endifSpan, 
                                                                                          isInvers, c),
                                  equivalenceKey: title),
                diagnostic);
        }

        private async Task<Document> ConsolidateToActiveConditionalIfBranchAsync(Document document,TextSpan ifSpan, TextSpan? elseSpan, 
                                                                                 TextSpan endifSpan, bool isInvers,
                                                                                 CancellationToken cancellationToken)
        {
            if (!isInvers)
            {
                var sourceText = await document.GetTextAsync(cancellationToken);
                sourceText = sourceText.Replace(ifSpan, String.Empty);

                if (elseSpan.HasValue)
                {
                    sourceText = sourceText.Replace(
                        new TextSpan(elseSpan.Value.Start-ifSpan.Length, endifSpan.End - elseSpan.Value.Start), 
                        String.Empty);
                }
                else
                {
                    sourceText = sourceText.Replace(new TextSpan(endifSpan.Start - ifSpan.Length, endifSpan.Length), String.Empty);
                }

                document = document.WithText(sourceText);
                return document;
            }
            else
            {
                var sourceText = await document.GetTextAsync(cancellationToken);

                if (elseSpan.HasValue)
                {
                    var blockSpanToDelete = elseSpan.Value.End - ifSpan.Start;
                    sourceText = sourceText.Replace(new TextSpan(ifSpan.Start, blockSpanToDelete), String.Empty);
                    sourceText = sourceText.Replace(new TextSpan(endifSpan.Start - blockSpanToDelete,
                                                                 endifSpan.End - endifSpan.Start), String.Empty);
                }
                else
                {
                    sourceText = sourceText.Replace(new TextSpan(ifSpan.Start , endifSpan.End-ifSpan.Start), String.Empty);
                }

                document = document.WithText(sourceText);
                return document;
            }
        }
    }
}
