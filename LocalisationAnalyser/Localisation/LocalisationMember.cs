// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Immutable;

namespace LocalisationAnalyser.Localisation
{
    /// <summary>
    /// Represents a localisation method or member within a <see cref="LocalisationFile"/>.
    /// </summary>
    public class LocalisationMember : IEquatable<LocalisationMember>
    {
        /// <summary>
        /// The name.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// The key to use for lookups.
        /// </summary>
        public readonly string Key;

        /// <summary>
        /// The english default text. This is also used for the XMLDoc.
        /// </summary>
        public readonly string EnglishText;

        /// <summary>
        /// The XMLDoc for this member. Usually matches <see cref="EnglishText"/>.
        /// </summary>
        /// <remarks>
        /// This value is not XML-encoded.
        /// </remarks>
        public readonly string XmlDoc;

        /// <summary>
        /// Any parameters. If this is non-empty, the <see cref="LocalisationMember"/> represents a method.
        /// </summary>
        public readonly ImmutableArray<LocalisationParameter> Parameters;

        /// <summary>
        /// Creates a new <see cref="LocalisationMember"/>.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="key">The localisation key to use for lookups.</param>
        /// <param name="englishText">The english default text. This is also used for the XMLDoc.</param>
        /// <param name="parameters">Any parameters. If this is non-empty, the <see cref="LocalisationMember"/> will represent a method.</param>
        public LocalisationMember(string name, string key, string englishText, params LocalisationParameter[] parameters)
            : this(name, key, englishText, englishText, parameters)
        {
        }

        /// <summary>
        /// Creates a new <see cref="LocalisationMember"/>.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="key">The localisation key to use for lookups.</param>
        /// <param name="englishText">The english default text. This is also used for the XMLDoc.</param>
        /// <param name="xmlDoc">The XMLDoc for this member. Usually matches <paramref name="englishText"/> but may be provided separately.
        /// Must not be XML-encoded.</param>
        /// <param name="parameters">Any parameters. If this is non-empty, the <see cref="LocalisationMember"/> will represent a method.</param>
        public LocalisationMember(string name, string key, string englishText, string xmlDoc, params LocalisationParameter[] parameters)
        {
            Name = name;
            Key = key;
            EnglishText = englishText;
            XmlDoc = xmlDoc;
            Parameters = parameters.ToImmutableArray();
        }

        public bool Equals(LocalisationMember? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Name == other.Name && Key == other.Key && EnglishText == other.EnglishText && XmlDoc == other.XmlDoc && Parameters.Equals(other.Parameters);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;

            return Equals((LocalisationMember)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Name.GetHashCode();
                hashCode = (hashCode * 397) ^ Key.GetHashCode();
                hashCode = (hashCode * 397) ^ EnglishText.GetHashCode();
                hashCode = (hashCode * 397) ^ XmlDoc.GetHashCode();
                hashCode = (hashCode * 397) ^ Parameters.GetHashCode();
                return hashCode;
            }
        }
    }
}
