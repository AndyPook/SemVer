using System;
using System.Collections;
using System.Collections.Generic;

namespace SemVer
{
    public class SemVer : IComparable<SemVer>, IEquatable<SemVer>
    {
        public class ParseException : Exception
        {
            public ParseException(string message) : base(message) { }
        }

        private static readonly string[] EmptyIdentifiers = new string[0];

        public SemVer(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                throw new ParseException("Empty version");

            this.version = version;
            if (!string.IsNullOrWhiteSpace(version))
                Parse();
        }

        private readonly string version;

        public int Major { get; private set; } = 0;
        public int Minor { get; private set; } = 0;
        public int Patch { get; private set; } = 0;
        public string PreRelease { get; private set; } = string.Empty;
        public string BuildMetadata { get; private set; } = string.Empty;

        public bool IsPreRelease => !string.IsNullOrEmpty(PreRelease);
        private IReadOnlyList<string> preReleaseIdentifiers;
        public IReadOnlyList<string> PreReleaseIdentifiers => 
            preReleaseIdentifiers ?? 
            (preReleaseIdentifiers = PreRelease.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries));

        private int pos;
        private bool eof => pos >= version.Length;

        private void Parse()
        {
            Major = ReadNumber();
            Minor = ReadNumber();
            Patch = ReadNumber();
            if (eof)
                return;
            if (version[pos] == '-')
            {
                pos++;
                PreRelease = ReadTo('+');
            }
            if (eof)
                return;
            if (version[pos] == '+')
            {
                pos++;
                BuildMetadata = ReadToEnd();
            }
        }

        private int ReadNumber()
        {
            int start = pos;
            int result = 0;
            while (!eof)
            {
                var c = version[pos];
                if (c >= '0' && c <= '9')
                    result = (result * 10) + (c - '0');
                else if (c == '.')
                {
                    pos++;
                    break;
                }
                else if (c == '-' || c == '+')
                    break;
                else
                    throw new ParseException("unexpected character '" + c + "' pos=" + pos);
                pos++;
            }
            if (pos == start)
                throw new ParseException("Empty identifier");
            return result;
        }

        private string ReadToEnd() => ReadTo((char)0);

        private string ReadTo(char separator)
        {
            int start = pos;
            char prev = (char)0;
            var c = version[pos];
            while (c != separator)
            {
                if (c == '.')
                {
                    if (prev == '.')
                        throw new ParseException("Empty identifier");
                }
                else if (!ValidIdentifierChar(c))
                    throw new ParseException("Invalid identifier character '" + c + "' pos=" + pos);
                pos++;
                if (eof)
                    break;
                prev = c;
                c = version[pos];
            }
            return version.Substring(start, pos - start);
        }

        static bool ValidIdentifierChar(char c)
        {
            if (c == '-')
                return true;
            if (c >= '0' && c <= '9')
                return true;
            if (c >= 'a' && c <= 'z')
                return true;
            if (c >= 'A' && c <= 'Z')
                return true;
            return false;
        }

        /// <summary>
        /// Precedence refers to how versions are compared to each other when ordered. 
        /// Precedence MUST be calculated by separating the version into major, minor, patch and 
        /// pre-release identifiers in that order (Build metadata does not figure into precedence). 
        /// Precedence is determined by the first difference when comparing each of these 
        /// identifiers from left to right as follows: Major, minor, and patch versions are always compared numerically. 
        /// Example: 1.0.0 &lt; 2.0.0 &lt; 2.1.0 &lt; 2.1.1. 
        /// When major, minor, and patch are equal, a pre-release version has lower precedence than a normal version. 
        /// Example: 1.0.0-alpha &lt; 1.0.0. 
        /// Precedence for two pre-release versions with the same major, minor, and patch version 
        /// MUST be determined by comparing each dot separated identifier from left to right until 
        /// a difference is found as follows: 
        /// identifiers consisting of only digits are compared numerically and identifiers with 
        /// letters or hyphens are compared lexically in ASCII sort order. 
        /// Numeric identifiers always have lower precedence than non-numeric identifiers. 
        /// A larger set of pre-release fields has a higher precedence than a smaller set, 
        /// if all of the preceding identifiers are equal. 
        /// Example: 1.0.0-alpha &lt; 1.0.0-alpha.1 &lt; 1.0.0-alpha.beta &lt; 1.0.0-beta &lt; 
        /// 1.0.0-beta.2 &lt; 1.0.0-beta.11 &lt; 1.0.0-rc.1 &lt; 1.0.0.
        /// <seealso href="http://semver.org/#spec-item-11">SemVer spec</seealso>
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(SemVer other)
        {
            if (ReferenceEquals(other, null))
                return 1;

            var compare = Major.CompareTo(other.Major);
            if (compare != 0)
                return compare;

            compare = Minor.CompareTo(other.Minor);
            if (compare != 0)
                return compare;

            compare = Patch.CompareTo(other.Patch);
            if (compare != 0)
                return compare;

            if (string.IsNullOrEmpty(PreRelease) && !string.IsNullOrEmpty(other.PreRelease))
                return 1;

            compare = CompareIdentifiers(PreReleaseIdentifiers, other.PreReleaseIdentifiers);
            return compare;
        }

