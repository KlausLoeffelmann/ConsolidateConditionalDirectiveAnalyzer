using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace ConsolidateConditionalDirective
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ConsolidateConditionalDirectiveAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ConsolidateConditionalDirective";
        public const string SYMBOL_NAME = "WINFORMS_CONTROL";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Refactoring";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.IfDirectiveTrivia);
        }

        private static void Analyze(SyntaxNodeAnalysisContext context)
        {
            var ifDirective = (IfDirectiveTriviaSyntax) context.Node;

            // If the compiler symbol is not defined, let's not further bother.
            if (!ifDirective.IsActive)
            {
                return;
            }

            bool isInvers = false;

            // Find the Symbol.
            var collector = new ConditionalDirectiveCollector();
            collector.Visit((CSharpSyntaxNode) context.Node.SyntaxTree.GetRoot(), ifDirective);

            var temp = collector.ConditionalDirectives;

            var condition = ifDirective.Condition as IdentifierNameSyntax;
            if (condition==null)
            {
                var invCondition = ifDirective.Condition as PrefixUnaryExpressionSyntax;
                condition = invCondition.ChildNodes().FirstOrDefault() as IdentifierNameSyntax;
                isInvers = true;
            }


            if (condition?.Identifier.ValueText == SYMBOL_NAME)
            {
                if (collector.ConditionalDirectives.Count > 0)
                {
                    var lastEndifDirective = (EndIfDirectiveTriviaSyntax)temp[temp.Count - 1].SyntaxNode;

                    Location ifLocation = ifDirective.GetLocationIncludingEol();

                    var elseDirective = collector.ConditionalDirectives.Where((item) => typeof(ElseDirectiveTriviaSyntax).GetTypeInfo().
                                                  IsAssignableFrom(item.SyntaxNode.GetType().GetTypeInfo()) && item.Level == 1).
                                                    SingleOrDefault().SyntaxNode as ElseDirectiveTriviaSyntax;
                    var endifDirective = collector.ConditionalDirectives.Where((item) => typeof(EndIfDirectiveTriviaSyntax).GetTypeInfo().
                                                    IsAssignableFrom(item.SyntaxNode.GetType().GetTypeInfo()) && item.Level == 0).
                                                    SingleOrDefault().SyntaxNode as EndIfDirectiveTriviaSyntax;

                    Location elseLocation = elseDirective?.GetLocationIncludingEol();
                    Location endifLocation = endifDirective?.GetLocationIncludingEol();

                    if (!(ifLocation == null || endifLocation == null))
                    {

                        Diagnostic diagnostic;
                        var props = ImmutableDictionary.Create<string, string>();
                        props = props.Add(nameof(isInvers), isInvers.ToString());

                        // Careful! We MUST not pass a null Location, or all hell breaks loose...
                        if (elseLocation == null)
                        {
                            diagnostic = Diagnostic.Create(Rule, ifLocation,
                                additionalLocations: new Location[] { endifLocation },
                                props);
                        }
                        else
                        {
                            diagnostic = Diagnostic.Create(Rule, ifLocation,
                                additionalLocations: new Location[] { elseLocation, endifLocation },
                                props);
                        }

                        context.ReportDiagnostic(diagnostic);

                    }
                }
            }
        }
    }
}
