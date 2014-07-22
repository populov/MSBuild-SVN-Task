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
            const string template = "Some text where %Test Key% and %Key2% replaced";
            var result = Svn2AssemblyInfo.GenerateFileContent(svnInfo, template);
            Assert.AreEqual("Some text where Test Val and Value 2 replaced", result);
        }
    }
}
