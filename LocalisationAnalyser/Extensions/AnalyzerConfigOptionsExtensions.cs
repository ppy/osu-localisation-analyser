// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using LocalisationAnalyser.Localisation;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LocalisationAnalyser.Extensions
{
    public static class AnalyzerConfigOptionsExtensions
    {
        public static string GetLocalisationClassNamespace(this AnalyzerConfigOptions? options)
        {
            if (options == null || !options.TryGetValue($"dotnet_diagnostic.{DiagnosticRules.STRING_CAN_BE_LOCALISED.Id}.class_namespace", out string? customFileNamespace))
                return SyntaxTemplates.LOCALISATION_CLASS_NAMESPACE;

            if (string.IsNullOrEmpty(customFileNamespace))
                throw new InvalidOperationException("Custom file namespace cannot be empty.");

            return customFileNamespace;
        }
    }
}
