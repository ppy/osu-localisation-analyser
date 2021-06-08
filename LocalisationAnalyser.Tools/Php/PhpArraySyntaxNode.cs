// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Immutable;

namespace LocalisationAnalyser.Tools.Php
{
    /// <summary>
    /// A PHP syntax node for an array.
    /// </summary>
    public class PhpArraySyntaxNode : PhpSyntaxNode
    {
        public readonly ImmutableArray<PhpArrayElementSyntaxNode> Elements;

        public PhpArraySyntaxNode(ImmutableArray<PhpArrayElementSyntaxNode> elements)
        {
            Elements = elements;
        }

        public static PhpArraySyntaxNode Parse(PhpTokeniser tokeniser)
        {
            tokeniser.SkipWhitespace();
            tokeniser.SkipPattern(new[] { '[' });

            var elements = ImmutableArray.CreateBuilder<PhpArrayElementSyntaxNode>();

            while (true)
            {
                tokeniser.SkipWhitespace();
                if (tokeniser.GetTrivia() == ']')
                    break;

                elements.Add(PhpArrayElementSyntaxNode.Parse(tokeniser));
            }

            // Skip trailing trivia.
            tokeniser.Advance();
            tokeniser.SkipWhitespace();

            return new PhpArraySyntaxNode(elements.ToImmutableArray());
        }
    }
}
