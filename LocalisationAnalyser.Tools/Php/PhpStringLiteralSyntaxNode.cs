// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Text;

namespace LocalisationAnalyser.Tools.Php
{
    /// <summary>
    /// A PHP syntax node for a string literal.
    /// </summary>
    public class PhpStringLiteralSyntaxNode : PhpLiteralSyntaxNode
    {
        public readonly string Text;

        public PhpStringLiteralSyntaxNode(string text)
        {
            Text = text;
        }

        public static PhpStringLiteralSyntaxNode Parse(PhpTokeniser tokeniser)
        {
            tokeniser.SkipWhitespace();

            char trivia = tokeniser.GetTrivia();

            // Skip leading trivia.
            tokeniser.Advance();

            var stringBuilder = new StringBuilder();
            bool isEscaping = false;

            while (isEscaping || tokeniser.GetTrivia() != trivia)
            {
                var token = tokeniser.GetTrivia();
                tokeniser.Advance();

                if (token == '\\' && !isEscaping)
                {
                    isEscaping = true;
                    continue;
                }

                stringBuilder.Append(token);
                isEscaping = false;
            }

            // Skip trailing trivia.
            tokeniser.Advance();
            tokeniser.SkipWhitespace();

            return new PhpStringLiteralSyntaxNode(stringBuilder.ToString());
        }
    }
}
