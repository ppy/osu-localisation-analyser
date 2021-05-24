// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;

namespace LocalisationAnalyser.Abstractions.IO.Default
{
    internal class DefaultFile : IFile
    {
        public string ReadAllText(string fullname) => File.ReadAllText(fullname);
    }
}
