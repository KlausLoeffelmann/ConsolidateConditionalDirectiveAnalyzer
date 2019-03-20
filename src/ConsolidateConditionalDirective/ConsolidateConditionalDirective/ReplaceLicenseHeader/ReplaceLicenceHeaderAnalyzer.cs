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
    public class ReplaceLicenseHeaderAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ReplaceLicenseHeader";
        public static readonly string LicenseHeader = @"// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.";

        private static readonly LocalizableString Title = "License Header Warning.";
        private static readonly LocalizableString MessageFormat = "Wrong or missing License header.";
        private static readonly LocalizableString Description = "Replace License Header.";
        private const string Category = "Refactoring";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, 
            Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            // NOTE: Registring for Nodes (SingleLine, MultiLine) would not work, so we ned to do this.
            context.RegisterSyntaxTreeAction(Analyze);
        }

        private static void Analyze(SyntaxTreeAnalysisContext context)
        {
            SyntaxNode root = context.Tree.GetCompilationUnitRoot(context.CancellationToken);

            Diagnostic diagnostic;
            var props = ImmutableDictionary.Create<string, string>();
            var firstToken = root.GetFirstToken();

            if (!firstToken.HasLeadingTrivia)
            {
                // We need to report, that there is NO trivia,
                // so the licence file on top of this code file is missing.
                props = props.Add("hasNoHeader", true.ToString());
                diagnostic = Diagnostic.Create(Rule, firstToken.GetLocation(),
                    props);

                context.ReportDiagnostic(diagnostic);

            }
            else
            {
                var leadingTrivia = root.GetFirstToken().LeadingTrivia;

                if (leadingTrivia.ToString() != LicenseHeader)
                {
                    var leadingTriviaLocation = Location.Create(context.Tree,
                        leadingTrivia.FullSpan);

                    props = props.Add("hasNoHeader", false.ToString());
                    diagnostic = Diagnostic.Create(Rule, leadingTriviaLocation,
                        props);

                    context.ReportDiagnostic(diagnostic);

                }
            }
        }
    }
}
