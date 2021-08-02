// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.CodeAnalysis.Diagnostics;

namespace LocalisationAnalyser.Tools
{
    public class OsuAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, out string value)
        {
            // License header is hard-coded for now as the tool is meant for internal osu! usage only.
            if (key == $"dotnet_diagnostic.{DiagnosticRules.STRING_CAN_BE_LOCALISED.Id}.license_header")
            {
                value = "// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.\n// See the LICENCE file in the repository root for full licence text.";
                return true;
            }

            value = string.Empty;
            return false;
        }
    }
}
