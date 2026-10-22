using System.Reflection;
using NetArchTest.Rules;

namespace OpenSpec.ArchitectureTests;

public class ModuleBoundaryTests
{
    private static readonly string[] Modules = ["Users", "Catalog", "Orders"];
    private static readonly string[] Layers = ["Domain", "Application", "Infrastructure", "Api"];

    public static TheoryData<string, string> ModulePairs()
    {
        var data = new TheoryData<string, string>();
        foreach (var source in Modules)
            foreach (var target in Modules.Where(m => m != source))
                data.Add(source, target);
        return data;
    }

    public static TheoryData<string> AllModules() => new(Modules);

    [Theory]
    [MemberData(nameof(ModulePairs))]
    public void Module_ShouldOnlyDependOnContractsOfOtherModules(string source, string target)
    {
        var forbidden = Layers.Select(l => $"OpenSpec.Modules.{target}.{l}").ToArray();

        foreach (var layer in Layers)
        {
            var result = Types.InAssembly(Assembly.Load($"OpenSpec.Modules.{source}.{layer}"))
                .ShouldNot().HaveDependencyOnAny(forbidden)
                .GetResult();

            Assert.True(result.IsSuccessful,
                $"{source}.{layer} phụ thuộc vào {target}: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
    }

    [Theory]
    [MemberData(nameof(AllModules))]
    public void Domain_ShouldNotDependOnInfrastructure(string module)
    {
        var result = Types.InAssembly(Assembly.Load($"OpenSpec.Modules.{module}.Domain"))
            .ShouldNot().HaveDependencyOnAny("Microsoft.EntityFrameworkCore", $"OpenSpec.Modules.{module}.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }
}
