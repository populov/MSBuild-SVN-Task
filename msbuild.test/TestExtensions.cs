using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace HauntedSoft.MsBuild.Test
{
    static class TestExtensions
    {
        public static void AssertContainsOnly(this IEnumerable<string> actualValues, params string[] expected)
        {
            var actual = actualValues.ToArray();
            foreach (var s in expected)
            {
                Assert.Contains(s, actual);
            }
            if (expected.Length != actual.Length)
            {
                var exp = new List<string>(expected);
                exp.Sort();
                var act = new List<string>(actual);
                act.Sort();
                Assert.AreEqual(string.Join(",", exp), string.Join(",", act));
            }
        }
    }
}
