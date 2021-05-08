// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using System.Threading.Tasks;

namespace LocalisationAnalyser.Abstractions.IO
{
    internal interface IFile
    {
        Task<string> ReadAllTextAsync(string fullname, CancellationToken cancellationToken);
    }
}
