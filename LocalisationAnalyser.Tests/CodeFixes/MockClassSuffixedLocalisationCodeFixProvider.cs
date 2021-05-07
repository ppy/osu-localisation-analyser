// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using LocalisationAnalyser.CodeFixes;
using LocalisationAnalyser.Tests.Helpers.IO;

namespace LocalisationAnalyser.Tests.CodeFixes
{
    public class MockClassSuffixedLocalisationCodeFixProvider : ClassSuffixedLocalisationCodeFixProvider
    {
        public MockClassSuffixedLocalisationCodeFixProvider()
            : base(new MockFileSystem())
        {
        }
    }
}
