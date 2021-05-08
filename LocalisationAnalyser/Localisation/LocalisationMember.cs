// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Immutable;

namespace LocalisationAnalyser.Localisation
{
    public class LocalisationMember
    {
        public readonly string Name;
        public readonly string Key;
        public readonly string EnglishText;
        public readonly ImmutableArray<LocalisationParameter> Parameters;

        public LocalisationMember(string name, string key, string englishText, params LocalisationParameter[] parameters)
        {
            Name = name;
            Key = key;
            EnglishText = englishText;
            Parameters = parameters.ToImmutableArray();
        }
    }
}