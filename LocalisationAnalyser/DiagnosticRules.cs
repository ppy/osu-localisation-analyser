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

#pragma warning restore RS2008
    }
}
