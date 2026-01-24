using System;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.ReportGenerator;
using Nuke.Common.Utilities.Collections;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.ReportGenerator.ReportGeneratorTasks;

[ShutdownDotNetAfterServerBuild]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly string Configuration = IsLocalBuild ? "Debug" : "Release";

    [Parameter("Enable code coverage collection")]
    readonly bool CoverageEnabled = true;

    [Parameter("Verbosity level for build output")]
    readonly DotNetVerbosity BuildVerbosity = DotNetVerbosity.minimal;

    [Solution(GenerateProjects = false)]
    readonly Solution Solution;

    [GitVersion(NoFetch = true, Framework = "net10.0")]
    [CanBeNull]
    readonly GitVersion GitVersion;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath TestsDirectory => RootDirectory / "tests";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath TestResultsDirectory => ArtifactsDirectory / "test-results";
    AbsolutePath CoverageDirectory => ArtifactsDirectory / "coverage";
    AbsolutePath PublishDirectory => ArtifactsDirectory / "publish";
    AbsolutePath PackagesDirectory => ArtifactsDirectory / "packages";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(d => d.DeleteDirectory());
            TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(d => d.DeleteDirectory());
            ArtifactsDirectory.CreateOrCleanDirectory();
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetAssemblyVersion(GitVersion?.AssemblySemVer)
                .SetFileVersion(GitVersion?.AssemblySemFileVer)
                .SetInformationalVersion(GitVersion?.InformationalVersion)
                .EnableNoRestore()
                .SetVerbosity(BuildVerbosity));
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Produces(TestResultsDirectory / "*.trx")
        .Produces(CoverageDirectory / "*.xml")
        .Executes(() =>
        {
            var testSettings = new DotNetTestSettings()
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoBuild()
                .EnableNoRestore()
                .SetResultsDirectory(TestResultsDirectory)
                .SetLoggers("trx")
                .SetVerbosity(BuildVerbosity);

            if (CoverageEnabled)
            {
                testSettings = testSettings
                    .SetDataCollector("XPlat Code Coverage")
                    .SetProperty("CollectCoverage", true)
                    .SetProperty("CoverletOutputFormat", "cobertura")
                    .SetProperty("CoverletOutput", CoverageDirectory / "coverage.cobertura.xml")
                    .SetProperty("ExcludeByFile", "**/*Designer.cs")
                    .SetProperty("ExcludeByAttribute", "Obsolete,GeneratedCodeAttribute,CompilerGeneratedAttribute");
            }

            DotNetTest(testSettings);
        });

    Target IntegrationTest => _ => _
        .DependsOn(Compile)
        .Produces(TestResultsDirectory / "integration-*.trx")
        .Executes(() =>
        {
            var integrationTestProjects = TestsDirectory.GlobFiles("**/*.Tests.csproj")
                .Where(x => x.ToString().Contains("Integration", StringComparison.OrdinalIgnoreCase));

            DotNetTest(s => s
                .SetConfiguration(Configuration)
                .EnableNoBuild()
                .EnableNoRestore()
                .SetResultsDirectory(TestResultsDirectory)
                .SetLoggers("trx;LogFileName=integration-tests.trx")
                .SetVerbosity(BuildVerbosity)
                .CombineWith(integrationTestProjects, (cs, project) => cs
                    .SetProjectFile(project)));
        });

    Target Coverage => _ => _
        .DependsOn(Test)
        .Produces(CoverageDirectory / "*.xml")
        .Executes(() =>
        {
            Log.Information("Coverage files generated in: {Directory}", CoverageDirectory);

            var coverageFiles = CoverageDirectory.GlobFiles("**/*.cobertura.xml");
            foreach (var file in coverageFiles)
            {
                Log.Information("Coverage file: {File}", file);
            }
        });

    Target CoverageReport => _ => _
        .DependsOn(Coverage)
        .Produces(CoverageDirectory / "report" / "**/*")
        .Executes(() =>
        {
            ReportGenerator(s => s
                .SetReports(CoverageDirectory / "**/*.cobertura.xml")
                .SetTargetDirectory(CoverageDirectory / "report")
                .SetReportTypes(
                    ReportTypes.Html,
                    ReportTypes.Badges,
                    ReportTypes.TextSummary,
                    ReportTypes.Cobertura)
                .SetFramework("net10.0"));

            Log.Information("Coverage report generated: {Report}", CoverageDirectory / "report" / "index.html");
        });

    Target Pack => _ => _
        .DependsOn(Compile)
        .Produces(PackagesDirectory / "*.nupkg")
        .Executes(() =>
        {
            DotNetPack(s => s
                .SetProject(Solution)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(PackagesDirectory)
                .SetVersion(GitVersion?.NuGetVersionV2)
                .EnableNoBuild()
                .EnableNoRestore()
                .SetVerbosity(BuildVerbosity));
        });

    Target Publish => _ => _
        .DependsOn(Compile)
        .Produces(PublishDirectory / "**/*")
        .Executes(() =>
        {
            var publishProjects = new[]
            {
                Solution.GetProject("PokManager.ApiService"),
                Solution.GetProject("PokManager.Web"),
                Solution.GetProject("PokManager.AppHost")
            }.Where(p => p != null);

            DotNetPublish(s => s
                .SetConfiguration(Configuration)
                .SetOutput(PublishDirectory)
                .EnableNoBuild()
                .EnableNoRestore()
                .SetVerbosity(BuildVerbosity)
                .CombineWith(publishProjects, (ps, project) => ps
                    .SetProject(project)
                    .SetOutput(PublishDirectory / project.Name)));
        });

    Target Format => _ => _
        .Executes(() =>
        {
            DotNet($"format \"{Solution}\" --verbosity {Verbosity}");
        });

    Target FormatVerify => _ => _
        .Executes(() =>
        {
            DotNet($"format \"{Solution}\" --verify-no-changes --verbosity {Verbosity}");
        });

    Target Lint => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .SetProperty("EnforceCodeStyleInBuild", true)
                .SetProperty("TreatWarningsAsErrors", true)
                .SetVerbosity(BuildVerbosity));
        });

    Target Default => _ => _
        .DependsOn(Restore)
        .DependsOn(Compile)
        .DependsOn(Test);

    Target Full => _ => _
        .DependsOn(Clean)
        .DependsOn(Restore)
        .DependsOn(Compile)
        .DependsOn(Test)
        .DependsOn(CoverageReport)
        .DependsOn(Pack)
        .DependsOn(Publish);

    Target CI => _ => _
        .DependsOn(Clean)
        .DependsOn(Restore)
        .DependsOn(Compile)
        .DependsOn(Test)
        .DependsOn(Coverage)
        .DependsOn(Pack);
}
