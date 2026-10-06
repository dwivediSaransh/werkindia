using System.Xml.Linq;

namespace IndiaWorks.ArchitectureTests;

public sealed class ProjectDependencyTests
{
    [Fact]
    public void Backend_projects_follow_declared_dependency_direction()
    {
        var root = FindRepositoryRoot();

        // Verify assembly markers exist for each project
        Assert.True(File.Exists(root.Project("IndiaWorks.Domain").Replace("IndiaWorks.Domain.csproj", "AssemblyMarker.cs")));
        Assert.True(File.Exists(root.Project("IndiaWorks.Application").Replace("IndiaWorks.Application.csproj", "AssemblyMarker.cs")));
        Assert.True(File.Exists(root.Project("IndiaWorks.Infrastructure").Replace("IndiaWorks.Infrastructure.csproj", "AssemblyMarker.cs")));
        Assert.True(File.Exists(root.Project("IndiaWorks.Contracts").Replace("IndiaWorks.Contracts.csproj", "AssemblyMarker.cs")));
        Assert.True(File.Exists(root.Project("IndiaWorks.Api").Replace("IndiaWorks.Api.csproj", "Program.cs")));
        Assert.True(File.Exists(root.Project("IndiaWorks.Workers").Replace("IndiaWorks.Workers.csproj", "Program.cs")));

        // Domain and Contracts are the foundation - no project references
        AssertReferences(root, "IndiaWorks.Domain", []);
        AssertReferences(root, "IndiaWorks.Contracts", []);

        // Application references Domain and Contracts
        AssertReferences(root, "IndiaWorks.Application", ["IndiaWorks.Domain", "IndiaWorks.Contracts"]);

        // Infrastructure references Application and Domain
        AssertReferences(root, "IndiaWorks.Infrastructure", ["IndiaWorks.Application", "IndiaWorks.Domain"]);

        // Api references Application, Infrastructure, and Contracts
        AssertReferences(root, "IndiaWorks.Api", ["IndiaWorks.Application", "IndiaWorks.Infrastructure", "IndiaWorks.Contracts"]);

        // Workers references Application and Infrastructure
        AssertReferences(root, "IndiaWorks.Workers", ["IndiaWorks.Application", "IndiaWorks.Infrastructure"]);
    }

    private static void AssertReferences(PathInfo root, string projectName, string[] expectedProjectNames)
    {
        var project = root.Project(projectName);
        var document = XDocument.Load(project);
        var actual = document
            .Descendants("ProjectReference")
            .Select(element => Path.GetFileNameWithoutExtension((string?)element.Attribute("Include") ?? string.Empty))
            .Order()
            .ToArray();

        Assert.Equal(expectedProjectNames.Order().ToArray(), actual);
    }

    private static PathInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "IndiaWorks.slnx")))
            {
                return new PathInfo(directory.FullName);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate IndiaWorks.slnx from test output directory.");
    }

    private sealed record PathInfo(string Root)
    {
        public string Project(string projectName)
        {
            return Path.Combine(Root, "src", "backend", projectName, $"{projectName}.csproj");
        }
    }
}
