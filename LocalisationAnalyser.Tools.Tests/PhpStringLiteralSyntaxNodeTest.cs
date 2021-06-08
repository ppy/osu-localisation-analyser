// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using LocalisationAnalyser.Tools.Php;
using Xunit;

namespace LocalisationAnalyser.Tools.Tests
{
    public class PhpStringLiteralSyntaxNodeTest
    {
        [Theory]
        [InlineData("''")]
        [InlineData("\"\"")]
        public void TestEmptyString(string input)
        {
            Assert.Equal(string.Empty, parse(input));
        }

        [Theory]
        [InlineData("1", "'1'")]
        [InlineData("1", "\"1\"")]
        [InlineData("a", "'a'")]
        [InlineData("a", "\"a\"")]
        [InlineData("hello world", "'hello world'")]
        [InlineData("hello world", "\"hello world\"")]
        [InlineData("😊😊", "'😊😊'")]
        [InlineData("😊😊", "\"😊😊\"")]
        [InlineData("hello\nworld", "'hello\nworld'")]
        [InlineData("hello\nworld", "\"hello\nworld\"")]
        public void TestBasicString(string expected, string input)
        {
            Assert.Equal(expected, parse(input));
        }

        [Theory]
        [InlineData(":username's data", "':username\\'s data'")]
        [InlineData(":username's data", "\":username\\'s data\"")]
        [InlineData("\"escaped\" quotes", "\"\\\"escaped\\\" quotes\"")]
        public void TestEscapedString(string expected, string input)
        {
            Assert.Equal(expected, parse(input));
        }

        private string parse(string input) => PhpStringLiteralSyntaxNode.Parse(new PhpTokeniser(input)).Text;
    }
}
