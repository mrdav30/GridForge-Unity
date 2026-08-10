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

        /// <summary>Gets the component currently forwarding GridForge messages.</summary>
        public static GridForgeUnityLogger ActiveLogger => _activeLogger;

        /// <summary>Gets whether a Unity logger is currently active.</summary>
        public static bool HasActiveLogger => _activeLogger != null;

        /// <summary>Gets or sets whether logging starts when the component is enabled.</summary>
        public bool EnableOnComponentEnable
        {
            get => _enableOnComponentEnable;
            set => _enableOnComponentEnable = value;
        }

        /// <summary>Gets or sets the minimum diagnostic level forwarded to Unity.</summary>
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

        /// <summary>Gets whether this component currently owns GridForge logging.</summary>
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

        /// <summary>Maps a GridForge diagnostic level to a Unity log type.</summary>
        /// <param name="level">The GridForge diagnostic level.</param>
        /// <returns>The corresponding Unity log type.</returns>
        public static LogType GetUnityLogType(DiagnosticLevel level)
        {
            return level switch
            {
                DiagnosticLevel.Warning => LogType.Warning,
                DiagnosticLevel.Error => LogType.Error,
                _ => LogType.Log
            };
        }

        /// <summary>Formats a GridForge diagnostic message for Unity logging.</summary>
        /// <param name="level">The diagnostic level.</param>
        /// <param name="message">The message text.</param>
        /// <param name="source">The optional diagnostic source.</param>
        /// <returns>The formatted Unity log message.</returns>
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
