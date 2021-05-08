// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace LocalisationAnalyser.Abstractions.IO.Default
{
    internal class DefaultFileSystem : IFileSystem
    {
        public IPath Path { get; } = new DefaultPath();

        public IFile File { get; } = new DefaultFile();

        public IDirectory Directory { get; } = new DefaultDirectory();

        public IFileInfoFactory FileInfo { get; }

        public DefaultFileSystem()
        {
            FileInfo = new DefaultFileInfoFactory(this);
        }
    }
}
