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
        [InlineData("hello\\nworld", "'hello\\nworld'")]
        [InlineData("hello\nworld", "\"hello\\nworld\"")]
        public void TestBasicString(string expected, string input)
        {
            Assert.Equal(expected, parse(input));
        }

        [Theory]
        // \'
        [InlineData("\\'", "\"\\'\"")]
        [InlineData("'", "'\\''")]
        // \\
        [InlineData("\\", "\"\\\\\"")]
        [InlineData("\\", "'\\\\'")]
        // \n
        [InlineData("\n", "\"\\n\"")]
        [InlineData("\\n", "'\\n'")]
        // \r
        [InlineData("\r", "\"\\r\"")]
        [InlineData("\\r", "'\\r'")]
        // \t
        [InlineData("\t", "\"\\t\"")]
        [InlineData("\\t", "'\\t'")]
        // \v
        [InlineData("\v", "\"\\v\"")]
        [InlineData("\\v", "'\\v'")]
        // \e
        [InlineData("\x1B", "\"\\e\"")]
        [InlineData("\\e", "'\\e'")]
        // \f
        [InlineData("\f", "\"\\f\"")]
        [InlineData("\\f", "'\\f'")]
        // \$
        [InlineData("$", "\"\\$\"")]
        [InlineData("\\$", "'\\$'")]
        // \"
        [InlineData("\"", "\"\\\"\"")]
        [InlineData("\\\"", "'\\\"'")]
        // \[0-7]{1,3}
        [InlineData("A", "\"\\101\"")]
        [InlineData("\\101", "'\\101'")]
        [InlineData("AB", "\"\\101\\102\"")]
        [InlineData("\\101\\102", "'\\101\\102'")]
        [InlineData("\0", "\"\\400\"")]
        [InlineData("\\400", "'\\400'")]
        [InlineData("\\800", "\"\\800\"")]
        [InlineData("\\800", "'\\800'")]
        // \x[0-9A-Fa-f]{1,2}
        [InlineData("A", "\"\\x41\"")]
        [InlineData("\\x41", "'\\x41'")]
        [InlineData("AB", "\"\\x41\\x42\"")]
        [InlineData("\\x41\\x42", "'\\x41\\x42'")]
        // \u{[0-9A-Fa-f]+}
        [InlineData("A", "\"\\u{41}\"")]
        [InlineData("\\u{41}", "'\\u{41}'")]
        [InlineData("AB", "\"\\u{41}\\u{42}\"")]
        [InlineData("\\u{41}\\u{42}", "'\\u{41}\\u{42}'")]
        // Invalid escape sequence
        [InlineData("\\g", "\"\\g\"")]
        [InlineData("\\g", "'\\g'")]
        // Other escaped strings
        [InlineData(":username's data", "':username\\'s data'")]
        [InlineData(":username\\'s data", "\":username\\'s data\"")]
        [InlineData("\"escaped\" quotes", "\"\\\"escaped\\\" quotes\"")]
        public void TestEscapedString(string expected, string input)
        {
            Assert.Equal(expected, parse(input));
        }

        private string parse(string input) => PhpStringLiteralSyntaxNode.Parse(new PhpTokeniser(input)).Text;
    }
}
