using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace ConsolidateConditionalDirective
{
    class ConditionalDirectiveCollector : CSharpSyntaxWalker
    {
        public readonly List<(CSharpSyntaxNode SyntaxNode, int Level)> ConditionalDirectives = 
                        new List<(CSharpSyntaxNode, int)>();

        private CSharpSyntaxNode _startingIfDirective;

        private int _ifDirectiveBoxLevel;
        private bool _searchEnded;
 
        // Important to call the base constructor, so we can set the Depth, so the SyntaxWalker will at all visit Trivia items.
        public ConditionalDirectiveCollector() : base(Microsoft.CodeAnalysis.SyntaxWalkerDepth.StructuredTrivia)
        {
        }

        public void Visit(CSharpSyntaxNode nodeToTraverse, CSharpSyntaxNode startingIfDirective)
        {
            _startingIfDirective = startingIfDirective;
            Visit(nodeToTraverse);
        }

        public override void VisitIfDirectiveTrivia(IfDirectiveTriviaSyntax node)
        {
            if (_searchEnded)
            {
                return;
            }

            if (_ifDirectiveBoxLevel == 0 && node == _startingIfDirective)
            {
                _ifDirectiveBoxLevel = 1;
                ConditionalDirectives.Add((node, _ifDirectiveBoxLevel));
                return;
            }

            if (_ifDirectiveBoxLevel == 0)
            {
                return;
            }

            _ifDirectiveBoxLevel += 1;
            ConditionalDirectives.Add((node, _ifDirectiveBoxLevel));
            
        }

        public override void VisitElseDirectiveTrivia(ElseDirectiveTriviaSyntax node)
        {
            if (_ifDirectiveBoxLevel == 0 || _searchEnded)
            {
                return;
            }

            ConditionalDirectives.Add((node, _ifDirectiveBoxLevel));
        }

        public override void VisitEndIfDirectiveTrivia(EndIfDirectiveTriviaSyntax node)
        {
            if (_ifDirectiveBoxLevel == 0 || _searchEnded)
            {
                return;
            }

            _ifDirectiveBoxLevel -= 1;
            ConditionalDirectives.Add((node, _ifDirectiveBoxLevel));

            if (_ifDirectiveBoxLevel==0)
            {
                _searchEnded = true;
            }
        }
    }
}
