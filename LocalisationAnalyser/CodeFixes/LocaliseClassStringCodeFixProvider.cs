// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Composition;
using LocalisationAnalyser.Abstractions.IO;
using LocalisationAnalyser.Abstractions.IO.Default;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace LocalisationAnalyser.CodeFixes
{
    /// <summary>
    /// Code-fix provider for <see cref="DiagnosticRules.STRING_CAN_BE_LOCALISED"/> inspections to replace strings with a localisation specific to the containing class.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LocaliseClassStringCodeFixProvider)), Shared]
    internal class LocaliseClassStringCodeFixProvider : AbstractLocaliseStringCodeFixProvider
    {
        private const string class_suffix = "Strings";

        public LocaliseClassStringCodeFixProvider()
            : this(new DefaultFileSystem())
        {
        }

        public LocaliseClassStringCodeFixProvider(IFileSystem fileSystem)
            : base(fileSystem, "class")
        {
        }

        protected override string GetLocalisationFileName(string className) => $"{base.GetLocalisationFileName(className)}{class_suffix}";
    }
}
