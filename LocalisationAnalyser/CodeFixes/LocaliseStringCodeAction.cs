// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;

namespace LocalisationAnalyser.CodeFixes
{
    internal class LocaliseStringCodeAction : CodeAction
    {
        public override string Title { get; }
        public override string? EquivalenceKey { get; }

        private readonly LocaliseStringDelegate createChangedSolution;

        public LocaliseStringCodeAction(string title, LocaliseStringDelegate createChangedSolution, string? equivalenceKey = null)
        {
            this.createChangedSolution = createChangedSolution;

            Title = title;
            EquivalenceKey = equivalenceKey;
        }

        protected override Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken) => createChangedSolution(false, cancellationToken);

        protected override async Task<IEnumerable<CodeActionOperation>?> ComputePreviewOperationsAsync(CancellationToken cancellationToken)
        {
            var changedSolution = await createChangedSolution(true, cancellationToken).ConfigureAwait(false);
            if (changedSolution == null)
                return null;

            return new CodeActionOperation[] { new ApplyChangesOperation(changedSolution) };
        }
    }

    internal delegate Task<Solution> LocaliseStringDelegate(bool preview, CancellationToken cancellationToken);
}
