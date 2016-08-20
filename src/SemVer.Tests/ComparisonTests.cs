using Xunit;

namespace SemVer.Tests
{
    public class ComparisonTests
    {
        //1.0.0-alpha < 1.0.0-alpha.1 < 1.0.0-alpha.beta < 1.0.0-beta < 1.0.0-beta.2 < 1.0.0-beta.11 < 1.0.0-rc.1 < 1.0.0
        [Theory]
        [InlineData("0.0.0", "1.0.0")]
        [InlineData("1.0.0-x", "1.0.0")]
        [InlineData("1.0.0-x", "1.0.0-x.1")]
        [InlineData("1.0.0-alpha", "1.0.0-alpha.1")]
        [InlineData("1.0.0-alpha.1", "1.0.0-alpha.beta")]
        [InlineData("1.0.0-alpha.beta", "1.0.0-beta")]
        [InlineData("1.0.0-beta", "1.0.0-beta.2")]
        [InlineData("1.0.0-beta.2", "1.0.0-beta.11")]
        [InlineData("1.0.0-beta.11", "1.0.0-rc.1")]
        [InlineData("1.0.0-rc.1", "1.0.0")]
        public void LessThan(string left, string right)
        {
            var l = new SemVer(left);
            var r = new SemVer(right);

            Assert.True(l < r);
            Assert.False(l > r);
        }
    }
}