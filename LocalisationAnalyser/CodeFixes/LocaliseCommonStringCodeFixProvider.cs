// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Composition;
using LocalisationAnalyser.Abstractions.IO;
using LocalisationAnalyser.Abstractions.IO.Default;
using LocalisationAnalyser.Localisation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace LocalisationAnalyser.CodeFixes
{
    /// <summary>
    /// Code-fix provider for <see cref="DiagnosticRules.STRING_CAN_BE_LOCALISED"/> inspections to replace strings with a localisation from the common localisation class.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LocaliseCommonStringCodeFixProvider)), Shared]
    [ExtensionOrder(After = nameof(LocaliseClassStringCodeFixProvider))]
    internal class LocaliseCommonStringCodeFixProvider : AbstractLocaliseStringCodeFixProvider
    {
        public LocaliseCommonStringCodeFixProvider()
            : this(new DefaultFileSystem())
        {
        }

        public LocaliseCommonStringCodeFixProvider(IFileSystem fileSystem)
            : base(fileSystem, "common")
        {
        }

        protected override string GetLocalisationPrefix(string @namespace, string className) => base.GetLocalisationPrefix(@namespace, SyntaxTemplates.COMMON_STRINGS_CLASS_NAME);

        protected override string GetLocalisationFileName(string className) => $"{SyntaxTemplates.COMMON_STRINGS_CLASS_NAME}{SyntaxTemplates.STRINGS_FILE_SUFFIX}";
    }
}
