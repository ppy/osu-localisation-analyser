// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace LocalisationAnalyser.Localisation
{
    public class LocalisationParameter : IEquatable<LocalisationParameter>
    {
        public readonly string Type;
        public readonly string Name;

        public LocalisationParameter(string type, string name)
        {
            Type = type;
            Name = name;
        }

        public bool Equals(LocalisationParameter? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Type == other.Type && Name == other.Name;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;

            return Equals((LocalisationParameter)obj);
        }

        public override int GetHashCode() => HashCode.Combine(Type, Name);
    }
}
