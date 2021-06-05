// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics.CodeAnalysis;

namespace LocalisationAnalyser.Tools.Php
{
    /// <summary>
    /// Tokenises a PHP file.
    /// </summary>
    public class PhpTokeniser
    {
        private readonly string content;

        private int currentIndex;
        private int currentLine;
        private int currentColumn;

        /// <summary>
        /// Creates a new <see cref="PhpTokeniser"/>.
        /// </summary>
        /// <param name="content">The PHP content.</param>
        public PhpTokeniser(string content)
        {
            this.content = content;
        }

        /// <summary>
        /// Attempts to retrieve the current trivia, returning a value indicating whether the retrieval succeeded.
        /// </summary>
        /// <param name="trivia">The trivia, if any.</param>
        /// <returns>Whether the trivia was retrieved successfully.</returns>
        public bool TryGetTrivia(out char trivia)
        {
            if (currentIndex >= content.Length)
            {
                trivia = default;
                return false;
            }

            trivia = content[currentIndex];
            return true;
        }

        /// <summary>
        /// Retrieves the current trivia.
        /// </summary>
        /// <returns>The trivia.</returns>
        /// <exception cref="InvalidOperationException">Thrown when at the end of the PHP content.</exception>
        public char GetTrivia()
        {
            if (currentIndex >= content.Length)
                ThrowError("EOF");

            return content[currentIndex];
        }

        /// <summary>
        /// Attempts to advance to the next trivia.
        /// </summary>
        public void TryAdvance()
        {
            if (currentIndex >= content.Length)
                return;

            Advance();
        }

        /// <summary>
        /// Advances to the next trivia.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when at the end of the PHP content.</exception>
        public void Advance()
        {
            if (currentIndex == content.Length)
                ThrowError("Failed to advance, reached EOF.");

            if (GetTrivia() == '\n')
            {
                currentLine++;
                currentColumn = 0;
            }
            else
                currentColumn++;

            currentIndex++;
        }

        /// <summary>
        /// Attempts to peek the next trivia, returning a value indicating whether the retrieval succeeded.
        /// </summary>
        /// <param name="trivia">The next trivia, if any.</param>
        /// <returns>Whether the trivia was retrieved successfully.</returns>
        public bool TryPeekNext(out char trivia)
        {
            if (currentIndex + 1 >= content.Length)
            {
                trivia = default;
                return false;
            }

            trivia = content[currentIndex + 1];
            return true;
        }

        /// <summary>
        /// Skips all current whitespace and comments.
        /// </summary>
        public void SkipWhitespace()
        {
            char trivia;

            while (TryGetTrivia(out trivia) && char.IsWhiteSpace(trivia))
                TryAdvance();

            if (TryGetTrivia(out trivia) && trivia != '/')
                return;

            // // comment pattern.
            if (TryPeekNext(out trivia) && trivia == '/')
            {
                while (TryGetTrivia(out trivia) && trivia != '\n')
                    TryAdvance();
                TryAdvance();
                return;
            }

            // /* comment pattern.
            if (TryPeekNext(out trivia) && trivia == '*')
            {
                TryAdvance();
                TryAdvance();

                while (true)
                {
                    if (TryGetTrivia(out trivia) && trivia == '*' && TryPeekNext(out var nextTrivia) && nextTrivia == '/')
                    {
                        TryAdvance();
                        TryAdvance();
                        break;
                    }

                    TryAdvance();
                }
            }
        }

        /// <summary>
        /// Skips a trivia pattern, throwing an exception where the pattern does not match the given input.
        /// </summary>
        /// <param name="pattern">The pattern to skip.</param>
        /// <exception cref="InvalidOperationException">When the current pattern does not match the given input..</exception>
        public void SkipPattern(ReadOnlySpan<char> pattern)
        {
            for (int i = 0; i < pattern.Length; i++)
            {
                if (GetTrivia() != pattern[i])
                    ThrowError($"Invalid token (expected {pattern.ToString()})");

                Advance();
            }
        }

        /// <summary>
        /// Throws a detailed exception containing the given error message.
        /// </summary>
        /// <param name="description">The error description.</param>
        [DoesNotReturn]
        public void ThrowError(string description)
            => throw ConstructError(description);

        /// <summary>
        /// Constructs a detailed exception containing the given error message.
        /// </summary>
        /// <param name="description">The error description.</param>
        public Exception ConstructError(string description)
            => new InvalidOperationException($"{description} at {currentLine}:{currentColumn}.");
    }
}
