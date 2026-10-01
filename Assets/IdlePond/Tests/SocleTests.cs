using System.Globalization;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace IdlePond.Tests
{
    public class SocleTests
    {
        [Test, Description("le socle compile et NUnit tourne en batch")]
        public void Le_socle_compile_et_NUnit_tourne_en_batch()
        {
            Assert.That(JObject.Parse("{\"a\":2.4}")["a"].Value<double>(), Is.EqualTo(2.4));
            Assert.That(2.4.ToString(CultureInfo.InvariantCulture), Is.EqualTo("2.4"));
        }
    }
}
