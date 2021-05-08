// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;

namespace LocalisationAnalyser.Abstractions.IO
{
    internal interface IFileInfo
    {
        bool Exists { get; }

        string FullName { get; }

        string DirectoryName { get; }

        IFileSystem FileSystem { get; }

        Stream OpenRead();

        Stream OpenWrite();
    }
}
