// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Composition;
using LocalisationAnalyser.Abstractions.IO;
using LocalisationAnalyser.Abstractions.IO.Default;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace LocalisationAnalyser.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ClassSuffixedLocalisationCodeFixProvider)), Shared]
    public class ClassSuffixedLocalisationCodeFixProvider : AbstractLocalisationCodeFixProvider
    {
        private const string class_suffix = "Strings";

        public ClassSuffixedLocalisationCodeFixProvider()
            : this(new DefaultFileSystem())
        {
        }

        public ClassSuffixedLocalisationCodeFixProvider(IFileSystem fileSystem)
            : base(fileSystem)
        {
        }

        protected override string GetClassName(string defaultName) => $"{base.GetClassName(defaultName)}{class_suffix}";
    }
}
