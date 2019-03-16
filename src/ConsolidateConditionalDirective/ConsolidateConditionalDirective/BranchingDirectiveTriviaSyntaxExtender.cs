using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsolidateConditionalDirective
{
    public static class BranchingDirectiveTriviaSyntaxExtender
    {
        public static Location GetLocationIncludingEol(this DirectiveTriviaSyntax triviaDirective)
        {
            Location locationToReturn = triviaDirective.GetLocation();

            //triviaDirective.ChildNodes().Where();
            if (triviaDirective.HasTrailingTrivia)
            {
                var trailTrivia = triviaDirective.GetTrailingTrivia().LastOrDefault();
                if (trailTrivia!=null &&
                    trailTrivia.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.EndOfLineTrivia))
                {
                    locationToReturn = Location.Create(triviaDirective.SyntaxTree,
                        new TextSpan(triviaDirective.SpanStart, trailTrivia.Span.End - triviaDirective.SpanStart));
                }
            }

            return locationToReturn;
        }
    }
}
