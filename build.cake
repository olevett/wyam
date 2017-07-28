#tool "nuget:https://api.nuget.org/v3/index.json?package=Wyam&prerelease"
#addin "nuget:https://api.nuget.org/v3/index.json?package=Cake.Git"
#addin "nuget:https://api.nuget.org/v3/index.json?package=Cake.Wyam&prerelease"
#addin "nuget:https://api.nuget.org/v3/index.json?package=Cake.Yaml"
#addin "nuget:https://api.nuget.org/v3/index.json?package=Octokit"

using Octokit;

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define variables
var isRunningOnAppVeyor = AppVeyor.IsRunningOnAppVeyor;
var isPullRequest       = AppVeyor.Environment.PullRequest.IsPullRequest;
var accessToken         = EnvironmentVariable("git_access_token");
var deployRemote        = EnvironmentVariable("git_deploy_remote");
var currentBranch       = isRunningOnAppVeyor ? BuildSystem.AppVeyor.Environment.Repository.Branch : GitBranchCurrent("./").FriendlyName;
var deployBranch        = string.Concat("publish/", currentBranch);

// Define directories.
var releaseDir          = Directory("./release");
var sourceDir           = releaseDir + Directory("repo");
var outputPath          = MakeAbsolute(Directory("./output"));
var rootPublishFolder   = MakeAbsolute(Directory("publish"));

// Variables


//////////////////////////////////////////////////////////////////////
// SETUP
//////////////////////////////////////////////////////////////////////


Setup(ctx =>
{
    // Executed BEFORE the first task.
    Information("Building branch {0} ({1})...", currentBranch, deployBranch);
});


//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    if(DirectoryExists(releaseDir))
    {
        CleanDirectory(releaseDir);
        DeleteDirectory(releaseDir, true);
    }
});

Task("GetSource")
    .IsDependentOn("Clean")
    .Does(() =>
    {
        GitHubClient github = new GitHubClient(new ProductHeaderValue("ReactiveUI"));
        if (!string.IsNullOrEmpty(accessToken))
        {
            github.Credentials = new Credentials(accessToken);
        }
        // The GitHub releases API returns Not Found if all are pre-release, so need workaround below
        //Release release = github.Repository.Release.GetLatest("reactiveui", "reactiveui").Result;
        Release release = github.Repository.Release.GetAll("reactiveui", "reactiveui").Result.First();
        FilePath releaseZip = DownloadFile(release.ZipballUrl);
        Unzip(releaseZip, releaseDir);

        // Need to rename the container directory in the zip file to something consistent
        var containerDir = GetDirectories(releaseDir.Path.FullPath + "/*").First(x => x.GetDirectoryName().StartsWith("reactiveui"));
        MoveDirectory(containerDir, sourceDir);
    });

Task("Build")
    .IsDependentOn("GetArtifacts")
    .Does(() =>
    {
        Wyam(new WyamSettings
        {
            Recipe = "Docs",
            Theme = "Samson",
            UpdatePackages = true
        });
    });

// Does not download artifacts (run Build or GetArtifacts target first)
Task("Preview")
    .Does(() =>
    {
        Wyam(new WyamSettings
        {
            Recipe = "Docs",
            Theme = "Samson",
            UpdatePackages = false,
            Preview = true,
            Watch = true
        });
    });

// Assumes Wyam source is local and at ../Wyam
Task("Debug")
    .Does(() =>
    {
        StartProcess("../Wyam/src/clients/Wyam/bin/Debug/wyam.exe",
            "-a \"../Wyam/src/**/bin/Debug/*.dll\" -r \"docs -i\" -t \"../Wyam/themes/Docs/Samson\" -p --attach");
    });

Task("Deploy")
    .WithCriteria(isRunningOnAppVeyor)
    .WithCriteria(!isPullRequest)
    .WithCriteria(!string.IsNullOrEmpty(accessToken))
    .WithCriteria(!string.IsNullOrEmpty(deployRemote))
    .WithCriteria(!string.IsNullOrEmpty(deployBranch))
    .IsDependentOn("Build")
    .Does(() =>
    {
        EnsureDirectoryExists(rootPublishFolder);
    });

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Build");

Task("GetArtifacts")
    .IsDependentOn("GetSource");

Task("AppVeyor")
    .IsDependentOn(isPullRequest ? "Build" : "Deploy");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

if (!StringComparer.OrdinalIgnoreCase.Equals(target, "Deploy"))
{
    RunTarget(target);
}
