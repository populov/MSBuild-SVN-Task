using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace HauntedSoft.MsBuild
{
    public static class Util
    {
        public delegate void LogMessageFunction(string command, params object[] arguments);

        public static string QuoteIfNeed(string s)
        {
            if (s == null)
                return null;
            s = s.Trim(new[] {' ', '"'});
            if (!s.Contains(" "))
                return s;
            return "\"" + s + "\"";
        }

        public static string GetCommandOutput(string command, string args, string workingDirectory, LogMessageFunction log, bool outputToConsole = true)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT ||
                Environment.OSVersion.Platform == PlatformID.Win32Windows)
            {
                args = "/c " + command + " " + args;
                command = "cmd";
            }

            var psi = new ProcessStartInfo
            {
                Arguments = args,
                CreateNoWindow = true,
                ErrorDialog = false,
                FileName = command,
                UseShellExecute = false,
                WorkingDirectory = workingDirectory,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };
            log(command + " " + args);
            var output = new StringBuilder();
            try
            {
                var p = Process.Start(psi);
                if (p == null)
                    return string.Empty;
                while (!p.HasExited)
                {
                    if (!p.StandardOutput.EndOfStream || !p.StandardError.EndOfStream)
                    {
                        AppendOutput(p, output, outputToConsole);
                    }
                    else
                    {
                        Thread.Sleep(200);
                    }
                }
                AppendOutput(p, output, outputToConsole);
                log("exit code:" + p.ExitCode);
                return p.ExitCode == 0 ? output.ToString() : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void AppendOutput(Process p, StringBuilder output, bool outputToConsole)
        {
            if (!p.StandardOutput.EndOfStream)
            {
                var str = p.StandardOutput.ReadToEnd();
                output.Append(str);
                if (outputToConsole)
                    Console.WriteLine(str);
            }
            if (!p.StandardError.EndOfStream)
            {
                var str = p.StandardError.ReadToEnd();
                if (outputToConsole)
                    Console.WriteLine(str);
            }
        }
    }
}
