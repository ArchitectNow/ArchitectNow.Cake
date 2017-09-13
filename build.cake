#tool nuget:?package=GitVersion.CommandLine&version=4.0.0-beta0012
#tool nuget:?package=vswhere

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target                  = Argument("target", "Default");
var configuration           = Argument("configuration", "Release");
var treatWarningsAsErrors   = Argument("treatWarningsAsErrors", "false");
var solutionPath            = MakeAbsolute(File(Argument("solutionPath", "./ArchitectNow.Cake.sln")));
var myGetApiKey             = EnvironmentVariable("myGetApiKey");
var nugetApiKey             = EnvironmentVariable("nugetApiKey");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

var artifacts               = MakeAbsolute(Directory(Argument("artifactPath", "./artifacts")));
var versionAssemblyInfo     = MakeAbsolute(File(Argument("versionAssemblyInfo", "VersionAssemblyInfo.cs")));

IEnumerable<FilePath> nugetProjectPaths     = null;
SolutionParserResult solution               = null;
GitVersion versionInfo                      = null;

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Setup(ctx => {
    if(!FileExists(solutionPath)) throw new Exception(string.Format("Solution file not found - {0}", solutionPath.ToString()));
    solution = ParseSolution(solutionPath.ToString());

    Information("[Setup] Using Solution '{0}'", solutionPath.ToString());

    if(DirectoryExists(artifacts)) 
    {
        DeleteDirectory(artifacts, true);
    }
    
    EnsureDirectoryExists(artifacts);
    
    var binDirs = GetDirectories(solutionPath.GetDirectory() +@"\src\**\bin");
    var objDirs = GetDirectories(solutionPath.GetDirectory() +@"\src\**\obj");
    DeleteDirectories(binDirs, true);
    DeleteDirectories(objDirs, true);
});

Task("Update-Version-Info")
    .IsDependentOn("Create-Version-Info")
    .Does(() => 
{
        versionInfo = GitVersion(new GitVersionSettings {
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFilePath = versionAssemblyInfo
        });

    if(versionInfo != null) {
        Information("Version: {0}", versionInfo.FullSemVer);
    } else {
        throw new Exception("Unable to determine version");
    }
});

Task("Create-Version-Info")
    .WithCriteria(() => !FileExists(versionAssemblyInfo))
    .Does(() =>
{
    Information("Creating version assembly info");
    CreateAssemblyInfo(versionAssemblyInfo, new AssemblyInfoSettings {
        Version = "0.0.0.0",
        FileVersion = "0.0.0.0",
        InformationalVersion = "",
    });
});

Task("DotNet-MsBuild-Restore")
    .IsDependentOn("Update-Version-Info")
    .Does(() => {

        MSBuild(solutionPath, c => c
            .SetConfiguration(configuration)
            .SetVerbosity(Verbosity.Minimal)
            .UseToolVersion(MSBuildToolVersion.VS2017)
            .WithTarget("Restore")
        );
});

Task("DotNet-MsBuild")
    .IsDependentOn("Restore")
    .Does(() => {

        MSBuild(solutionPath, c => c
            .SetConfiguration(configuration)
            .SetVerbosity(Verbosity.Minimal)
            .UseToolVersion(MSBuildToolVersion.VS2017)
            .WithProperty("TreatWarningsAsErrors", treatWarningsAsErrors)
            .WithTarget("Build")
        );

});

Task("DotNet-MsBuild-Pack")
    .IsDependentOn("Build")
    .Does(() => {

        MSBuild("src/ArchitectNow.Cake/ArchitectNow.Cake.csproj", c => c
            .SetConfiguration(configuration)
            .SetVerbosity(Verbosity.Normal)
            .UseToolVersion(MSBuildToolVersion.VS2017)
            .WithProperty("PackageVersion", versionInfo.NuGetVersionV2)
            .WithProperty("NoBuild", "true")
            .WithTarget("Pack")
    );
});

Task("DotNet-MsBuild-CopyToArtifacts")
    .IsDependentOn("DotNet-MsBuild-Pack")
    .Does(() => {

        EnsureDirectoryExists(artifacts);
        CopyFiles("src/ArchitectNow.Cake/bin/" +configuration +"/*.nupkg", artifacts);
});

Task("MyGet-Upload-Artifacts")
    .IsDependentOn("Package")    
    .Does(() =>
{
    var nugetFeed = "https://www.myget.org/F/architectnow/api/v2/package";
    
    foreach(var nupkg in GetFiles(artifacts + "/*.nupkg")) {
        Information("Pushing: " + nupkg);
        var settings = new NuGetPushSettings {
             Source = nugetFeed
        };
        if(!string.IsNullOrEmpty(myGetApiKey)){
            settings.ApiKey = myGetApiKey;
        }
        NuGetPush(nupkg, settings);
    }
});

Task("MyGet-Upload-Symbols")
    .IsDependentOn("MyGet-Upload-Artifacts")    
    .Does(() =>
{
    var nugetFeed = "https://www.myget.org/F/architectnow/symbols/api/v2/package";
    
    foreach(var nupkg in GetFiles(artifacts +"/*.symbols.nupkg")) {
        Information("Pushing: " + nupkg);
        /*
        NuGetPush(nupkg, new NuGetPushSettings {
             Source = nugetFeed
        });
        */
    }
});

Task("Nuget-Upload-Artifacts")
    .IsDependentOn("Package")    
    .Does(() =>
{
    var nugetFeed = "https://www.nuget.org/api/v2/package";

    foreach(var nupkg in GetFiles(artifacts +"/*.nupkg")) {
        Information("Pushing: " + nupkg);
        var settings = new NuGetPushSettings {
             Source = nugetFeed
        };
        if(!string.IsNullOrEmpty(nugetApiKey)){
            settings.ApiKey = nugetApiKey;
        }

        NuGetPush(nupkg, settings);        
    }
});

Task("MyGet")        
    // .IsDependentOn("Nuget-Upload-Artifacts")
    .IsDependentOn("MyGet-Upload-Artifacts")
    .IsDependentOn("MyGet-Upload-Symbols");

Task("Nuget")        
    .IsDependentOn("Nuget-Upload-Artifacts");
// ************************** //

Task("Restore")
    .IsDependentOn("DotNet-MsBuild-Restore");

Task("Build")
    .IsDependentOn("Restore")
    .IsDependentOn("DotNet-MsBuild");

Task("Package")
    .IsDependentOn("Build")
    .IsDependentOn("DotNet-MsBuild-CopyToArtifacts")
    .IsDependentOn("DotNet-MsBuild-Pack");

Task("CI")    
    .IsDependentOn("Default")
    .IsDependentOn("NuGet")
    .IsDependentOn("MyGet");
//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Restore")
    .IsDependentOn("Build")
    .IsDependentOn("Package");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);