// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LocalisationAnalyser.Tools.Php
{
    /// <summary>
    /// A PHP syntax node for a string literal.
    /// </summary>
    public class PhpStringLiteralSyntaxNode : PhpLiteralSyntaxNode
    {
        private static readonly Regex oct_pattern = new Regex("^([0-7]{1,3})", RegexOptions.Compiled);
        private static readonly Regex hex_pattern = new Regex("^(x[0-9A-Fa-f]{1,2})", RegexOptions.Compiled);
        private static readonly Regex uni_pattern = new Regex("^(u{[0-9A-Fa-f]+})", RegexOptions.Compiled);

        public readonly string Text;

        public PhpStringLiteralSyntaxNode(string text)
        {
            Text = text;
        }

        public static PhpStringLiteralSyntaxNode Parse(PhpTokeniser tokeniser)
        {
            tokeniser.SkipWhitespace();

            char leader = tokeniser.GetTrivia();
            tokeniser.Advance();

            var stringBuilder = new StringBuilder();

            while (tokeniser.GetTrivia() != leader)
            {
                char token = tokeniser.GetTrivia();
                tokeniser.Advance();

                if (token == '\\')
                {
                    stringBuilder.Append(processEscapeSequence(leader, tokeniser));
                    continue;
                }

                stringBuilder.Append(token);
            }

            // Skip trailing trivia.
            tokeniser.Advance();
            tokeniser.SkipWhitespace();

            return new PhpStringLiteralSyntaxNode(stringBuilder.ToString());
        }

        private static string processEscapeSequence(char leader, PhpTokeniser tokeniser)
        {
            char trivia = tokeniser.GetTrivia();

            // Base cases for \{leader} and \\, supported by both single- and double-quoted strings.
            if (trivia == leader || trivia == '\\')
            {
                tokeniser.Advance();
                return trivia.ToString();
            }

            // No other escape sequences are supported for single-quoted strings.
            if (leader == '\'')
                return @"\";

            // Double-quoted strings have a few more cases...
            switch (trivia)
            {
                case 'n':
                    tokeniser.Advance();
                    return "\n";

                case 'r':
                    tokeniser.Advance();
                    return "\r";

                case 't':
                    tokeniser.Advance();
                    return "\t";

                case 'v':
                    tokeniser.Advance();
                    return "\v";

                case 'e':
                    tokeniser.Advance();
                    return "\x1B";

                case 'f':
                    tokeniser.Advance();
                    return "\f";

                case '$':
                    tokeniser.Advance();
                    return "$";

                case >= '0' and <= '7':
                {
                    Match match = oct_pattern.Match($"{trivia}{tokeniser.PeekNext(2)}");

                    if (match.Success)
                    {
                        tokeniser.Advance(match.Length);

                        unchecked
                        {
                            byte octValue = (byte)Convert.ToInt32(match.Value, 8);
                            return ((char)octValue).ToString();
                        }
                    }

                    break;
                }

                case 'x':
                {
                    Match match = hex_pattern.Match($"{trivia}{tokeniser.PeekNext(2)}");

                    if (match.Success)
                    {
                        tokeniser.Advance(match.Length);
                        return ((char)byte.Parse(match.Value[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToString();
                    }

                    break;
                }

                case 'u':
                {
                    Match match = uni_pattern.Match($"{trivia}{tokeniser.PeekNext(16)}");

                    if (match.Success)
                    {
                        tokeniser.Advance(match.Length);
                        return char.ConvertFromUtf32(Convert.ToInt32(match.Value[2..^1], 16));
                    }

                    break;
                }
            }

            return @"\";
        }
    }
}
