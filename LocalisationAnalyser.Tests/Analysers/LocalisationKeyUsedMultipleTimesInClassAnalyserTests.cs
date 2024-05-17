using System.Threading.Tasks;
using LocalisationAnalyser.Analysers;
using Xunit;

namespace LocalisationAnalyser.Tests.Analysers
{
    public class LocalisationKeyUsedMultipleTimesInClassAnalyserTests : AbstractAnalyserTests<LocalisationKeyUsedMultipleTimesInClassAnalyser>
    {
        [Theory]
        [InlineData("DuplicatedLocalisationKeys")]
        public Task RunTest(string name) => Check(name);
    }
}