﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    /// <summary>
    /// The blame logger.
    /// </summary>
    [FriendlyName(BlameLogger.FriendlyName)]
    [ExtensionUri(BlameLogger.ExtensionUri)]
    public class BlameLogger : ITestLoggerWithParameters
    {
        #region Constants

        /// <summary>
        /// Uri used to uniquely identify the Blame logger.
        /// </summary>
        public const string ExtensionUri = "logger://Microsoft/TestPlatform/Extensions/Blame/v1";

        /// <summary>
        /// Alternate user friendly string to uniquely identify the Blame logger.
        /// </summary>
        public const string FriendlyName = "Blame";

        /// <summary>
        /// The blame reader writer.
        /// </summary>
        private readonly IBlameReaderWriter blameReaderWriter;

        /// <summary>
        /// The output.
        /// </summary>
        private readonly IOutput output;

        /// <summary>
        /// The Environment
        /// </summary>
        private IEnvironment environment;

        /// <summary>
        /// Is Dump Enabled
        /// </summary>
        private bool isDumpEnabled;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="BlameLogger"/> class.
        /// </summary>
        public BlameLogger()
             : this(ConsoleOutput.Instance, new XmlReaderWriter(), new PlatformEnvironment())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlameLogger"/> class.
        /// Constructor added for testing purpose
        /// </summary>
        /// <param name="output">Output Instance</param>
        /// <param name="blameReaderWriter">BlameReaderWriter Instance</param>
        /// <param name="environment">Environment</param>
        protected BlameLogger(IOutput output, IBlameReaderWriter blameReaderWriter, IEnvironment environment)
        {
            this.output = output;
            this.blameReaderWriter = blameReaderWriter;
            this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        #endregion

        #region ITestLogger

        /// <summary>
        /// Initializes the Logger.
        /// </summary>
        /// <param name="events">Events that can be registered for.</param>
        /// <param name="testRunDictionary">Test Run Directory</param>
        public void Initialize(TestLoggerEvents events, string testRunDictionary)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            events.TestRunComplete += this.TestRunCompleteHandler;
        }

        /// <summary>
        /// Initializes the Logger.
        /// </summary>
        /// <param name="events">Events that can be registered for.</param>
        /// <param name="testRunDictionary">Test Run Directory</param>
        public void Initialize(TestLoggerEvents events, Dictionary<string, string> testRunDictionary)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            events.TestRunComplete += this.TestRunCompleteHandler;

            if (testRunDictionary.ContainsKey(Constants.DumpKey))
            {
                this.isDumpEnabled = true;
            }
        }

        /// <summary>
        /// Called when a test run is complete.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">TestRunCompleteEventArgs</param>
        private void TestRunCompleteHandler(object sender, TestRunCompleteEventArgs e)
        {
            if (sender == null)
            {
                throw new ArgumentNullException(nameof(sender));
            }

            ValidateArg.NotNull<object>(sender, "sender");
            ValidateArg.NotNull<TestRunCompleteEventArgs>(e, "e");

            if (!e.IsAborted)
            {
                return;
            }

            this.output.WriteLine(string.Empty, OutputLevel.Information);

            // Gets the faulty test cases if test aborted
            var testCaseNames = this.GetFaultyTestCaseNames(e);
            if (testCaseNames.Count() == 0)
            {
                return;
            }

            this.output.Error(false, Resources.Resources.AbortedTestRun);

            StringBuilder sb = new StringBuilder();
            foreach (var tcn in testCaseNames)
            {
                sb.Append(tcn).Append(Environment.NewLine);
            }

            this.output.Error(false, sb.ToString());

            if (this.isDumpEnabled)
            {
            }

            /*
            // Checks for operating system
            // If windows, prints the dump folder name if obtained
            if (this.environment.OperatingSystem.Equals(PlatformOperatingSystem.Windows))
            {
                if (this.isDumpEnabled)
                {
                    if (dumpList.Count > 0)
                    {
                        this.output.WriteLine(string.Empty, OutputLevel.Information);
                        this.output.WriteLine(Resources.Resources.LocalCrashDumpLink, OutputLevel.Information);
                        this.output.WriteLine("  " + dumpList.FirstOrDefault(), OutputLevel.Information);
                    }
                    else
                    {
                        this.output.WriteLine(string.Empty, OutputLevel.Information);
                        this.output.WriteLine(Resources.Resources.EnableLocalCrashDumpGuidance + "broken link", OutputLevel.Information);
                    }
                }
                else
                {
                    this.output.WriteLine(string.Empty, OutputLevel.Information);
                    this.output.WriteLine(Resources.Resources.EnableLocalCrashDumpGuidance + "broken link", OutputLevel.Information);
                }
            }*/
        }

        #endregion

        #region Faulty test case fetch

        /// <summary>
        /// Fetches faulty test case
        /// </summary>
        /// <param name="e">
        /// The TestRunCompleteEventArgs.
        /// </param>
        /// <returns>
        /// Faulty test cases name
        /// </returns>
        private IEnumerable<string> GetFaultyTestCaseNames(TestRunCompleteEventArgs e)
        {
            var faultyTestCaseNames = new List<string>();
            foreach (var attachmentSet in e.AttachmentSets)
            {
                if (attachmentSet.DisplayName.Equals(Constants.BlameDataCollectorName))
                {
                    var uriDataAttachment = attachmentSet.Attachments.LastOrDefault((attachment) => { return attachment.Uri.ToString().EndsWith(".xml"); });
                    if (uriDataAttachment != null)
                    {
                        var filepath = uriDataAttachment.Uri.LocalPath;

                        var testCaseList = this.blameReaderWriter.ReadTestSequence(filepath);
                        if (testCaseList.Count > 0)
                        {
                            var testcase = testCaseList.Last();
                            faultyTestCaseNames.Add(testcase.FullyQualifiedName);
                        }
                    }
                }
            }

            return faultyTestCaseNames;
        }

        #endregion
    }
}