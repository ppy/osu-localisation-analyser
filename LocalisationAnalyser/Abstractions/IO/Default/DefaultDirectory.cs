// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;

namespace LocalisationAnalyser.Abstractions.IO.Default
{
    public class DefaultDirectory : IDirectory
    {
        public void CreateDirectory(string name) => Directory.CreateDirectory(name);
    }
}
