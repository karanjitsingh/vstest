﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    public class ProcessDumpUtility : IProcessDumpUtility
    {
        private IProcessHelper processHelper;
        private IFileHelper fileHelper;
        private IEnvironment environment;
        private Process procDumpProcess;
        private string testResultsDirectory;
        private string dumpFileName;

        public ProcessDumpUtility()
            : this(new ProcessHelper(), new FileHelper(), new PlatformEnvironment())
        {
        }

        public ProcessDumpUtility(IProcessHelper processHelper, IFileHelper fileHelper, IEnvironment environment)
        {
            this.processHelper = processHelper;
            this.fileHelper = fileHelper;
            this.procDumpProcess = null;
            this.environment = environment;
        }

        /// <inheritdoc/>
        public string GetDumpFile()
        {
            if (this.procDumpProcess == null)
            {
                return string.Empty;
            }

            this.processHelper.WaitForProcessExit(this.procDumpProcess);

            var dumpFile = Path.Combine(this.testResultsDirectory, this.dumpFileName);
            if (this.fileHelper.Exists(dumpFile))
            {
                return dumpFile;
            }

            if (EqtTrace.IsErrorEnabled)
            {
                EqtTrace.Error("ProcessDumpUtility.GetCrashDumpFile: No dump file generated.");
            }

            return string.Empty;
        }

        /// <inheritdoc/>
        public void StartProcessDump(int processId, string dumpFileGuid, string testResultsDirectory)
        {
            this.dumpFileName = $"{this.processHelper.GetProcessName(processId)}_{processId}_{dumpFileGuid}.dmp";
            this.testResultsDirectory = testResultsDirectory;

            this.procDumpProcess = this.processHelper.LaunchProcess(
                                            this.GetProcDumpExecutable(),
                                            ProcessDumpUtility.BuildProcDumpArgs(processId, this.dumpFileName),
                                            testResultsDirectory,
                                            null,
                                            null,
                                            null) as Process;
        }

        /// <summary>
        /// Arguments for procdump.exe
        /// </summary>
        /// <param name="processId">
        /// Process Id
        /// </param>
        /// <param name="filename">
        /// Filename for dump file
        /// </param>
        /// <returns>Arguments</returns>
        private static string BuildProcDumpArgs(int processId, string filename)
        {
            return "-t -g -ma " + processId + " " + filename;
        }

        /// <summary>
        /// Get procdump executable path
        /// </summary>
        /// <returns>procdump executable path</returns>
        private string GetProcDumpExecutable()
        {
            var procdumpPath = Environment.GetEnvironmentVariable("PROCDUMP_PATH");
            if (!string.IsNullOrEmpty(procdumpPath))
            {
                string filename = string.Empty;

                if (this.environment.Architecture == PlatformArchitecture.X64)
                {
                    filename = "procdump64.exe";
                }
                else if (this.environment.Architecture == PlatformArchitecture.X86)
                {
                    filename = "procdump.exe";
                }

                var procDumpExe = Path.Combine(procdumpPath, filename);

                if (EqtTrace.IsVerboseEnabled)
                {
                    EqtTrace.Verbose("Using procdump at: {0}", procDumpExe);
                }

                return procDumpExe;
            }
            else
            {
                throw new Exception(Resources.Resources.ProcDumpEnvVarEmpty);
            }
        }
    }
}
