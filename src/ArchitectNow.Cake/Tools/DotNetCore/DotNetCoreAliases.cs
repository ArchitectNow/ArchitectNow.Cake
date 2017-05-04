using System;
using ArchitectNow.Cake.Tools.DotNetCore.XUnit;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Test;
using Cake.Core;
using Cake.Core.Annotations;

namespace ArchitectNow.Cake.Tools.DotNetCore
{
	public static class DotNetCoreAliases
	{
		
		/// <summary>
		/// Test project with path.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="project">The project path.</param>
		/// <param name="xunitSettings">The xunitSettings.</param>
		/// <example>
		/// <code>
		///     DotNetCoreTest("./src/Project/Project.csproj");
		/// </code>
		/// </example>
		[CakeMethodAlias]
		[CakeAliasCategory("Test")]
		[CakeNamespaceImport("ArchitectNow.Cake.Tools.DotNetCore.XUnit")]
		public static void DotNetCoreTest(this ICakeContext context, string project, XUnitCoreSettings xunitSettings)
		{
			context.DotNetCoreTest(project, new DotNetCoreTestSettings(), xunitSettings);
		}

		/// <summary>
		/// Test project with path.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="project">The project path.</param>
		/// <param name="settings">DotNetCore Test Settings</param>
		/// <param name="xunitSettings">The xunitSettings.</param>
		/// <example>
		/// <code>
		///     DotNetCoreTest("./src/Project/Project.csproj");
		/// </code>
		/// </example>
		[CakeMethodAlias]
		[CakeAliasCategory("Test")]
		[CakeNamespaceImport("ArchitectNow.Cake.Tools.DotNetCore.XUnit")]
		public static void DotNetCoreTest(this ICakeContext context, string project, DotNetCoreTestSettings settings, XUnitCoreSettings xunitSettings)
		{
			if (context == null)
			{
				throw new ArgumentNullException(nameof(context));
			}

			if (xunitSettings == null)
			{
				xunitSettings = new XUnitCoreSettings();
			}

			var tester = new DotNetCoreXUnitTester(context.FileSystem, context.Environment, context.ProcessRunner, context.Tools);
			tester.Test(project, settings, xunitSettings);
		}
	}
}
