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
        public string Type { get; init; }

        /// <summary>
        /// The name.
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// Whether this parameter represents a quantity.
        /// Controls whether the localisation member is a pluralisable string or not.
        /// </summary>
        public bool IsQuantity { get; init; }

        /// <summary>
        /// Creates a new <see cref="LocalisationParameter"/>.
        /// </summary>
        /// <param name="type">The parameter type.</param>
        /// <param name="name">The parameter name.</param>
        /// <param name="isQuantity">For pluralisable strings, whether this parameter represents the quantity.</param>
        public LocalisationParameter(string type, string name, bool isQuantity = false)
        {
            Type = type;
            Name = name;
            IsQuantity = isQuantity;
        }

        /// <summary>
        /// Creates a new <see cref="LocalisationParameter"/>, using another as a reference.
        /// </summary>
        /// <param name="other">The other <see cref="LocalisationParameter"/>.</param>
        public LocalisationParameter(LocalisationParameter other)
        {
            Type = other.Type;
            Name = other.Name;
            IsQuantity = other.IsQuantity;
        }

        public bool Equals(LocalisationParameter? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Type == other.Type && Name == other.Name && IsQuantity == other.IsQuantity;
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
                return (Type.GetHashCode() * 397) ^ (Name.GetHashCode() * 397) ^ IsQuantity.GetHashCode();
            }
        }
    }
}
