// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using LocalisationAnalyser.Localisation;

namespace LocalisationAnalyser.Tools
{
    public class LocalisationMemberKeyEqualityComparer : IEqualityComparer<LocalisationMember>
    {
        public bool Equals(LocalisationMember? x, LocalisationMember? y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (ReferenceEquals(x, null))
                return false;

            if (ReferenceEquals(y, null))
                return false;

            if (x.GetType() != y.GetType())
                return false;

            return x.Key == y.Key;
        }

        public int GetHashCode(LocalisationMember obj) => obj.Key.GetHashCode();
    }
}
