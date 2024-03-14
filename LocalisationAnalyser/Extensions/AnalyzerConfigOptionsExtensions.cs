// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LocalisationAnalyser.Extensions
{
    public static class AnalyzerConfigOptionsExtensions
    {
        /// <summary>
        /// Retrieves the namespace under which the localisation strings are expected to be placed.
        /// </summary>
        /// <param name="config">The config that may provide a custom value via <c>localisation_namespace</c>.</param>
        /// <param name="fallback">The fallback value in case no custom value is provided.</param>
        /// <returns>The localisation strings namespace - either a custom value from <paramref name="config"/>, or the given <paramref name="fallback"/>.</returns>
        /// <exception cref="InvalidOperationException">If the config contained an empty string as a custom value.</exception>
        public static string GetLocalisationNamespace(this AnalyzerConfigOptions? config, string fallback)
        {
            if (config == null)
                return fallback;

            if (config.TryGetValue($"dotnet_diagnostic.{DiagnosticRules.STRING_CAN_BE_LOCALISED.Id}.localisation_namespace", out string? customFileNamespace))
            {
                if (string.IsNullOrEmpty(customFileNamespace))
                    throw new InvalidOperationException("Custom localisation namespace cannot be empty.");

                return customFileNamespace;
            }

            return fallback;
        }

        /// <summary>
        /// Retrieves the namespace under which the localisation resources are expected to be found.
        /// </summary>
        /// <param name="config">The config that may provide a custom value via either <c>resource_namespace</c> or <c>prefix_namespace</c>.</param>
        /// <param name="fallback">The fallback value in case no custom value is provided.</param>
        /// <returns>The localisation resource namespace - either a custom value from <paramref name="config"/>, or the given <paramref name="fallback"/>.</returns>
        /// <exception cref="InvalidOperationException">If the config contained an empty string as a custom value.</exception>
        public static string GetResourceNamespace(this AnalyzerConfigOptions? config, string fallback)
        {
            if (config == null)
                return fallback;

            if (config.TryGetValue($"dotnet_diagnostic.{DiagnosticRules.STRING_CAN_BE_LOCALISED.Id}.resource_namespace", out string? customResourceNamespace))
            {
                if (string.IsNullOrEmpty(customResourceNamespace))
                    throw new InvalidOperationException("Custom resource namespace cannot be empty.");

                return customResourceNamespace;
            }

            if (config.TryGetValue($"dotnet_diagnostic.{DiagnosticRules.STRING_CAN_BE_LOCALISED.Id}.prefix_namespace", out string? customPrefixNamespace))
            {
                if (string.IsNullOrEmpty(customPrefixNamespace))
                    throw new InvalidOperationException("Custom prefix namespace cannot be empty.");

                return customPrefixNamespace;
            }

            return fallback;
        }

        public static string[]? GetLicenseHeader(this AnalyzerConfigOptions? config)
        {
            if (config == null)
                return null;

            if (!config.TryGetValue($"dotnet_diagnostic.{DiagnosticRules.STRING_CAN_BE_LOCALISED.Id}.license_header", out string licenseHeader))
                return null;

            if (string.IsNullOrEmpty(licenseHeader))
                return null;

            if (!licenseHeader.StartsWith("//", StringComparison.Ordinal))
                licenseHeader = $"// {licenseHeader}";

            return licenseHeader.Split(new[] { "\\n" }, StringSplitOptions.None);
        }
    }
}
