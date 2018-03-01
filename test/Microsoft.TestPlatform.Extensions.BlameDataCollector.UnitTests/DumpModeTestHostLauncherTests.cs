// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector.UnitTests
{
    /* using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Microsoft.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    /// <summary>
    /// The blame dump mode test host launcher tests.
    /// </summary>
    [TestClass]
    public class DumpModeTestHostLauncherTests
    {
        private Mock<IProcessHelper> mockProcessHelper;
        private TestableCrashDumpUtilities crashDumpUtilities;
        private Mock<IEnvironment> mockEnvironment;
        private Mock<IFileHelper> mockFileHelper;
        private Mock<IBlameDumpFolder> mockBlameDumpFolderGetter;
        private DumpModeTestHostLauncher dumpModeTestHostLauncher;
        private string errorMessage;
        private int exitCode;

        public DumpModeTestHostLauncherTests()
        {
            this.mockProcessHelper = new Mock<IProcessHelper>();
            this.mockFileHelper = new Mock<IFileHelper>();
            this.mockBlameDumpFolderGetter = new Mock<IBlameDumpFolder>();
            this.mockEnvironment = new Mock<IEnvironment>();
        }

        [TestMethod]
        public void LaunchTestHostShouldThrowExceptionIfTestHostStartInfIsNull()
        {
            this.dumpModeTestHostLauncher = this.GetDumpModeTestHostLauncher();
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.dumpModeTestHostLauncher.LaunchTestHost(null);
            });
        }

        [TestMethod]
        public void LaunchTestHostShouldReturnTestHostProcessId()
        {
            this.mockProcessHelper.Setup(
                ph =>
                    ph.LaunchProcess(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<IDictionary<string, string>>(),
                        It.IsAny<Action<object, string>>(),
                        It.IsAny<Action<object>>())).Returns(Process.GetCurrentProcess());
            this.dumpModeTestHostLauncher = this.GetDumpModeTestHostLauncher();

            int processId = this.dumpModeTestHostLauncher.LaunchTestHost(this.GetTestProcessStartInfo());

            Assert.AreEqual(Process.GetCurrentProcess().Id, processId);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        public void ProcessExitedButNoErrorMessageIfNoDataWritten(int exitCode)
        {
            this.dumpModeTestHostLauncher = this.GetDumpModeTestHostLauncher();
            this.ExitCallBackTestHelper(exitCode);

            this.dumpModeTestHostLauncher.LaunchTestHost(this.GetTestProcessStartInfo());

            Assert.AreEqual(this.errorMessage, string.Empty);
            Assert.AreEqual(this.exitCode, exitCode);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void ErrorReceivedCallbackShouldNotLogNullOrEmptyData(string errorData)
        {
            this.dumpModeTestHostLauncher = this.GetDumpModeTestHostLauncher();
            this.ErrorCallBackTestHelper(errorData, -1);

            this.dumpModeTestHostLauncher.LaunchTestHost(this.GetTestProcessStartInfo());

            Assert.AreEqual(this.errorMessage, string.Empty);
        }

        [TestMethod]
        [DataRow(-1)]
        public void OnHostExitedShouldGetCrashDumpFileIfPlatformOSIsWindows(int exitCode)
        {
            this.crashDumpUtilities = new TestableCrashDumpUtilities(@"C:\dumpfile.mp", this.mockFileHelper.Object);
            this.mockEnvironment.Setup(x => x.OperatingSystem).Returns(PlatformOperatingSystem.Windows);
            this.mockFileHelper.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);
            string dumpFolder;
            this.mockBlameDumpFolderGetter.Setup(x => x.GetCrashDumpFolderPath(It.IsAny<string>(), out dumpFolder)).Returns(true);

            this.dumpModeTestHostLauncher = this.GetDumpModeTestHostLauncher();
            this.ExitCallBackTestHelper(exitCode);

            this.dumpModeTestHostLauncher.LaunchTestHost(this.GetTestProcessStartInfo());

            Assert.AreEqual(1, BlameLogger.GetDumpListCount());
        }

        [TestMethod]
        [DataRow(-1)]
        public void OnHostExitedShouldNotGetCrashDumpFileIfPlatformOSIsNotWindows(int exitCode)
        {
            this.mockEnvironment.Setup(x => x.OperatingSystem).Returns(PlatformOperatingSystem.Unix);
            this.mockFileHelper.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);

            this.dumpModeTestHostLauncher = this.GetDumpModeTestHostLauncher();
            this.ExitCallBackTestHelper(exitCode);

            this.dumpModeTestHostLauncher.LaunchTestHost(this.GetTestProcessStartInfo());

            Assert.AreEqual(0, BlameLogger.GetDumpListCount());
        }

        [TestMethod]
        [DataRow("", -1)]
        [DataRow(null, -1)]
        public void NullOrEmptyFileShouldNotBeAddedToDumpList(string filename, int exitCode)
        {
            this.mockEnvironment.Setup(x => x.OperatingSystem).Returns(PlatformOperatingSystem.Unix);
            this.mockFileHelper.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);

            this.crashDumpUtilities = new TestableCrashDumpUtilities(filename, this.mockFileHelper.Object);
            this.dumpModeTestHostLauncher = this.GetDumpModeTestHostLauncher();
            this.ExitCallBackTestHelper(exitCode);

            this.dumpModeTestHostLauncher.LaunchTestHost(this.GetTestProcessStartInfo());

            Assert.AreEqual(0, BlameLogger.GetDumpListCount());
        }

        [TestCleanup]
        public void CleanUp()
        {
            BlameLogger.ClearDumpList();
        }

        private TestableDumpModeTestHostLauncher GetDumpModeTestHostLauncher()
        {
            var launcher = new TestableDumpModeTestHostLauncher(
                this.mockProcessHelper.Object,
                this.mockEnvironment.Object,
                this.mockFileHelper.Object);

            return launcher;
        }

        private TestProcessStartInfo GetTestProcessStartInfo()
        {
            TestProcessStartInfo processInfo = new TestProcessStartInfo();
            processInfo.FileName = "C:\\Documents\\dotnet.exe";
            return processInfo;
        }

        private void DumpModeTestHostLauncherHostExited(object sender, HostProviderEventArgs e)
        {
            if (e.ErrroCode != 0)
            {
                this.errorMessage = e.Data.TrimEnd(Environment.NewLine.ToCharArray());
            }
        }

        private void TestabledumpModeTestHostLauncherHostExited(object sender, HostProviderEventArgs e)
        {
            this.errorMessage = e.Data.TrimEnd(Environment.NewLine.ToCharArray());
            this.exitCode = e.ErrroCode;
        }

         private void ErrorCallBackTestHelper(string errorMessage, int exitCode)
        {
            this.dumpModeTestHostLauncher.HostExited += this.DumpModeTestHostLauncherHostExited;

            this.mockProcessHelper.Setup(
                    ph =>
                        ph.LaunchProcess(
                            It.IsAny<string>(),
                            It.IsAny<string>(),
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, string>>(),
                            It.IsAny<Action<object, string>>(),
                            It.IsAny<Action<object>>()))
                .Callback<string, string, string, IDictionary<string, string>, Action<object, string>, Action<object>>(
                    (var1, var2, var3, dictionary, errorCallback, exitCallback) =>
                    {
                        var process = Process.GetCurrentProcess();

                        errorCallback(process, errorMessage);
                        exitCallback(process);
                    }).Returns(Process.GetCurrentProcess());

            this.mockProcessHelper.Setup(ph => ph.TryGetExitCode(It.IsAny<object>(), out exitCode)).Returns(true);
        }

        private void ExitCallBackTestHelper(int exitCode)
        {
            this.dumpModeTestHostLauncher.HostExited += this.TestabledumpModeTestHostLauncherHostExited;

            this.mockProcessHelper.Setup(
                    ph =>
                        ph.LaunchProcess(
                            It.IsAny<string>(),
                            It.IsAny<string>(),
                            It.IsAny<string>(),
                            It.IsAny<IDictionary<string, string>>(),
                            It.IsAny<Action<object, string>>(),
                            It.IsAny<Action<object>>()))
                .Callback<string, string, string, IDictionary<string, string>, Action<object, string>, Action<object>>(
                    (var1, var2, var3, dictionary, errorCallback, exitCallback) =>
                    {
                        var process = Process.GetCurrentProcess();
                        exitCallback(process);
                    }).Returns(Process.GetCurrentProcess());

            this.mockProcessHelper.Setup(ph => ph.TryGetExitCode(It.IsAny<object>(), out exitCode)).Returns(true);
        }

        /// <summary>
        /// The testable blame mode test host launcher.
        /// </summary>
        private class TestableDumpModeTestHostLauncher : DumpModeTestHostLauncher
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="TestableDumpModeTestHostLauncher"/> class.
            /// </summary>
            /// <param name="processHelper">Process Helper</param>
            /// <param name="crashDumpUtilities">Crash dump Utilities</param>
            /// <param name="environment">Environment</param>
            /// <param name="fileHelper">File Helper</param>
            /// <param name="processDumpUtility">Process dump utility</param>
            public TestableDumpModeTestHostLauncher(
                IProcessHelper processHelper,
                IEnvironment environment,
                IFileHelper fileHelper)
                : base(processHelper, environment, fileHelper)
            {
                this.ErrorLength = 22;
            }
        }

        /// <summary>
        /// The testable crash dump utilities.
        /// </summary>
        private class TestableCrashDumpUtilities : LocalCrashDumpUtilities
        {
            private string filename;

            /// <summary>
            /// Initializes a new instance of the <see cref="TestableCrashDumpUtilities"/> class.
            /// </summary>
            /// <param name="fileHelper">File Helper</param>
            /// <param name="filename">filename</param>
            public TestableCrashDumpUtilities(
                string filename,
                IFileHelper fileHelper)
                : base(fileHelper)
            {
                this.filename = filename;
            }

            public override string GetCrashDumpFile(string dumpPath, string applicationName, int processId)
            {
                return this.filename;
            }
        }
    } */
}