// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace LocalisationAnalyser.Localisation
{
    /// <summary>
    /// A method parameter of <see cref="LocalisationMember"/>s that represent methods.
    /// </summary>
    public class LocalisationParameter : IEquatable<LocalisationParameter>
    {
        /// <summary>
        /// The type.
        /// </summary>
        public readonly string Type;

        /// <summary>
        /// The name.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Creates a new <see cref="LocalisationParameter"/>.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="name">The name.</param>
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

        public override int GetHashCode()
        {
            unchecked
            {
                return (Type.GetHashCode() * 397) ^ Name.GetHashCode();
            }
        }
    }
}
