// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using LocalisationAnalyser.CodeFixes;

namespace LocalisationAnalyser.Tests.CodeFixes
{
    public class MockLocalisationCodeFixProvider : LocalisationCodeFixProvider
    {
        protected override IFileSystem GetFileSystem() => new MockFileSystem();
    }
}
