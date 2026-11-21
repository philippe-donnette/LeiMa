using System.Reflection;
using System.Xml.Linq;

namespace LeiMa.Runner.ArchitectureTests;

/// <summary>Enforces the dependency rules of ADR 0003: references point inward only.</summary>
public class LayerDependencyTests
{
    private const string Prefix = "LeiMa.Runner.";

    private static readonly Dictionary<string, string[]> AllowedLayers = new()
    {
        ["Domain"] = [],
        ["Application"] = ["Domain"],
        ["Infrastructure"] = ["Domain", "Application"],
        ["Api"] = ["Domain", "Application", "Infrastructure"],

        // A second composition root next to the Api, not a layer: nothing may reference it (ADR 0005).
        ["Migrator"] = ["Application", "Infrastructure"],
    };

    private static readonly string[] ForbiddenInCore =
    [
        "Microsoft.EntityFrameworkCore", "Npgsql", "Microsoft.AspNetCore", "Azure", "Microsoft.Azure", "Amazon",
    ];

    private static readonly string[] ForbiddenAttributeNamespaces =
    [
        "Microsoft.EntityFrameworkCore", "System.ComponentModel.DataAnnotations",
    ];

    public static IEnumerable<object[]> AllLayers => AllowedLayers.Keys.Select(layer => new object[] { layer });

    public static IEnumerable<object[]> CoreLayers => new[] { "Domain", "Application" }.Select(layer => new object[] { layer });

    [Theory]
    [MemberData(nameof(AllLayers))]
    public void ProjectReferences_Layer_PointInwardOnly(string layer)
    {
        var referenced = ProjectFile(layer).Descendants("ProjectReference")
            .Select(e => Path.GetFileNameWithoutExtension(((string)e.Attribute("Include")!).Replace('\\', '/')))
            .Where(name => name.StartsWith(Prefix, StringComparison.Ordinal))
            .Select(name => name[Prefix.Length..]);

        AssertNoViolations(
            referenced.Except(AllowedLayers[layer]),
            $"{layer} has a project reference to a layer it may not depend on (allowed: {Describe(AllowedLayers[layer])}); dependencies must point inward only (ADR 0003)");
    }

    [Theory]
    [MemberData(nameof(AllLayers))]
    public void CompiledReferences_Layer_PointInwardOnly(string layer)
    {
        var referenced = Assembly.Load(Prefix + layer).GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(name => name.StartsWith(Prefix, StringComparison.Ordinal))
            .Select(name => name[Prefix.Length..]);

        AssertNoViolations(
            referenced.Except(AllowedLayers[layer]),
            $"The compiled {layer} assembly uses types from a layer it may not depend on (allowed: {Describe(AllowedLayers[layer])}); dependencies must point inward only (ADR 0003)");
    }

    [Theory]
    [MemberData(nameof(CoreLayers))]
    public void Packages_CoreLayer_HasNoOrmWebOrCloudDependency(string layer)
    {
        var packages = ProjectFile(layer).Descendants("PackageReference")
            .Select(e => (string)e.Attribute("Include")!);
        var assemblies = Assembly.Load(Prefix + layer).GetReferencedAssemblies().Select(a => a.Name!);

        AssertNoViolations(
            packages.Concat(assemblies).Where(IsForbiddenInCore).Distinct(),
            $"{layer} depends on an ORM, web framework or cloud SDK; these belong in Infrastructure or Api behind ports (ADR 0003)");
    }

    [Fact]
    public void Attributes_Domain_HaveNoOrmOrDataAnnotationAttributes()
    {
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        var domain = Assembly.Load(Prefix + "Domain");

        var violations = domain.GetTypes()
            .SelectMany(type => new MemberInfo[] { type }.Concat(type.GetMembers(all)))
            .SelectMany(member => member.GetCustomAttributesData().Select(attribute => (member, attribute)))
            .Where(found => ForbiddenAttributeNamespaces.Any(ns => (found.attribute.AttributeType.Namespace ?? string.Empty).StartsWith(ns, StringComparison.Ordinal)))
            .Select(found => $"{found.member.DeclaringType?.Name ?? found.member.Name}.{found.member.Name} has [{found.attribute.AttributeType.Name}]")
            .Distinct();

        AssertNoViolations(
            violations,
            "Domain classes must carry no ORM or data annotation attributes; the mapping lives in Infrastructure configuration classes and the rules in the domain code (ADR 0003)");
    }

    private static string Describe(string[] layers) => layers.Length == 0 ? "none" : string.Join(", ", layers);

    private static void AssertNoViolations(IEnumerable<string> violations, string rule)
    {
        var found = violations.ToList();
        if (found.Count > 0)
        {
            Assert.Fail($"{rule}. Violations: {string.Join(", ", found)}");
        }
    }

    private static bool IsForbiddenInCore(string name) =>
        ForbiddenInCore.Any(p => name == p || name.StartsWith(p + ".", StringComparison.Ordinal));

    private static XDocument ProjectFile(string layer)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "LeiMa.Runner.slnx")))
        {
            root = root.Parent;
        }

        Assert.True(root is not null, "LeiMa.Runner.slnx was not found above the test output folder, so the project files cannot be located.");
        return XDocument.Load(Path.Combine(root.FullName, "src", Prefix + layer, Prefix + layer + ".csproj"));
    }
}
