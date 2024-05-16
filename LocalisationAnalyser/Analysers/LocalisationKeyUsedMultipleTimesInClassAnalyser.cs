using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LocalisationAnalyser.Analysers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class LocalisationKeyUsedMultipleTimesInClassAnalyser : AbstractMemberAnalyser
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(DiagnosticRules.LOCALISATION_KEY_USED_MULTIPLE_TIMES_IN_CLASS);
    }
}