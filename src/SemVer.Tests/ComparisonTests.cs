using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SemVer.Tests
{
    public class ComparisonTests
    {
        [Theory]
        [InlineData("1", "0")]
        [InlineData("1", "1.0.0-x")]
        public void Greater(string left, string right)
        {
            var l = new SemVer(left);
            var r = new SemVer(right);

            Assert.True(l > r);
            Assert.False(l < r);
        }
    }
}