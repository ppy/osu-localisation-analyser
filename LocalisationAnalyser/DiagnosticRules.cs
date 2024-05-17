// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.CodeAnalysis;

namespace LocalisationAnalyser
{
    public static class DiagnosticRules
    {
        // Disable's roslyn analyser release tracking. Todo: Temporary? The analyser doesn't behave well with Rider :/
        // Read more: https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md
#pragma warning disable RS2008

        public static readonly DiagnosticDescriptor STRING_CAN_BE_LOCALISED = new DiagnosticDescriptor(
            "OLOC001",
            "String can be localised",
            "'{0}' can be localised",
            "Globalization",
            DiagnosticSeverity.Info,
            true,
            "This string can be localised by using TranslatableString.");

        public static readonly DiagnosticDescriptor XMLDOC_DOES_NOT_MATCH_TEXT = new DiagnosticDescriptor(
            "OLOC002",
            "XMLDoc does not match the translation text",
            "XMLDoc does not match the translation text",
            "Globalization",
            DiagnosticSeverity.Warning,
            true,
            "Prevent confusion by matching the XMLDoc and the translation text.");

        public static readonly DiagnosticDescriptor TEXT_DOES_NOT_MATCH_XMLDOC = new DiagnosticDescriptor(
            "OLOC003",
            "Translation text does not match the XMLDoc",
            "Translation text does not match the XMLDoc",
            "Globalization",
            DiagnosticSeverity.Warning,
            true,
            "Prevent confusion by matching the XMLDoc and the translation text.");

        public static readonly DiagnosticDescriptor LOCALISATION_KEY_USED_MULTIPLE_TIMES_IN_CLASS = new DiagnosticDescriptor(
            "OLOC004",
            "Localisation key used multiple times in class",
            "The localisation key '{0}' has been used multiple times in this class",
            "Globalization",
            DiagnosticSeverity.Warning,
            true,
            "Use unique localisation keys for every member in a single class containing localisations.");

        public static readonly DiagnosticDescriptor RESOLVED_ATTRIBUTE_NULLABILITY_IS_REDUNDANT = new DiagnosticDescriptor(
            "OSUF001",
            "Nullability should be provided by the type",
            "Nullability should be provided by the type",
            "Design",
            DiagnosticSeverity.Warning,
            true);

#pragma warning restore RS2008
    }
}