        private static int CompareIdentifiers(IReadOnlyList<string> thisIds, IReadOnlyList<string> otherIds)
        {
            // 1.0.0-alpha < 1.0.0-alpha.1 < 1.0.0-alpha.beta < 1.0.0-beta < 
            // 1.0.0-beta.2 < 1.0.0-beta.11 < 1.0.0-rc.1 < 1.0.0.

            if (thisIds.Count == 0 && otherIds.Count > 0)
                return 1;
            if (thisIds.Count > 0 && otherIds.Count == 0)
                return -1;

            var minCount = thisIds.Count > otherIds.Count ? otherIds.Count : thisIds.Count;
            for (int i = 0; i < minCount; i++)
            {
                var thisId = thisIds[i];
                var otherId = otherIds[i];

                int thisNum;
                bool thisIsNum = int.TryParse(thisId, out thisNum);
                int otherNum;
                bool otherIsNum = int.TryParse(otherId, out otherNum);

                if (thisIsNum && otherIsNum)
                {
                    if (thisNum == otherNum)
                        continue;
                    return thisNum > otherNum ? 1 : -1;
                }
                if (thisIsNum && !otherIsNum)
                    return -1;
                if (!thisIsNum && otherIsNum)
                    return 1;
                int compare = string.CompareOrdinal(thisId, otherId);
                if (compare != 0)
                    return compare > 0 ? 1 : -1;
            }
            if (thisIds.Count == otherIds.Count)
                return 0;
            return thisIds.Count > otherIds.Count ? 1 : -1;
        }

        public bool Equals(SemVer other)
        {
            if (other == null)
                return false;

            return
                Major == other.Major &&
                Minor == other.Minor &&
                Patch == other.Patch &&
                PreRelease == other.PreRelease;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
                return false;
            if (ReferenceEquals(obj, this))
                return true;

            var other = (SemVer)obj;
            return Equals(other);
        }

        public override int GetHashCode()
        {
            return version.GetHashCode();
        }

        public override string ToString()
        {
            return version;
        }

        public static bool Equals(SemVer x, SemVer y)
        {
            if (ReferenceEquals(x, null))
                return false;
            if (ReferenceEquals(y, null))
                return false;
            return x.Equals(y);
        }
        public static int Compare(SemVer x, SemVer y)
        {
            if (ReferenceEquals(x, null))
                return ReferenceEquals(y, null) ? 0 : -1;
            return x.CompareTo(y);
        }

        public static implicit operator SemVer(string version)
        {
            return new SemVer(version);
        }

        /// <summary>
        /// The override of the equals operator. 
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns>If left is equal to right <c>true</c>, else <c>false</c>.</returns>
        public static bool operator ==(SemVer left, SemVer right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// The override of the un-equal operator. 
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns>If left is not equal to right <c>true</c>, else <c>false</c>.</returns>
        public static bool operator !=(SemVer left, SemVer right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// The override of the greater operator. 
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns>If left is greater than right <c>true</c>, else <c>false</c>.</returns>
        public static bool operator >(SemVer left, SemVer right)
        {
            return Compare(left, right) > 0;
        }

        /// <summary>
        /// The override of the greater than or equal operator. 
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns>If left is greater than or equal to right <c>true</c>, else <c>false</c>.</returns>
        public static bool operator >=(SemVer left, SemVer right)
        {
            return Compare(left, right) >= 0;
        }

        /// <summary>
        /// The override of the less operator. 
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns>If left is less than right <c>true</c>, else <c>false</c>.</returns>
        public static bool operator <(SemVer left, SemVer right)
        {
            return Compare(left, right) < 0;
        }

        /// <summary>
        /// The override of the less than or equal operator. 
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns>If left is less than or equal to right <c>true</c>, else <c>false</c>.</returns>
        public static bool operator <=(SemVer left, SemVer right)
        {
            return Compare(left, right) <= 0;
        }
    }
}