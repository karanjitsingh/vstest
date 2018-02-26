// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    using Microsoft.TestPlatform.Extensions.BlameDataCollector;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    /// <summary>
    /// The blame logger tests.
    /// </summary>
    [TestClass]
    public class BlameLoggerTests
    {
        private Mock<ITestRunRequest> testRunRequest;
        private Mock<TestLoggerEvents> events;
        private Mock<IOutput> mockOutput;
        private Mock<IBlameReaderWriter> mockBlameReaderWriter;
        private BlameLogger blameLogger;
        private Mock<IEnvironment> mockEnvironment;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlameLoggerTests"/> class.
        /// </summary>
        public BlameLoggerTests()
        {
            // Mock for ITestRunRequest
            this.testRunRequest = new Mock<ITestRunRequest>();
            this.events = new Mock<TestLoggerEvents>();
            this.mockOutput = new Mock<IOutput>();
            this.mockBlameReaderWriter = new Mock<IBlameReaderWriter>();
            this.mockEnvironment = new Mock<IEnvironment>();
            this.blameLogger = new TestableBlameLogger(this.mockOutput.Object, this.mockBlameReaderWriter.Object, this.mockEnvironment.Object);
        }

        /// <summary>
        /// The initialize should throw exception if events is null.
        /// </summary>
        [TestMethod]
        public void InitializeShouldThrowExceptionIfEventsIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.blameLogger.Initialize(null, string.Empty);
            });
        }

        /// <summary>
        /// The test run complete handler should get faulty test run if test run aborted.
        /// </summary>
        [TestMethod]
        public void TestRunCompleteHandlerShouldGetFaultyTestRunIfTestRunAborted()
        {
            this.InitializeAndVerify(1);
        }

        /// <summary>
        /// The test run complete handler should get faulty test run if test run aborted for multiple test project.
        /// </summary>
        [TestMethod]
        public void TestRunCompleteHandlerShouldGetFaultyTestRunIfTestRunAbortedForMultipleProjects()
        {
            this.InitializeAndVerify(2);
        }

        /// <summary>
        /// The test run complete handler should not read file if test run not aborted.
        /// </summary>
        [TestMethod]
        public void TestRunCompleteHandlerShouldNotReadFileIfTestRunNotAborted()
        {
            // Initialize Blame Logger
            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            this.blameLogger.Initialize(loggerEvents, (string)null);

            // Setup and Raise event
            this.mockBlameReaderWriter.Setup(x => x.ReadTestSequence(It.IsAny<string>()));
            loggerEvents.CompleteTestRun(null, false, false, null, null, new TimeSpan(1, 0, 0, 0));

            // Verify Call
            this.mockBlameReaderWriter.Verify(x => x.ReadTestSequence(It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// The test run complete handler should return if uri attachment is null.
        /// </summary>
        [TestMethod]
        public void TestRunCompleteHandlerShouldReturnIfUriAttachmentIsNull()
        {
            // Initialize
            var attachmentSet = new AttachmentSet(new Uri("test://uri"), "Blame");
            var attachmentSetList = new List<AttachmentSet> { attachmentSet };

            // Initialize Blame Logger
            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            this.blameLogger.Initialize(loggerEvents, (string)null);

            // Setup and Raise event
            loggerEvents.CompleteTestRun(null, false, true, null, new Collection<AttachmentSet>(attachmentSetList), new TimeSpan(1, 0, 0, 0));

            // Verify Call
            this.mockBlameReaderWriter.Verify(x => x.ReadTestSequence(It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// The test run complete handler should print dump folder name if os is windows and dump is enabled
        /// </summary>
        [TestMethod]
        public void TestRunCompleteHandlerShouldPrintDumpsFolderIfDumpFileObtained()
        {
            // Initialize
            var attachmentSetList = new List<AttachmentSet>();
            attachmentSetList.Add(this.GetAttachmentSet());

            var param = new Dictionary<string, string>();
            param["dump"] = string.Empty;

            BlameLogger.AddFileToDumpList("C:\\dumps\\testhost.exe.1439.dmp");

            var testCaseList = this.GetTestCaseList();

            // Setup
            this.mockEnvironment.Setup(x => x.OperatingSystem).Returns(PlatformOperatingSystem.Windows);
            this.mockBlameReaderWriter.Setup(x => x.ReadTestSequence(It.IsAny<string>())).Returns(testCaseList);

            // Initialize Blame Logger
            this.blameLogger.Initialize(this.events.Object, param);

            // Raise event
            this.testRunRequest.Raise(
                m => m.OnRunCompletion += null,
                new TestRunCompleteEventArgs(stats: null, isCanceled: false, isAborted: true, error: null, attachmentSets: new Collection<AttachmentSet>(attachmentSetList), elapsedTime: new TimeSpan(1, 0, 0, 0)));

            // Verify
            this.mockOutput.Verify(x => x.WriteLine("  " + "C:\\dumps\\testhost.exe.1439.dmp", OutputLevel.Information), Times.Once);
        }

        /// <summary>
        /// The test run complete handler should print Guidance Link If no dump file obtained
        /// </summary>
        [TestMethod]
        public void TestRunCompleteHandlerShouldPrintGuidanceLinkIfNoDumpFileObtained()
        {
            // Initialize
            var attachmentSetList = new List<AttachmentSet>();
            attachmentSetList.Add(this.GetAttachmentSet());

            var testCaseList = this.GetTestCaseList();
            var param = new Dictionary<string, string>();
            param["dump"] = string.Empty;

            // Setup
            this.mockEnvironment.Setup(x => x.OperatingSystem).Returns(PlatformOperatingSystem.Windows);
            this.mockBlameReaderWriter.Setup(x => x.ReadTestSequence(It.IsAny<string>())).Returns(testCaseList);

            // Initialize Blame Logger
            this.blameLogger.Initialize(this.events.Object, param);

            // Raise event
            this.testRunRequest.Raise(
                m => m.OnRunCompletion += null,
                new TestRunCompleteEventArgs(stats: null, isCanceled: false, isAborted: true, error: null, attachmentSets: new Collection<AttachmentSet>(attachmentSetList), elapsedTime: new TimeSpan(1, 0, 0, 0)));

            // Verify
            this.mockOutput.Verify(x => x.WriteLine(Resources.Resources.EnableLocalCrashDumpGuidance + LocalCrashDumpUtilities.EnableLocalCrashDumpForwardLink, OutputLevel.Information), Times.Once);
        }

        [TestCleanup]
        public void CleanUp()
        {
            BlameLogger.ClearDumpList();
        }

        private List<TestCase> GetTestCaseList()
        {
            return new List<TestCase>
                    {
                        new TestCase("ABC.UnitTestMethod1", new Uri("test://uri"), "C://test/filepath"),
                        new TestCase("ABC.UnitTestMethod2", new Uri("test://uri"), "C://test/filepath")
                    };
        }

        private AttachmentSet GetAttachmentSet()
        {
            var attachmentSet = new AttachmentSet(new Uri("test://uri"), "Blame");
            var uriDataAttachment = new UriDataAttachment(new Uri("C:/folder1/sequence.xml"), "description");
            attachmentSet.Attachments.Add(uriDataAttachment);

            return attachmentSet;
        }

        private void InitializeAndVerify(int count)
        {
            // Initialize
            var attachmentSetList = new List<AttachmentSet>();

            for (int i = 0; i < count; i++)
            {
                attachmentSetList.Add(this.GetAttachmentSet());
            }

            // Initialize Blame Logger
            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            this.blameLogger.Initialize(loggerEvents, (string)null);

            var testCaseList =
                    new List<TestCase>
                        {
                        new TestCase("ABC.UnitTestMethod1", new Uri("test://uri"), "C://test/filepath"),
                        new TestCase("ABC.UnitTestMethod2", new Uri("test://uri"), "C://test/filepath")
                        };

            // Setup and Raise event
            this.mockBlameReaderWriter.Setup(x => x.ReadTestSequence(It.IsAny<string>())).Returns(testCaseList);
            loggerEvents.CompleteTestRun(null, false, true, null, new Collection<AttachmentSet>(attachmentSetList), new TimeSpan(1, 0, 0, 0));

            // Verify Call
            this.mockBlameReaderWriter.Verify(x => x.ReadTestSequence(It.IsAny<string>()), Times.Exactly(count));
        }

        /// <summary>
        /// The testable blame logger.
        /// </summary>
        internal class TestableBlameLogger : BlameLogger
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="TestableBlameLogger"/> class.
            /// </summary>
            /// <param name="output">
            /// The output.
            /// </param>
            /// <param name="blameReaderWriter">
            /// The blame Reader Writer.
            /// </param>
            /// <param name="environment">
            /// Environment Helper
            /// </param>
            internal TestableBlameLogger(IOutput output, IBlameReaderWriter blameReaderWriter, IEnvironment environment)
                : base(output, blameReaderWriter, environment)
            {
            }
        }
    }
}
