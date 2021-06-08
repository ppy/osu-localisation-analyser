// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using LocalisationAnalyser.Tools.Php;
using Xunit;

namespace LocalisationAnalyser.Tools.Tests
{
    public class PhpArraySyntaxNodeTest
    {
        [Fact]
        public void TestEmptyArray()
        {
            var array = PhpArraySyntaxNode.Parse(new PhpTokeniser("[]"));
            Assert.Empty(array.Elements);
        }

        [Fact]
        public void TestSingleElement()
        {
            var array = PhpArraySyntaxNode.Parse(new PhpTokeniser(@"
[
    'Key' => 'Value',
]"));

            Assert.Single(array.Elements);
            Assert.Equal("Key", array.Elements[0].Key.Text);
            Assert.Equal("Value", ((PhpStringLiteralSyntaxNode)array.Elements[0].Value).Text);
        }

        [Fact]
        public void TestMultipleElements()
        {
            var array = PhpArraySyntaxNode.Parse(new PhpTokeniser(@"
[
    'Key1' => 'Value1',
    'Key2' => 'Value2',
]"));

            Assert.Equal(2, array.Elements.Length);
            Assert.Equal("Key1", array.Elements[0].Key.Text);
            Assert.Equal("Value1", ((PhpStringLiteralSyntaxNode)array.Elements[0].Value).Text);
            Assert.Equal("Key2", array.Elements[1].Key.Text);
            Assert.Equal("Value2", ((PhpStringLiteralSyntaxNode)array.Elements[1].Value).Text);
        }

        [Fact]
        public void TestNestedArray()
        {
            var array = PhpArraySyntaxNode.Parse(new PhpTokeniser(@"
[
    'Key1' => 'Value1',
    'Key2' => [
        'Key3' => 'Value3'
    ],
]"));

            Assert.Equal(2, array.Elements.Length);
            Assert.Equal("Key1", array.Elements[0].Key.Text);
            Assert.Equal("Key2", array.Elements[1].Key.Text);

            var nestedArray = (PhpArraySyntaxNode)array.Elements[1].Value;
            Assert.Single(nestedArray.Elements);
            Assert.Equal("Key3", nestedArray.Elements[0].Key.Text);
            Assert.Equal("Value3", ((PhpStringLiteralSyntaxNode)nestedArray.Elements[0].Value).Text);
        }
    }
}
