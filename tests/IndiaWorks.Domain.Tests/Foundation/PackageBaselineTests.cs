using System.Text.Json;
using System.Xml.Linq;

namespace IndiaWorks.Domain.Tests.Foundation;

public sealed class PackageBaselineTests
{
    [Fact]
    public void Repository_pins_dotnet_sdk_and_central_package_versions()
    {
        var root = FindRepositoryRoot();

        using var globalJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "global.json")));
        Assert.Equal("10.0.401", globalJson.RootElement.GetProperty("sdk").GetProperty("version").GetString());

        var packages = XDocument.Load(Path.Combine(root, "Directory.Packages.props"))
            .Descendants("PackageVersion")
            .ToDictionary(
                element => (string)element.Attribute("Include")!,
                element => (string)element.Attribute("Version")!);

        Assert.Equal("10.0.12", packages["Microsoft.EntityFrameworkCore"]);
        Assert.Equal("10.0.12", packages["Microsoft.EntityFrameworkCore.SqlServer"]);
        Assert.Equal("10.0.12", packages["Microsoft.EntityFrameworkCore.Design"]);
        Assert.Equal("16.6.7", packages["HotChocolate.AspNetCore"]);
    }

    [Fact]
    public void Frontend_project_uses_angular_stack()
    {
        var root = FindRepositoryRoot();

        using var packageJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "src", "web", "package.json")));
        var dependencies = packageJson.RootElement.GetProperty("dependencies");
        var devDependencies = packageJson.RootElement.GetProperty("devDependencies");

        Assert.Equal("indiaworks-web", packageJson.RootElement.GetProperty("name").GetString());
        Assert.True(dependencies.TryGetProperty("@angular/core", out _));
        Assert.True(dependencies.TryGetProperty("@angular/router", out _));
        Assert.True(dependencies.TryGetProperty("@angular/ssr", out _));
        Assert.True(devDependencies.TryGetProperty("@angular/cli", out _));
        Assert.True(devDependencies.TryGetProperty("typescript", out _));

        Assert.False(dependencies.TryGetProperty("react", out _));
        Assert.False(dependencies.TryGetProperty("next", out _));
        Assert.False(dependencies.TryGetProperty("vue", out _));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "IndiaWorks.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate IndiaWorks.slnx from test output directory.");
    }
}
