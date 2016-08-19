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
        [Fact]
        public void Simple()
        {
            var subject = new SemVer("1.2.3");

            Assert.Equal(1, subject.Major);
            Assert.Equal(2, subject.Minor);
            Assert.Equal(3, subject.Patch);
            Assert.Equal(string.Empty, subject.PreRelease);
            Assert.Equal(string.Empty, subject.BuildMetadata);
        }

        [Fact]
        public void OnlyMajor()
        {
            var subject = new SemVer("1");

            Assert.Equal(1, subject.Major);
            Assert.Equal(0, subject.Minor);
            Assert.Equal(0, subject.Patch);
            Assert.Equal(string.Empty, subject.PreRelease);
            Assert.Equal(string.Empty, subject.BuildMetadata);
        }

        [Fact]
        public void WithPreRelease()
        {
            var subject = new SemVer("1.2.3-abc");

            Assert.Equal(1, subject.Major);
            Assert.Equal(2, subject.Minor);
            Assert.Equal(3, subject.Patch);
            Assert.Equal("abc", subject.PreRelease);
            Assert.Equal(string.Empty, subject.BuildMetadata);
        }

        [Fact]
        public void WithBuildMeta()
        {
            var subject = new SemVer("1.2.3+xyz");

            Assert.Equal(1, subject.Major);
            Assert.Equal(2, subject.Minor);
            Assert.Equal(3, subject.Patch);
            Assert.Equal(string.Empty, subject.PreRelease);
            Assert.Equal("xyz", subject.BuildMetadata);
        }

        [Fact]
        public void WithPreReleaseAndBuildMeta()
        {
            var subject = new SemVer("1.2.3-abc+xyz");

            Assert.Equal(1, subject.Major);
            Assert.Equal(2, subject.Minor);
            Assert.Equal(3, subject.Patch);
            Assert.Equal("abc", subject.PreRelease);
            Assert.Equal("xyz", subject.BuildMetadata);
        }

        [Theory]
        [InlineData("1.2.3-rc1.2+zxc", 1, 2, 3, "rc1.2", "zxc")]
        [InlineData("1.2.3-a-b+zxc-1.x", 1, 2, 3, "a-b", "zxc-1.x")]
        [InlineData("123.45.6789", 123, 45, 6789, null, null)]
        public void Version(string version, int major, int minor, int patch, string pre, string build)
        {
            var subject = new SemVer(version);

            Assert.Equal(major, subject.Major);
            Assert.Equal(minor, subject.Minor);
            Assert.Equal(patch, subject.Patch);
            Assert.Equal(pre ?? string.Empty, subject.PreRelease);
            Assert.Equal(build ?? string.Empty, subject.BuildMetadata);
        }
    }
}