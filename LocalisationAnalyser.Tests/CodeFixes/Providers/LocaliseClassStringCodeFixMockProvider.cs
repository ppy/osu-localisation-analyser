// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using LocalisationAnalyser.CodeFixes;
using LocalisationAnalyser.Tests.Helpers.IO;

namespace LocalisationAnalyser.Tests.CodeFixes.Providers
{
    internal class LocaliseClassStringCodeFixMockProvider : LocaliseClassStringCodeFixProvider
    {
        public LocaliseClassStringCodeFixMockProvider()
            : base(new MockFileSystem())
        {
        }
    }
}
