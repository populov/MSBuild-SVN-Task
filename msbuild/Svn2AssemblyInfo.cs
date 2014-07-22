using System.Linq;

namespace HauntedSoft.MsBuild
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Threading;
    using Microsoft.Build.Utilities;

    public class Svn2AssemblyInfo : Task
    {
        private delegate void LogMessageFunction(string command, params object[] arguments);

        public string TemplateFile { get; set; }
        public string OutputFile { get; set; }

        public override bool Execute()
        {
            LogMessageFunction log = (str, args) => Log.LogMessage(str, args);
            var templateFile = TemplateFile ?? "Properties\\AssemblyInfo.cs.in";
            var outputFile = OutputFile ?? "Properties\\AssemblyInfo.cs";

            var svnInfo = GetCommandOutput("cmd", "/c svn info", log);
            if (string.IsNullOrWhiteSpace(svnInfo) || svnInfo.Count(c => c == '\r' || c == '\n') < 3)
                svnInfo = GetCommandOutput("cmd", "/c git svn info", log);

            var template = File.Exists(templateFile) ? File.ReadAllText(templateFile) : "";
            var newContent = GenerateFileContent(svnInfo, template);

            var outDir = new FileInfo(outputFile).DirectoryName;
            if (outDir != null && !Directory.Exists(outDir))
                Directory.CreateDirectory(outDir);
            if (FileMissingOrOutdated(outputFile, newContent))
            {
                File.WriteAllText(outputFile, newContent);
                log("{0} updated", outputFile);
            }
            else
            {
                log("{0} stay untouched", outputFile);
            }
            return true;
        }

        public static string GenerateFileContent(string svnInfo, string template)
        {
            var pairs = svnInfo.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries)
                .Where(s => !string.IsNullOrWhiteSpace(s) && s.Contains(":"))
                .ToDictionary(GetKey, s => s.Substring(s.IndexOf(":", StringComparison.InvariantCulture) + 1).Trim());

            var result = new StringBuilder(template);
            foreach (var pair in pairs)
            {
                result.Replace(pair.Key, pair.Value);
            }
            return result.ToString();
        }

        public static string GetKey(string s)
        {
            return "%" + s.Substring(0, s.IndexOf(":", StringComparison.InvariantCulture)).Trim() + "%";
        }

        private static bool FileMissingOrOutdated(string fileName, string newContent)
        {
            if (!File.Exists(fileName))
                return true;
            var oldContent = File.ReadAllText(fileName);
            return newContent != oldContent;
        }

        private static string GetCommandOutput(string command, string args, LogMessageFunction log)
        {
            var psi = new ProcessStartInfo
                          {
                              Arguments = args,
                              CreateNoWindow = true,
                              ErrorDialog = false,
                              FileName = command,
                              UseShellExecute = false,
                              WorkingDirectory = Directory.GetCurrentDirectory(),
                              RedirectStandardError = true,
                              RedirectStandardOutput = true,
                          };
            log(command + " " + args);
            var output = new StringBuilder();
            try
            {
                var p = Process.Start(psi);
                while (!p.HasExited)
                {
                    if (!p.StandardOutput.EndOfStream || !p.StandardError.EndOfStream)
                    {
                        AppendOutput(p, output);
                    }
                    else
                    {
                        Thread.Sleep(200);
                    }
                }
                AppendOutput(p, output);
                log("exit code:" + p.ExitCode);
                return output.ToString();
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private static void AppendOutput(Process p, StringBuilder output)
        {
            if (!p.StandardOutput.EndOfStream)
            {
                var str = p.StandardOutput.ReadToEnd();
                output.Append(str);
                Console.WriteLine(str);
            }
            if (!p.StandardError.EndOfStream)
            {
                var str = p.StandardError.ReadToEnd();
                Console.WriteLine(str);
            }
        }
    }
}
