// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using LocalisationAnalyser.Tools.Php;
using Xunit;

namespace LocalisationAnalyser.Tools.Tests
{
    public class PhpArrayElementSyntaxNodeTest
    {
        [Theory]
        [InlineData("'Key' => 'Value'", "Key", "Value")]
        [InlineData("'Key' => 'Value',", "Key", "Value")]
        public void TestBasicElement(string input, string expectedKey, string expectedValue)
        {
            var syntaxNode = PhpArrayElementSyntaxNode.Parse(new PhpTokeniser(input));

            Assert.Equal(expectedKey, syntaxNode.Key.Text);
            Assert.IsType<PhpStringLiteralSyntaxNode>(syntaxNode.Value);
            Assert.Equal(expectedValue, ((PhpStringLiteralSyntaxNode)syntaxNode.Value).Text);
        }

        [Fact]
        public void TestArrayElement()
        {
            var syntaxNode = PhpArrayElementSyntaxNode.Parse(new PhpTokeniser(@"
'Key' => [
    'Key1' => 'Value1'
]"));

            Assert.Equal("Key", syntaxNode.Key.Text);
            Assert.IsType<PhpArraySyntaxNode>(syntaxNode.Value);

            var nestedArray = (PhpArraySyntaxNode)syntaxNode.Value;
            Assert.Single(nestedArray.Elements);
            Assert.Equal("Key1", nestedArray.Elements[0].Key.Text);
            Assert.Equal("Value1", ((PhpStringLiteralSyntaxNode)nestedArray.Elements[0].Value).Text);
        }

        [Theory]
        [InlineData("1 => 'Value'")]
        [InlineData("'Key' => 1")]
        [InlineData("'Key' : 'Value'")]
        [InlineData("'Key' 'Value'")]
        [InlineData("'Key'")]
        public void TestInvalidElement(string input)
        {
            Assert.ThrowsAny<Exception>(() => PhpArrayElementSyntaxNode.Parse(new PhpTokeniser(input)));
        }
    }
}
