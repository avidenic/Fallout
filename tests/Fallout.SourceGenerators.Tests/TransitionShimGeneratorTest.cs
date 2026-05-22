// Copyright 2026 Maintainers of Fallout.
// Originally based on NUKE by Matthias Koch and contributors.
// Distributed under the MIT License.
// https://github.com/ChrisonSimtian/Fallout/blob/main/LICENSE

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using VerifyXunit;
using Xunit;

namespace Fallout.SourceGenerators.Tests;

public class TransitionShimGeneratorTest
{
    // Each kind in the Easy tier (regular class, abstract class, interface,
    // attribute, generic class, nested type) emits a representative shim. The
    // Hard tier kinds (sealed class, static class, enum, delegate, class with
    // no public/protected ctor) emit SHIM001 diagnostics instead. Captures both
    // as a Verify snapshot.
    [Fact]
    public Task EmitsShimsForEachKindAndSkipsHardTier()
    {
        var canonical = CompileCanonicalAssembly("""
            namespace Fallout.Common
            {
                // Easy tier
                public class Regular { public Regular(string a) {} public Regular() {} }
                public abstract class Abstr { protected Abstr() {} }
                public interface IFoo { }
                [System.AttributeUsage(System.AttributeTargets.Class)]
                public class MyAttr : System.Attribute { public MyAttr(int n) {} }
                public class Generic<T> where T : class { public Generic(T item) {} }
                public class WithNested
                {
                    public WithNested() {}
                    public class Nested { public Nested() {} }
                }

                // Hard tier — should be skipped with SHIM001
                public sealed class SealedThing { public SealedThing() {} }
                public static class StaticHelpers { }
                public enum MyEnum { A, B }
                public class PrivateCtorOnly { private PrivateCtorOnly(string x) {} }
            }
            """);

        var shimCompilation = CSharpCompilation.Create("Nuke.TestShim",
            new[] { CSharpSyntaxTree.ParseText("""
                [assembly: Fallout.Migrate.Shims.ShimAllPublicTypesUnder("Fallout.Common", "Nuke.Common")]
                """) },
            Basic.Reference.Assemblies.NetStandard20.References.All
                .Concat(new[] { canonical }),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver.Create(new TransitionShimGenerator());
        var result = driver.RunGenerators(shimCompilation);
        return Verifier.Verify(result);
    }

    [Fact]
    public void EmitsNothingWhenNoMarkerAttributePresent()
    {
        var canonical = CompileCanonicalAssembly("""
            namespace Fallout.Common
            {
                public class Whatever { public Whatever() {} }
            }
            """);

        var shimCompilation = CSharpCompilation.Create("Nuke.NoMarker",
            new[] { CSharpSyntaxTree.ParseText("// no marker") },
            Basic.Reference.Assemblies.NetStandard20.References.All
                .Concat(new[] { canonical }),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver.Create(new TransitionShimGenerator());
        var result = driver.RunGenerators(shimCompilation).GetRunResult();

        // The post-init attribute definition is the only output expected.
        result.GeneratedTrees
            .Where(t => !t.FilePath.EndsWith("ShimAllPublicTypesUnderAttribute.g.cs", System.StringComparison.Ordinal))
            .Should().BeEmpty();
    }

    private static MetadataReference CompileCanonicalAssembly(string source)
    {
        var compilation = CSharpCompilation.Create("Fallout.TestCanonical",
            new[] { CSharpSyntaxTree.ParseText(source) },
            Basic.Reference.Assemblies.NetStandard20.References.All,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var stream = new MemoryStream();
        var emit = compilation.Emit(stream);
        emit.Success.Should().BeTrue(
            because: "canonical test compilation should compile: {0}",
            string.Join("; ", emit.Diagnostics.Select(d => d.GetMessage())));
        stream.Position = 0;
        return MetadataReference.CreateFromStream(stream);
    }
}
