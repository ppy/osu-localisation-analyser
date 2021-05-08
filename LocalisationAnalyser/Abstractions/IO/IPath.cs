// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace LocalisationAnalyser.Abstractions.IO
{
    public interface IPath
    {
        string GetDirectoryName(string path);

        string Combine(params string[] paths);

        string ChangeExtension(string path, string newExtension);

        string GetFileName(string path);
    }
}
