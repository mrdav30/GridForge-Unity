//=======================================================================
// GridForgeUnityLogger.cs
//=======================================================================
// MIT License, Copyright (c) 2024–present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

using GridForge;
using SwiftCollections.Diagnostics;
using System;
using UnityEngine;

namespace GridForge.Utility
{
    /// <summary>
    /// Optional Unity adapter that forwards <see cref="GridForgeLogger"/> messages into Unity logging.
    /// </summary>
    [AddComponentMenu("GridForge/Debugging/GridForge Unity Logger")]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public sealed class GridForgeUnityLogger : MonoBehaviour
    {
        private static GridForgeUnityLogger _activeLogger;

        [Tooltip("Enable GridForge log forwarding when this component is enabled.")]
        [SerializeField] private bool _enableOnComponentEnable;

        [Tooltip("Minimum GridForge diagnostic level forwarded to Unity logging.")]
        [SerializeField] private DiagnosticLevel _minimumLevel = DiagnosticLevel.Warning;

        private Action<DiagnosticLevel, string, string> _previousLogHandler;
        private DiagnosticLevel _previousMinimumLevel = DiagnosticLevel.Warning;
        private bool _isLoggingEnabled;

        public static GridForgeUnityLogger ActiveLogger => _activeLogger;

        public static bool HasActiveLogger => _activeLogger != null;

        public bool EnableOnComponentEnable
        {
            get => _enableOnComponentEnable;
            set => _enableOnComponentEnable = value;
        }

        public DiagnosticLevel MinimumLevel
        {
            get => _minimumLevel;
            set
            {
                _minimumLevel = value;

                if (_isLoggingEnabled)
                    GridForgeLogger.MinimumLevel = _minimumLevel;
            }
        }

        public bool IsLoggingEnabled => _isLoggingEnabled;

        private void OnEnable()
        {
            if (_enableOnComponentEnable)
                EnableLogging();
        }

        private void OnDisable()
        {
            DisableLogging();
        }

        private void OnDestroy()
        {
            DisableLogging();
        }

        private void OnValidate()
        {
            if (_isLoggingEnabled)
                GridForgeLogger.MinimumLevel = _minimumLevel;
        }

        /// <summary>
        /// Installs this component as the active Unity sink for GridForge messages.
        /// </summary>
        /// <returns><see langword="true"/> when logging is enabled by this component; otherwise, <see langword="false"/>.</returns>
        public bool EnableLogging()
        {
            if (_isLoggingEnabled)
            {
                GridForgeLogger.MinimumLevel = _minimumLevel;
                return true;
            }

            if (_activeLogger != null && !ReferenceEquals(_activeLogger, this))
            {
                Debug.LogWarning(
                    $"{nameof(GridForgeUnityLogger)} is already enabled on '{_activeLogger.name}'. " +
                    $"Disable that component before enabling another GridForge Unity logger.",
                    this);
                return false;
            }

            _previousLogHandler = GridForgeLogger.LogHandler;
            _previousMinimumLevel = GridForgeLogger.MinimumLevel;

            GridForgeLogger.LogHandler = HandleGridForgeLog;
            GridForgeLogger.MinimumLevel = _minimumLevel;

            _activeLogger = this;
            _isLoggingEnabled = true;
            return true;
        }

        /// <summary>
        /// Restores the GridForge logger settings captured when <see cref="EnableLogging"/> succeeded.
        /// </summary>
        public void DisableLogging()
        {
            if (!_isLoggingEnabled)
                return;

            if (ReferenceEquals(_activeLogger, this))
            {
                GridForgeLogger.LogHandler = _previousLogHandler;
                GridForgeLogger.MinimumLevel = _previousMinimumLevel;
                _activeLogger = null;
            }

            _previousLogHandler = null;
            _previousMinimumLevel = DiagnosticLevel.Warning;
            _isLoggingEnabled = false;
        }

        public static LogType GetUnityLogType(DiagnosticLevel level)
        {
            return level switch
            {
                DiagnosticLevel.Warning => LogType.Warning,
                DiagnosticLevel.Error => LogType.Error,
                _ => LogType.Log
            };
        }

        public static string FormatUnityLogMessage(DiagnosticLevel level, string message, string source)
        {
            string levelTag = level switch
            {
                DiagnosticLevel.None => "[NONE]",
                DiagnosticLevel.Info => "[INFO]",
                DiagnosticLevel.Warning => "[WARN]",
                DiagnosticLevel.Error => "[ERROR]",
                _ => "[LOG]"
            };

            string safeMessage = message ?? string.Empty;
            if (string.IsNullOrWhiteSpace(source))
                return $"[GridForge] {levelTag} {safeMessage}";

            return $"[GridForge] {levelTag} [{source}] {safeMessage}";
        }

        private void HandleGridForgeLog(DiagnosticLevel level, string message, string source)
        {
            string formattedMessage = FormatUnityLogMessage(level, message, source);

            switch (GetUnityLogType(level))
            {
                case LogType.Warning:
                    Debug.LogWarning(formattedMessage, this);
                    break;
                case LogType.Error:
                    Debug.LogError(formattedMessage, this);
                    break;
                default:
                    Debug.Log(formattedMessage, this);
                    break;
            }
        }
    }
}
