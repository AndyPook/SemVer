using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

using SemVer;

namespace SemVer.Tests
{
    public class ParseTests
    {
        [Theory]
        [InlineData("1.2.3", 1, 2, 3, null, null)]
        [InlineData("123.45.6789", 123, 45, 6789, null, null)]
        [InlineData("1.2.3-rc1", 1, 2, 3, "rc1", null)]
        [InlineData("1.2.3+abc", 1, 2, 3, null, "abc")]
        [InlineData("1.2.3-rc1+abc", 1, 2, 3, "rc1", "abc")]
        [InlineData("1.2.3-rc1.2+zxc", 1, 2, 3, "rc1.2", "zxc")]
        [InlineData("1.2.3-a-b+zxc-1.x", 1, 2, 3, "a-b", "zxc-1.x")]
        [InlineData("1.2.3-0.1", 1, 2, 3, "0.1", null)]
        [InlineData("1.2.3+0.01", 1, 2, 3, null, "0.01")]
        [InlineData("1.2.3+01", 1, 2, 3, null, "01")]
        public void ParseValid(string version, int major, int minor, int patch, string pre, string build)
        {
            var subject = new SemVer(version);

            Assert.Equal(major, subject.Major);
            Assert.Equal(minor, subject.Minor);
            Assert.Equal(patch, subject.Patch);
            Assert.Equal(pre ?? string.Empty, subject.PreRelease);
            Assert.Equal(build ?? string.Empty, subject.BuildMetadata);
        }

        [Theory]
        [InlineData("")]
        [InlineData("x")]
        [InlineData("1")]
        [InlineData("-1")]
        [InlineData("1.-1")]
        [InlineData("1.1.-1")]
        [InlineData("1.1")]
        [InlineData("1.")]
        [InlineData("1..")]
        [InlineData("1.1.x")]
        [InlineData("1.0.01")]
        [InlineData("1.0.0.")]
        [InlineData("1.0.0,ab")]
        [InlineData("1.0.0-")]
        [InlineData("1.0.0-#")]
        [InlineData("1.0.0-!")]
        [InlineData("1.0.0-.")]
        [InlineData("1.0.0-..")]
        [InlineData("1.0.0-a..b")]
        [InlineData("1.0.0-001")]
        [InlineData("1.0.0-00")]
        [InlineData("1.0.0-+")]
        [InlineData("1.0.0-+a")]
        [InlineData("1.0.0-a+")]
        [InlineData("1.0.0+")]
        public void ParseErrors(string version)
        {
            Assert.Throws<SemVer.ParseException>(() => new SemVer(version));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("#")]
        [InlineData("!")]
        [InlineData("+")]
        [InlineData(".")]
        [InlineData("..")]
        [InlineData("a..b")]
        [InlineData("a.")]
        [InlineData("-+a")]
        [InlineData("+a")]
        [InlineData("00")]
        [InlineData("01")]
        [InlineData("0.01")]
        public void PreReleaseErrors(string identifiers)
        {
            Assert.Throws<SemVer.ParseException>(() => new SemVer(preRelease: identifiers));
        }

        [Theory]
        [InlineData("0")]
        [InlineData("0.0")]
        [InlineData("0.1")]
        [InlineData("0a.1")]
        [InlineData("0-.1")]
        [InlineData("0-a.1")]
        public void PreReleaseValid(string identifiers)
        {
            var subject = new SemVer(preRelease: identifiers);
        }
    }
}