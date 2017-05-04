using System;
using System.Linq;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Test;
using Cake.Common.Tools.XUnit;
using Cake.Core;
using Cake.Core.Annotations;
using Cake.Core.IO;
using Cake.Core.Tooling;

namespace ArchitectNow.Cake.Tools.DotNetCore.XUnit
{
	/// <summary>
	/// .NET Core project tester.
	/// </summary>
	public sealed class DotNetCoreXUnitTester : DotNetCoreTool<DotNetCoreTestSettings>
	{
		private readonly ICakeEnvironment _environment;

		/// <summary>
		/// Initializes a new instance of the <see cref="DotNetCoreXUnitTester" /> class.
		/// </summary>
		/// <param name="fileSystem">The file system.</param>
		/// <param name="environment">The environment.</param>
		/// <param name="processRunner">The process runner.</param>
		/// <param name="tools">The tool locator.</param>
		public DotNetCoreXUnitTester(
			IFileSystem fileSystem,
			ICakeEnvironment environment,
			IProcessRunner processRunner,
			IToolLocator tools) : base(fileSystem, environment, processRunner, tools)
		{
			_environment = environment;
		}

		/// <summary>
		/// Tests the project using the specified path with arguments and settings.
		/// </summary>
		/// <param name="project">The target project path.</param>
		/// <param name="settings">The settings.</param>
		/// <param name="xunitSettings">The settings.</param>
		public void Test(string project, DotNetCoreTestSettings settings, XUnitCoreSettings xunitSettings)
		{
			if (settings == null)
			{
				throw new ArgumentNullException(nameof(settings));
			}

			_environment.WorkingDirectory = System.IO.Path.GetDirectoryName(project);

			var builder = CreateArgumentBuilder(settings);

			var processArgumentBuilder = GetArguments(project, builder, xunitSettings);
			
			Run(settings, processArgumentBuilder);
		}

		private ProcessArgumentBuilder GetArguments(
			FilePath project,
			ProcessArgumentBuilder builder,
			XUnitCoreSettings settings)
		{

			// No shadow copy?
			if (!settings.ShadowCopy)
			{
				throw new CakeException("-noshadow is not supported in .netcoreapp");
			}

			// No app domain?
			if (settings.NoAppDomain)
			{
				throw new CakeException("-noappdomain is not supported in .netcoreapp");
			}

			builder.Append("xunit");

			// Generate NUnit Style XML report?
			if (settings.NUnitReport)
			{
				var reportFileName = new FilePath(project.GetDirectory().GetDirectoryName());
				var assemblyFilename = reportFileName.AppendExtension(".xml");
				var outputPath = settings.OutputDirectory.MakeAbsolute(_environment).GetFilePath(assemblyFilename);

				builder.Append("-nunit");
				builder.AppendQuoted(outputPath.FullPath);
			}

			// Generate HTML report?
			if (settings.HtmlReport)
			{
				var reportFileName = new FilePath(project.GetDirectory().GetDirectoryName());
				var assemblyFilename = reportFileName.AppendExtension(".html");
				var outputPath = settings.OutputDirectory.MakeAbsolute(_environment).GetFilePath(assemblyFilename);

				builder.Append("-html");
				builder.AppendQuoted(outputPath.FullPath);
			}

			if (settings.XmlReportV1)
			{
				throw new CakeException("-xmlv1 is not supported in .netcoreapp");
			}

			// Generate XML report?
			if (settings.XmlReport)
			{
				var reportFileName = new FilePath(project.GetDirectory().GetDirectoryName());
				var assemblyFilename = reportFileName.AppendExtension(".xml");
				var outputPath = settings.OutputDirectory.MakeAbsolute(_environment).GetFilePath(assemblyFilename);

				builder.Append("-xml");
				builder.AppendQuoted(outputPath.FullPath);
			}

			// parallelize test execution?
			if (settings.Parallelism != ParallelismOption.None)
			{
				builder.Append("-parallel " + settings.Parallelism.ToString().ToLowerInvariant());
			}

			// max thread count for collection parallelization
			if (settings.MaxThreads.HasValue)
			{
				if (settings.MaxThreads.Value == 0)
				{
					builder.Append("-maxthreads unlimited");
				}
				else
				{
					builder.Append("-maxthreads " + settings.MaxThreads.Value);
				}
			}

			foreach (var trait in settings.TraitsToInclude
				.SelectMany(pair => pair.Value.Select(v => new { Name = pair.Key, Value = v })))
			{
				builder.Append("-trait \"{0}={1}\"", trait.Name, trait.Value);
			}

			foreach (var trait in settings.TraitsToExclude
				.SelectMany(pair => pair.Value.Select(v => new { Name = pair.Key, Value = v })))
			{
				builder.Append("-notrait \"{0}={1}\"", trait.Name, trait.Value);
			}
			Console.Write(builder.RenderSafe());
			return builder;
		}
	}
}