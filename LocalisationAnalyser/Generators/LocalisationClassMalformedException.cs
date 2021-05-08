// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace LocalisationAnalyser.Generators
{
    public class LocalisationClassMalformedException : Exception
    {
        public LocalisationClassMalformedException(string message)
            : base(message)
        {
        }
    }
}
