using NUnit.Framework;

namespace Nekoyume.Tests.Telemetry
{
    public class AnalyticsValueTests
    {
        [Test]
        public void Bool_KeepsMixpanelCapitalization()
        {
            AnalyticsValue t = true;
            AnalyticsValue f = false;
            Assert.AreEqual("True", t.ToString());
            Assert.AreEqual("False", f.ToString());
        }

        [Test]
        public void Numbers_StringsAndNulls()
        {
            AnalyticsValue i = 5;
            AnalyticsValue l = 12345678901L;
            AnalyticsValue s = "abc";
            int? none = null;
            AnalyticsValue n = none;
            Assert.AreEqual("5", i.ToString());
            Assert.AreEqual("12345678901", l.ToString());
            Assert.AreEqual("abc", s.ToString());
            Assert.AreEqual(string.Empty, n.ToString());
        }
    }
}
