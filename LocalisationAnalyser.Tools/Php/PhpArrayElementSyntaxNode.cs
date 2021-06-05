// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace LocalisationAnalyser.Tools.Php
{
    /// <summary>
    /// A PHP syntax node for an element of a <see cref="PhpArraySyntaxNode"/>.
    /// </summary>
    public class PhpArrayElementSyntaxNode : PhpSyntaxNode
    {
        public readonly PhpStringLiteralSyntaxNode Key;
        public readonly PhpSyntaxNode Value;

        public PhpArrayElementSyntaxNode(PhpStringLiteralSyntaxNode key, PhpSyntaxNode value)
        {
            Key = key;
            Value = value;
        }

        public static PhpArrayElementSyntaxNode Parse(PhpTokeniser tokeniser)
        {
            tokeniser.SkipWhitespace();

            PhpStringLiteralSyntaxNode key = tokeniser.GetTrivia() switch
            {
                '\'' => PhpStringLiteralSyntaxNode.Parse(tokeniser),
                '"' => PhpStringLiteralSyntaxNode.Parse(tokeniser),
                _ => throw tokeniser.ConstructError($"Invalid array value identifier ({tokeniser.GetTrivia()}).")
            };

            tokeniser.SkipWhitespace();
            tokeniser.SkipPattern(new[] { '=', '>' });
            tokeniser.SkipWhitespace();

            PhpSyntaxNode value = tokeniser.GetTrivia() switch
            {
                '\'' => PhpStringLiteralSyntaxNode.Parse(tokeniser),
                '"' => PhpStringLiteralSyntaxNode.Parse(tokeniser),
                '[' => PhpArraySyntaxNode.Parse(tokeniser),
                _ => throw tokeniser.ConstructError($"Invalid array value identifier ({tokeniser.GetTrivia()}).")
            };

            tokeniser.SkipWhitespace();

            // Skip trailing trivia for this element.
            if (tokeniser.TryGetTrivia(out var trivia) && trivia == ',')
                tokeniser.Advance();
            tokeniser.SkipWhitespace();

            return new PhpArrayElementSyntaxNode(key, value);
        }
    }
}
