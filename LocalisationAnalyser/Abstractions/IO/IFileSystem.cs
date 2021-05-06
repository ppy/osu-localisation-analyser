// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace LocalisationAnalyser.Abstractions.IO
{
    public interface IFileSystem
    {
        IPath Path { get; }

        IFile File { get; }

        IDirectory Directory { get; }

        IFileInfoFactory FileInfo { get; }
    }
}
