// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    public class ProcessDumpUtility
    {
        private IProcessHelper processHelper;
        private IFileHelper fileHelper;
        private string dumpFileGuid;
        private Process procDump;
        private int processId;
        private string processName;

        public ProcessDumpUtility()
            : this(new ProcessHelper(), new FileHelper())
        {
        }

        public ProcessDumpUtility(IProcessHelper processHelper, IFileHelper fileHelper)
        {
            this.processHelper = processHelper;
            this.fileHelper = fileHelper;
            this.procDump = null;
        }

        public List<string> GetDumpFiles()
        {
            if (this.procDump == null)
            {
                return new List<string>();
            }

            this.procDump.WaitForExit();
            return this.GetCrashDumpFiles(this.GetDumpsDirectory(), this.processName, this.processId);
        }

        public void StartProcessDump(int processId, string processName, string dumpFilePostfix)
        {
            Action<object, string> errorRecievedCallback = (process, data) =>
            {
                // EqtTrace.Warning("ProcessDumpUtility: StartProcessDump: Problem starting procdump.");
            };

            Action<object> exitCallback = (process) =>
            {
                // callback(this.GetCrashDumpFiles(this.GetDumpsDirectory(), processName, processId));
            };

            this.dumpFileGuid = dumpFilePostfix;
            this.processId = processId;
            this.processName = processName;

            try
            {
                this.procDump = this.processHelper.LaunchProcess(
                                                this.GetProcDumpExecutable(),
                                                this.BuildProcDumpArgs(processId, processName),
                                                this.GetDumpsDirectory(),
                                                null,
                                                errorRecievedCallback,
                                                exitCallback) as Process;
            }
            catch (Exception e)
            {
                errorRecievedCallback(e, string.Empty);
            }
        }

        private string GetProcDumpExecutable()
        {
            return "D:\\procdump64.exe";
        }

        private string GetDumpsDirectory()
        {
            string testResults = Path.Combine(Directory.GetCurrentDirectory(), "TestResults");
            string dumps = Path.Combine(testResults, "dumps");

            if (!this.fileHelper.DirectoryExists(dumps))
            {
                this.fileHelper.CreateDirectory(dumps);
            }

            return dumps;
        }

        private string BuildProcDumpArgs(int processId, string processName)
        {
            return "-t -n 3 -ma " + processId + " " + processName + "_" + this.dumpFileGuid + ".dmp";
        }

        /// <summary>
        /// Gets the crash dump file from paath
        /// </summary>
        /// <param name="dumpPath">Path for Dump Folder</param>
        /// <param name="applicationName">Application Name</param>
        /// <param name="processId">Process Id</param>
        /// <returns>Latest Crash Dump File</returns>
        private List<string> GetCrashDumpFiles(string dumpPath, string applicationName, int processId)
        {
            string latestCrashDump = string.Empty;
            string searchPattern = string.Empty;
            if (!string.IsNullOrEmpty(applicationName))
            {
                // Dump file names are in format <applicationName>.<ProcessId>.dmp...
                // Dump file names are in format <applicationName>(1).<ProcessId>.dmp...
                searchPattern = applicationName + "_" + this.dumpFileGuid + "*";
            }

            if (!string.IsNullOrEmpty(dumpPath)
                    && this.fileHelper.DirectoryExists(dumpPath))
            {
                string[] dumpfiles = this.fileHelper.GetFiles(dumpPath, searchPattern, SearchOption.TopDirectoryOnly);

                if (dumpfiles.Length > 0)
                {
                    latestCrashDump = dumpfiles[0];
                    if (dumpfiles.Length > 1)
                    {
                        if (EqtTrace.IsWarningEnabled)
                        {
                            EqtTrace.Warning(string.Format(CultureInfo.InvariantCulture, "LocalCrashDumpUtilities: GetCrashDumpFile: Mulitple crash dump file with name '{0}' found.", searchPattern));
                        }

                        // Find the latest one...
                        // latestCrashDump = this.FindLatestFile(dumpfiles);
                    }

                    return new List<string>(dumpfiles);
                }
            }

            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Verbose(string.Format(CultureInfo.InvariantCulture, "LocalCrashDumpUtilities: GetCrashDumpFile: Latest crash dump file is: '{0}'.", latestCrashDump));
            }

            return null;
        }
    }
}
