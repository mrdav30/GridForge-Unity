using GridForge;
using GridForge.Utility;
using NUnit.Framework;
using SwiftCollections;
using SwiftCollections.Diagnostics;
using System;
using UnityEngine;
using UnityEngine.TestTools;

namespace GridForge.Unity.Tests.EditMode
{
    public sealed class GridForgeUnityLoggerEditModeTests
    {
        private Action<DiagnosticLevel, string, string> _originalLogHandler;
        private Func<DiagnosticLevel, string, string, string> _originalFormatter;
        private DiagnosticLevel _originalMinimumLevel;
        private string _originalLogFilePath;
        private bool _originalDebugLogging;

        [SetUp]
        public void SetUp()
        {
            _originalLogHandler = GridForgeLogger.LogHandler;
            _originalFormatter = GridForgeLogger.CustomFormatter;
            _originalMinimumLevel = GridForgeLogger.MinimumLevel;
            _originalLogFilePath = GridForgeLogger.LogFilePath;
            _originalDebugLogging = GridForgeLogger.EnableDebugLogging;

            LogAssert.ignoreFailingMessages = false;
        }

        [TearDown]
        public void TearDown()
        {
            GridForgeLogger.LogHandler = _originalLogHandler;
            GridForgeLogger.CustomFormatter = _originalFormatter;
            GridForgeLogger.MinimumLevel = _originalMinimumLevel;
            GridForgeLogger.LogFilePath = _originalLogFilePath;
            GridForgeLogger.EnableDebugLogging = _originalDebugLogging;
            LogAssert.ignoreFailingMessages = false;
        }

        [Test]
        public void ComponentDoesNotInstallLoggingUntilExplicitlyEnabled()
        {
            SwiftList<string> capturedLogs = new();
            Action<DiagnosticLevel, string, string> previousHandler =
                (level, message, source) => capturedLogs.Add($"{level}|{source}|{message}");
            GridForgeLogger.LogHandler = previousHandler;
            GridForgeLogger.MinimumLevel = DiagnosticLevel.Warning;

            GameObject owner = new("GridForge Unity logger test");

            try
            {
                GridForgeUnityLogger logger = owner.AddComponent<GridForgeUnityLogger>();

                Assert.IsFalse(logger.IsLoggingEnabled);
                Assert.AreSame(previousHandler, GridForgeLogger.LogHandler);

                GridForgeLogger.Channel.Write(DiagnosticLevel.Warning, "not forwarded", "ExplicitOptIn");

                Assert.AreEqual(1, capturedLogs.Count);
                Assert.AreEqual("Warning|ExplicitOptIn|not forwarded", capturedLogs[0]);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void EnableLoggingRoutesGridForgeWarningsToUnityWarnings()
        {
            GameObject owner = new("GridForge Unity logger test");

            try
            {
                GridForgeUnityLogger logger = owner.AddComponent<GridForgeUnityLogger>();
                logger.MinimumLevel = DiagnosticLevel.Warning;

                Assert.IsTrue(logger.EnableLogging());

                LogAssert.Expect(LogType.Warning, "[GridForge] [WARN] [UnityLoggerTest] bridge warning");
                GridForgeLogger.Channel.Write(DiagnosticLevel.Warning, "bridge warning", "UnityLoggerTest");
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void MinimumLevelFiltersLowerSeverityMessages()
        {
            GameObject owner = new("GridForge Unity logger test");

            try
            {
                GridForgeUnityLogger logger = owner.AddComponent<GridForgeUnityLogger>();
                logger.MinimumLevel = DiagnosticLevel.Error;

                Assert.IsTrue(logger.EnableLogging());

                GridForgeLogger.Channel.Write(DiagnosticLevel.Warning, "suppressed warning", "UnityLoggerTest");

                LogAssert.Expect(LogType.Error, "[GridForge] [ERROR] [UnityLoggerTest] reported error");
                GridForgeLogger.Channel.Write(DiagnosticLevel.Error, "reported error", "UnityLoggerTest");
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void DisableLoggingRestoresPreviousHandlerAndMinimumLevel()
        {
            SwiftList<string> capturedLogs = new();
            Action<DiagnosticLevel, string, string> previousHandler =
                (level, message, source) => capturedLogs.Add($"{level}|{source}|{message}");
            GridForgeLogger.LogHandler = previousHandler;
            GridForgeLogger.MinimumLevel = DiagnosticLevel.Warning;
            GameObject owner = new("GridForge Unity logger test");

            try
            {
                GridForgeUnityLogger logger = owner.AddComponent<GridForgeUnityLogger>();
                logger.MinimumLevel = DiagnosticLevel.Error;

                Assert.IsTrue(logger.EnableLogging());
                Assert.AreEqual(DiagnosticLevel.Error, GridForgeLogger.MinimumLevel);

                logger.DisableLogging();

                Assert.IsFalse(logger.IsLoggingEnabled);
                Assert.AreEqual(DiagnosticLevel.Warning, GridForgeLogger.MinimumLevel);
                Assert.AreSame(previousHandler, GridForgeLogger.LogHandler);

                GridForgeLogger.Channel.Write(DiagnosticLevel.Warning, "restored", "PreviousHandler");

                Assert.AreEqual(1, capturedLogs.Count);
                Assert.AreEqual("Warning|PreviousHandler|restored", capturedLogs[0]);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void DestroyingEnabledComponentRestoresPreviousLoggerSettings()
        {
            Action<DiagnosticLevel, string, string> previousHandler = (_, _, _) => { };
            GridForgeLogger.LogHandler = previousHandler;
            GridForgeLogger.MinimumLevel = DiagnosticLevel.Info;
            GameObject owner = new("GridForge Unity logger test");
            GridForgeUnityLogger logger = owner.AddComponent<GridForgeUnityLogger>();
            logger.MinimumLevel = DiagnosticLevel.Error;

            Assert.IsTrue(logger.EnableLogging());

            UnityEngine.Object.DestroyImmediate(owner);

            Assert.AreEqual(DiagnosticLevel.Info, GridForgeLogger.MinimumLevel);
            Assert.AreSame(previousHandler, GridForgeLogger.LogHandler);
        }
    }
}
