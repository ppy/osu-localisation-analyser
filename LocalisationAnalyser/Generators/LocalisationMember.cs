using System.Collections.Immutable;

namespace LocalisationAnalyser.Generators
{
    public class LocalisationMember
    {
        public readonly string Name;
        public readonly string Key;
        public readonly string EnglishText;
        public readonly ImmutableArray<LocalisationParameter> Parameters;

        public LocalisationMember(string name, string key, string englishText, LocalisationParameter[]? parameters = null)
        {
            Name = name;
            Key = key;
            EnglishText = englishText;
            Parameters = parameters?.ToImmutableArray() ?? ImmutableArray<LocalisationParameter>.Empty;
        }
    }
}