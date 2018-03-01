﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.console.UnitTests.Processors
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using vstest.console.UnitTests.TestDoubles;
    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;
    using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Moq;
    using Microsoft.VisualStudio.TestPlatform.CommandLine;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.TestPlatformHelpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Client;
    using Microsoft.VisualStudio.TestPlatform.CommandLineUtilities;
    using System.Runtime.Versioning;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Publisher;

    [TestClass]
    public class EnableBlameArgumentProcessorTests
    {
        private TestableRunSettingsProvider settingsProvider;
        private const string DefaultRunSettings = "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors ></DataCollectors>\r\n  </DataCollectionRunSettings>\r\n  <RunConfiguration><ResultsDirectory>C:\\dir\\TestResults</ResultsDirectory></RunConfiguration>\r\n  </RunSettings>";
        private Mock<ITestHostLauncher> mockTestHostLauncher;
        private readonly Mock<IFileHelper> mockFileHelper;
        private Mock<ITestPlatformEventSource> mockTestPlatformEventSource;
        private Mock<IAssemblyMetadataProvider> mockAssemblyMetadataProvider;
        private Task<IMetricsPublisher> mockMetricsPublisherTask;
        private Mock<IMetricsPublisher> mockMetricsPublisher;
        private string dummyTestFilePath = "DummyTest.dll";
        private InferHelper inferHelper;

        public EnableBlameArgumentProcessorTests()
        {
            this.settingsProvider = new TestableRunSettingsProvider();
            this.mockTestHostLauncher = new Mock<ITestHostLauncher>();
            this.mockFileHelper = new Mock<IFileHelper>();
            this.mockFileHelper.Setup(fh => fh.Exists(this.dummyTestFilePath)).Returns(true);
            this.mockFileHelper.Setup(x => x.GetCurrentDirectory()).Returns("");
            this.mockMetricsPublisher = new Mock<IMetricsPublisher>();
            this.mockMetricsPublisherTask = Task.FromResult(this.mockMetricsPublisher.Object);
            this.mockTestPlatformEventSource = new Mock<ITestPlatformEventSource>();
            this.mockAssemblyMetadataProvider = new Mock<IAssemblyMetadataProvider>();
            this.mockAssemblyMetadataProvider.Setup(x => x.GetArchitecture(It.IsAny<string>())).Returns(Architecture.X64);
            this.mockAssemblyMetadataProvider.Setup(x => x.GetFrameWork(It.IsAny<string>())).Returns(new FrameworkName(Constants.DotNetFramework40));
            this.inferHelper = new InferHelper(this.mockAssemblyMetadataProvider.Object);

            CollectArgumentExecutor.EnabledDataCollectors.Clear();
        }
    
        [TestCleanup]
        public void CleanUp()
        {
            CommandLineOptions.Instance.Reset();
        }

        [TestMethod]
        public void GetMetadataShouldReturnEnableBlameArgumentProcessorCapabilities()
        {
            var processor = new EnableBlameArgumentProcessor();
            Assert.IsTrue(processor.Metadata.Value is EnableBlameArgumentProcessorCapabilities);
        }

        [TestMethod]
        public void GetExecuterShouldReturnEnableBlameArgumentProcessorCapabilities()
        {
            var processor = new EnableBlameArgumentProcessor();
            Assert.IsTrue(processor.Executor.Value is EnableBlameArgumentExecutor);
        }

        [TestMethod]
        public void CapabilitiesShouldReturnAppropriateProperties()
        {
            var capabilities = new EnableBlameArgumentProcessorCapabilities();

            Assert.AreEqual("/Blame", capabilities.CommandName);
            Assert.AreEqual(true, capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.Normal, capabilities.Priority);
            Assert.AreEqual(HelpContentPriority.EnableDiagArgumentProcessorHelpPriority, capabilities.HelpPriority);
            Assert.AreEqual(CommandLineResources.EnableBlameUsage, capabilities.HelpContentResourceName);

            Assert.AreEqual(false, capabilities.AllowMultiple);
            Assert.AreEqual(false, capabilities.AlwaysExecute);
            Assert.AreEqual(false, capabilities.IsSpecialCommand);
        }

        [TestMethod]
        public void InitializeShouldCreateEntryForBlameInRunSettingsIfNotAlreadyPresent()
        {
            SetSettingsProviderRunSettings();

            var executor = GetExecutor(new Mock<ITestRequestManager>().Object);
            executor.Initialize(string.Empty);

            Assert.IsNotNull(this.settingsProvider.ActiveRunSettings);
            Assert.AreEqual("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors>\r\n      <DataCollector friendlyName=\"blame\" enabled=\"True\">\r\n        <Configuration>\r\n          <ResultsDirectory>C:\\dir\\TestResults</ResultsDirectory>\r\n        </Configuration>\r\n      </DataCollector>\r\n    </DataCollectors>\r\n  </DataCollectionRunSettings>\r\n  <RunConfiguration>\r\n    <ResultsDirectory>C:\\dir\\TestResults</ResultsDirectory>\r\n  </RunConfiguration>\r\n  <LoggerRunSettings>\r\n    <Loggers>\r\n      <Logger friendlyName=\"blame\" enabled=\"True\" />\r\n    </Loggers>\r\n  </LoggerRunSettings>\r\n</RunSettings>", this.settingsProvider.ActiveRunSettings.SettingsXml);
        }
        
        [TestMethod]
        public void ExecutorExecuteForNoSourcesShouldThrowCommandLineException()
        {
            CommandLineOptions.Instance.Reset();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, new TestPlatform(), TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var executor = GetExecutor(testRequestManager);
            Assert.ThrowsException<CommandLineException>(() => executor.Execute());
        }

        [TestMethod]
        public void ExecutorExecuteShouldReturnSuccessWithoutExecutionInDesignMode()
        {
            var runSettingsProvider = new TestableRunSettingsProvider();

            CommandLineOptions.Instance.Reset();
            CommandLineOptions.Instance.IsDesignMode = true;
            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, new TestPlatform(), TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var executor = GetExecutor(testRequestManager);

            Assert.AreEqual(ArgumentProcessorResult.Success, executor.Execute());
        }

        [TestMethod]
        public void ExecutorExecuteShouldThrowOtherExceptions()
        {

            SetSettingsProviderRunSettings();

            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockTestRunRequest = new Mock<ITestRunRequest>();

            mockTestRunRequest.Setup(tr => tr.ExecuteAsync()).Throws(new Exception("DummyException"));
            mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>())).Returns(mockTestRunRequest.Object);

            this.ResetAndAddSourceToCommandLineOptions();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var executor = GetExecutor(testRequestManager);
            executor.Initialize(string.Empty);

            Assert.ThrowsException<Exception>(() => executor.Execute());

        }

        [TestMethod]
        public void ExecutorExecuteShouldCatchSettingsExceptionAndReturnFail()
        {
            SetSettingsProviderRunSettings();

            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockTestRunRequest = new Mock<ITestRunRequest>();

            mockTestRunRequest.Setup(tr => tr.ExecuteAsync()).Throws(new SettingsException("DummySettingsException"));
            mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>())).Returns(mockTestRunRequest.Object);

            this.ResetAndAddSourceToCommandLineOptions();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var executor = GetExecutor(testRequestManager);

            ArgumentProcessorResult argumentProcessorResult = executor.Execute();
            Assert.AreEqual(ArgumentProcessorResult.Fail, argumentProcessorResult);
        }

        [TestMethod]
        public void ExecutorExecuteShouldCatchTestPlatformExceptionAndReturnFail()
        {
            SetSettingsProviderRunSettings();

            var mockTestPlatform = new Mock<ITestPlatform>();
            var mockTestRunRequest = new Mock<ITestRunRequest>();

            mockTestRunRequest.Setup(tr => tr.ExecuteAsync()).Throws(new TestPlatformException("DummyTestPlatformException"));
            mockTestPlatform.Setup(tp => tp.CreateTestRunRequest(It.IsAny<IRequestData>(), It.IsAny<TestRunCriteria>())).Returns(mockTestRunRequest.Object);

            this.ResetAndAddSourceToCommandLineOptions();

            var testRequestManager = new TestRequestManager(CommandLineOptions.Instance, mockTestPlatform.Object, TestRunResultAggregator.Instance, this.mockTestPlatformEventSource.Object, this.inferHelper, this.mockMetricsPublisherTask);
            var executor = GetExecutor(testRequestManager);

            ArgumentProcessorResult argumentProcessorResult = executor.Execute();
            Assert.AreEqual(ArgumentProcessorResult.Fail, argumentProcessorResult);
        }

        private void SetSettingsProviderRunSettings()
        {
            var runsettingsString = string.Format(DefaultRunSettings, "");
            var runsettings = new RunSettings();
            runsettings.LoadSettingsXml(DefaultRunSettings);
            this.settingsProvider.SetActiveRunSettings(runsettings);
        }

        private EnableBlameArgumentExecutor GetExecutor(ITestRequestManager testRequestManager)
        {
            var executor = new EnableBlameArgumentExecutor(
                this.settingsProvider, testRequestManager, CommandLineOptions.Instance);
            return executor;
        }

        private void ResetAndAddSourceToCommandLineOptions()
        {
            CommandLineOptions.Instance.Reset();

            CommandLineOptions.Instance.FileHelper = this.mockFileHelper.Object;
            CommandLineOptions.Instance.AddSource(this.dummyTestFilePath);
        }
    }
}
