// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using LocalisationAnalyser.Tools.Php;
using Xunit;

namespace LocalisationAnalyser.Tools.Tests
{
    public class PhpTokeniserTest
    {
        [Fact]
        public void TestSkipSpaces()
        {
            var tokeniser = new PhpTokeniser("       $");
            tokeniser.SkipWhitespace();

            Assert.Equal('$', tokeniser.GetTrivia());
        }

        [Fact]
        public void TestSkipNewLine()
        {
            var tokeniser = new PhpTokeniser("\n\n\n \n   $");
            tokeniser.SkipWhitespace();

            Assert.Equal('$', tokeniser.GetTrivia());
        }

        [Fact]
        public void TestSkipLineComment()
        {
            var tokeniser = new PhpTokeniser(@"
//
// line 1
// line 2
// // //

$");

            tokeniser.SkipWhitespace();

            Assert.Equal('$', tokeniser.GetTrivia());
        }

        [Fact]
        public void TestSkipBlockComment()
        {
            var tokeniser = new PhpTokeniser(@"
/**
 * This is a
 * BLOCK COMMENT!!
 */
/**/
$");

            tokeniser.SkipWhitespace();

            Assert.Equal('$', tokeniser.GetTrivia());
        }
    }
}
