// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace LocalisationAnalyser.Localisation
{
    public class MalformedLocalisationException : Exception
    {
        public MalformedLocalisationException(string message)
            : base(message)
        {
        }
    }
}
