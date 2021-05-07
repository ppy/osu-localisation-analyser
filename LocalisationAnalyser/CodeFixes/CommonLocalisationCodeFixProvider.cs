// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Composition;
using LocalisationAnalyser.Abstractions.IO;
using LocalisationAnalyser.Abstractions.IO.Default;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace LocalisationAnalyser.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CommonLocalisationCodeFixProvider)), Shared]
    public class CommonLocalisationCodeFixProvider : AbstractLocalisationCodeFixProvider
    {
        private const string common_class_name = "Common";

        public CommonLocalisationCodeFixProvider()
            : this(new DefaultFileSystem())
        {
        }

        public CommonLocalisationCodeFixProvider(IFileSystem fileSystem)
            : base(fileSystem, " (common)")
        {
        }

        protected override string GetClassName(string defaultName) => common_class_name;
    }
}
