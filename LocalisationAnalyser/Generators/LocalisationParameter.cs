namespace LocalisationAnalyser.Generators
{
    public class LocalisationParameter
    {
        public readonly string Type;
        public readonly string Name;

        public LocalisationParameter(string type, string name)
        {
            Type = type;
            Name = name;
        }
    }
}