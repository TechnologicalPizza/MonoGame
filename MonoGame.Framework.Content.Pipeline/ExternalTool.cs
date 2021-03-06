﻿// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace MonoGame.Framework.Content.Pipeline
{
    /// <summary>
    /// Helper to run an external tool installed in the system. Useful for when
    /// we don't want to package the tool ourselves (ffmpeg) or it's provided
    /// by a third party (console manufacturer).
    /// </summary>
    internal class ExternalTool
    {
        public static int Run(string command, string arguments)
        {
            int result = Run(command, arguments, out _, out _);
            if (result < 0)
                throw new Exception(string.Format("{0} returned exit code {1}", command, result));

            return result;
        }

        public static int Run(
            string command,
            string arguments,
            out StringBuilder stdout,
            out StringBuilder stderr,
            string? stdin = null)
        {
            // This particular case is likely to be the most common and thus
            // warrants its own specific error message rather than falling
            // back to a general exception from Process.Start()
            string fullPath = FindCommand(command);
            if (string.IsNullOrEmpty(fullPath))
                throw new Exception(string.Format("Couldn't locate external tool '{0}'.", command));

            // We can't reference ref or out parameters from within
            // lambdas (for the thread functions), so we have to store
            // the data in a temporary variable and then assign these
            // variables to the out parameters.
            var stdoutTmp = new StringBuilder();
            var stderrTmp = new StringBuilder();

            static void RedirectStream(
                StreamReader input, StringBuilder output)
            {
                string? line;
                while ((line = input.ReadLine()) != null)
                {
                    output.AppendLine(line);
                }
            }

            EnsureExecutable(fullPath);

            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    Arguments = arguments,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    ErrorDialog = false,
                    FileName = fullPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                };
                process.Start();

                // We have to run these in threads, because reading on one stream can deadlock.
                var stdoutThread = new Thread(() => RedirectStream(process.StandardOutput, stdoutTmp));
                var stderrThread = new Thread(() => RedirectStream(process.StandardError, stderrTmp));
                stdoutThread.Start();
                stderrThread.Start();

                if (stdin != null)
                    process.StandardInput.Write(stdin);

                // Make sure interactive prompts don't block.
                process.StandardInput.Close();

                process.WaitForExit();

                stdoutThread.Join();
                stderrThread.Join();

                stdout = stdoutTmp;
                stderr = stderrTmp;
                return process.ExitCode;
            }
        }

        /// <summary>
        /// Returns the fully-qualified path for a command, searching the system path if necessary.
        /// </summary>
        /// <remarks>
        /// It's apparently necessary to use the full path when running on some systems.
        /// </remarks>
        private static string? FindCommand(string command)
        {
            // Expand any environment variables.
            command = Environment.ExpandEnvironmentVariables(command);

            if (PlatformInfo.CurrentOS == PlatformInfo.OS.Windows)
            {
                var exeName = string.Concat(AppContext.BaseDirectory, command, ".exe");
                if (File.Exists(exeName))
                    return exeName;
            }
            else
            {
                // If we have a full path just pass it through.
                if (File.Exists(command))
                    return command;
            }

            // For Linux check specific subfolder
            var lincom = Path.Combine(AppContext.BaseDirectory, "linux", command);
            if (PlatformInfo.CurrentOS == PlatformInfo.OS.Linux && File.Exists(lincom))
                return lincom;

            // For Mac check specific subfolder
            var maccom = Path.Combine(AppContext.BaseDirectory, "osx", command);
            if (PlatformInfo.CurrentOS == PlatformInfo.OS.MacOSX && File.Exists(maccom))
                return maccom;

            // We don't have a full path, so try running through the system path to find it.
            var paths = Path.Combine(AppContext.BaseDirectory, Environment.GetEnvironmentVariable("PATH"));

            var justTheName = Path.GetFileName(command);
            foreach (var path in paths.Split(Path.PathSeparator))
            {
                var fullName = Path.Combine(path, justTheName);
                if (File.Exists(fullName))
                    return fullName;

                if (PlatformInfo.CurrentOS == PlatformInfo.OS.Windows)
                {
                    var fullExeName = string.Concat(fullName, ".exe");
                    if (File.Exists(fullExeName))
                        return fullExeName;
                }
            }

            return null;
        }

        /// <summary>   
        /// Ensures the specified executable has the executable bit set.  If the    
        /// executable doesn't have the executable bit set on Linux or Mac OS, then 
        /// Mono will refuse to execute it. 
        /// </summary>  
        /// <param name="path">The full path to the executable.</param> 
        private static void EnsureExecutable(string path)
        {
            if (!path.StartsWith("/home") && 
                !path.StartsWith("/Users"))
                return;

            try
            {
                var p = Process.Start("chmod", "u+x \"" + path + "\"");
                p.WaitForExit();
            }
            catch
            {
                // This platform may not have chmod in the path, in which case we can't 
                // do anything reasonable here. 
            }
        }

        /// <summary>
        /// Safely deletes the file if it exists.
        /// </summary>
        /// <param name="filePath">The path to the file to delete.</param>
        public static void DeleteFile(string filePath)
        {
            try
            {
                File.Delete(filePath);
            }
            catch (Exception)
            {                    
            }
        }
    }
}
