using System.Linq;

namespace HauntedSoft.MsBuild
{
    using System;
    using System.IO;
    using System.Text;
    using Microsoft.Build.Utilities;

    public class Svn2AssemblyInfo : Task
    {
        readonly Util.LogMessageFunction log;

        public Svn2AssemblyInfo()
        {
            log = (str, args) => Log.LogMessage(str, args);
        }

        public Svn2AssemblyInfo(Util.LogMessageFunction log)
        {
            this.log = log;
        }

        public string TemplateFile { get; set; }
        public string OutputFile { get; set; }
        public bool Silent { get; set; }

        public override bool Execute()
        {
            var templateFile = TemplateFile ?? "Properties\\AssemblyInfo.cs.in";
            var outputFile = OutputFile ?? "Properties\\AssemblyInfo.cs";

            var svnInfo = Util.GetCommandOutput("cmd", "/c svn info", log, Silent);
            if (string.IsNullOrWhiteSpace(svnInfo) || svnInfo.Count(c => c == '\r' || c == '\n') < 3)
                svnInfo = Util.GetCommandOutput("cmd", "/c git svn info", log, Silent);

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
    }
}
