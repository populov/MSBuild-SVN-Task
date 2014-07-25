using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Build.Framework;

namespace HauntedSoft.MsBuild
{
    using System;
    using System.IO;
    using System.Text;
    using Microsoft.Build.Utilities;

    public class Svn2AssemblyInfo : Task
    {
        private const char c = '$';

        private static readonly Dictionary<string, string> textToTag = new Dictionary<string, string>
        {
            {"URL", "$WCURL$"},
            {"Repository Root", "$WCROOT$"},
            {"Repository UUID", "$WCUUID$"},
            {"Revision", "$WCREPOREV$"},
            {"Last Changed Author", "$WCAUTHOR$"},
            {"Last Changed Rev", "$WCREV$"},
            {"Last Changed Date", "$WCDATE$"},
            {"Корень репозитория", "$WCROOT$"},
            {"UUID репозитория", "$WCUUID$"},
            {"Редакция", "$WCREPOREV$"},
            {"Автор последнего изменения", "$WCAUTHOR$"},
            {"Редакция последнего изменения", "$WCREV$"},
            {"Дата последнего изменения", "$WCDATE$"},
        };

        private readonly Util.LogMessageFunction log;

        public Svn2AssemblyInfo()
        {
            log = (str, args) => Log.LogMessage(str, args);
        }

        public Svn2AssemblyInfo(Util.LogMessageFunction log)
        {
            this.log = log;
        }

        public string ProjectDir { get; set; }
        public string TemplateFile { get; set; }
        public string OutputFile { get; set; }
        public bool Silent { get; set; }

        public override bool Execute()
        {
            var templateFile = Util.QuoteIfNeed(TemplateFile) ?? "Properties\\AssemblyInfo.cs.in";
            var outputFile = Util.QuoteIfNeed(OutputFile) ?? "Properties\\AssemblyInfo.cs";
            if (string.IsNullOrEmpty(ProjectDir))
                ProjectDir = Environment.CurrentDirectory;

            if (RunSubWCRev(templateFile, outputFile))
                return true;
            var svnInfo = GetSvnInfo();
            if (string.IsNullOrWhiteSpace(svnInfo))
                svnInfo = GetGitSvnInfo();

            if (string.IsNullOrWhiteSpace(svnInfo))
            {
                log("{0} update failed", outputFile);
                return File.Exists(outputFile);
            }

            var template = File.Exists(templateFile) ? File.ReadAllText(templateFile) : "";
            var tags = BuildTags(svnInfo);
            var newContent = GenerateFileContent(tags, template);

            if (FileMissingOrOutdated(outputFile, newContent))
            {
                var outDir = new FileInfo(outputFile).DirectoryName;
                if (outDir != null && !Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);
                File.WriteAllText(outputFile, newContent);
                log("{0} updated", outputFile);
            }
            else
            {
                log("{0} stay untouched", outputFile);
            }
            return true;
        }

        private bool RunSubWCRev(string templateFile, string outputFile)
        {
            var parameters = Util.QuoteIfNeed(ProjectDir) + " " + templateFile + " " + outputFile;
            return null != Util.GetCommandOutput("SubWCRev.exe", parameters, ProjectDir, log, !Silent);
        }

        private string GetSvnInfo()
        {
            return Util.GetCommandOutput("svn", "info", ProjectDir, log, !Silent);
        }

        private string GetGitSvnInfo()
        {
            return Util.GetCommandOutput("git", "svn info", ProjectDir, log, !Silent);
        }

        public static string GenerateFileContent(IEnumerable<KeyValuePair<string, string>> tags, string template)
        {
            var result = new StringBuilder(template);
            foreach (var pair in tags)
            {
                result.Replace(pair.Key, pair.Value);
            }
            return result.ToString();
        }

        public static IDictionary<string, string> BuildTags(string svnInfo)
        {
            var pairs = (svnInfo ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(s => !string.IsNullOrWhiteSpace(s) && s.Contains(":"))
                .ToDictionary(GetKey, s => s.Substring(s.IndexOf(":", StringComparison.InvariantCulture) + 1).Trim());
            var result = pairs.ToDictionary(p => textToTag.ContainsKey(p.Key) ? textToTag[p.Key] : c+p.Key+c, p => p.Value);
            return result;
        }

        private static string GetKey(string s)
        {
            return s.Substring(0, s.IndexOf(":", StringComparison.InvariantCulture)).Trim();
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
