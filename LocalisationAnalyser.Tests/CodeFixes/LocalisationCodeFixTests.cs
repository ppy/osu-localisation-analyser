// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Tasks;
using Xunit;
using Verify = LocalisationAnalyser.Tests.Verifiers.CSharpCodeFixVerifier<LocalisationAnalyser.LocalisationAnalyser, LocalisationAnalyser.Tests.CodeFixes.MockLocalisationCodeFixProvider>;

namespace LocalisationAnalyser.Tests.CodeFixes
{
    public class LocalisationCodeFixTests
    {
        [Fact]
        public async Task LocaliseLiteralString()
        {
            await Verify.VerifyCodeFixAsync(@"
class Program
{
    static void Main()
    {
        string x = [|""abc""|];
    }
}", @"
class Program
{
    static void Main()
    {
        string x = string.Empty;
    }
}");
        }
    }
}
