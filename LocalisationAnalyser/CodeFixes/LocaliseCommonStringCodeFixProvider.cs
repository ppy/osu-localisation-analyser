// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Composition;
using LocalisationAnalyser.Abstractions.IO;
using LocalisationAnalyser.Abstractions.IO.Default;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace LocalisationAnalyser.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LocaliseCommonStringCodeFixProvider)), Shared]
    public class LocaliseCommonStringCodeFixProvider : AbstractLocaliseStringCodeFixProvider
    {
        private const string common_class_name = "Common";

        public LocaliseCommonStringCodeFixProvider()
            : this(new DefaultFileSystem())
        {
        }

        public LocaliseCommonStringCodeFixProvider(IFileSystem fileSystem)
            : base(fileSystem, "common")
        {
        }

        protected override string GetLocalisationPrefix(string className) => common_class_name;

        protected override string GetLocalisationFileName(string className) => common_class_name;
    }
}
