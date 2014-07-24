using System;
using System.IO;
using NUnit.Framework;

namespace HauntedSoft.MsBuild.Test
{
    [TestFixture]
    public class Svn2AssemblyInfoTest
    {
        [Test]
        public void Replace1()
        {
            const string svnInfo = "Test Key : Test Val\nKey2: Value 2 \n ";
            const string template = "Some text where $Test Key$ and $Key2$ replaced";
            var tags = Svn2AssemblyInfo.BuildTags(svnInfo);
            tags.Keys.AssertContainsOnly("$Test Key$", "$Key2$");
            tags.Values.AssertContainsOnly("Test Val", "Value 2");
            var result = Svn2AssemblyInfo.GenerateFileContent(tags, template);
            Assert.AreEqual("Some text where Test Val and Value 2 replaced", result);
        }

        [Test]
        public void ReplaceSampleGitSvn()
        {
            var svnInfo = File.ReadAllText(@"SampleData\GitSvnSample.txt");
            var tags = Svn2AssemblyInfo.BuildTags(svnInfo);
            tags.Keys.AssertContainsOnly("$WCREV$", "$Path$", "$WCURL$", "$WCROOT$", "$WCUUID$", "$WCREPOREV$", "$Node Kind$", "$Schedule$", "$WCAUTHOR$", "$WCDATE$");
            var result = Svn2AssemblyInfo.GenerateFileContent(tags, File.ReadAllText(@"SampleData\AssemblyInfo.cs.in"));
        }

        [Test]
        public void ComplexTest()
        {
            var expectedFile = new FileInfo("AssemblyInfo.cs.out").FullName;
            if (File.Exists(expectedFile))
                File.Delete(expectedFile);
            Assert.False(File.Exists(expectedFile));
            var task = new Svn2AssemblyInfo(Console.WriteLine)
            {
                TemplateFile = new FileInfo(@"SampleData\AssemblyInfo.cs.in").FullName,
                OutputFile = expectedFile,
                Silent = true
            };
            task.Execute();
            Assert.True(File.Exists(expectedFile));
        }
    }
}
