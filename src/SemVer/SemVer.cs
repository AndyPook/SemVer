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

        /// <summary>
        /// Creates an instance of <see cref="SemVer"/> by parsing the <see cref="version"/> string
        /// </summary>
        /// <param name="version"></param>
        public SemVer(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                throw new ParseException("Empty version");

            this.version = version;
            Parse();
        }

        /// <summary>
        /// Creates and instance of <see cref="SemVer"/> from the specified components
        /// </summary>
        /// <param name="major"></param>
        /// <param name="minor"></param>
        /// <param name="patch"></param>
        /// <param name="preRelease"></param>
        /// <param name="buildMetadata"></param>
        public SemVer(int major = 0, int minor = 0, int patch = 0, string preRelease = null, string buildMetadata = null)
        {
            if (major < 0)
                throw new ArgumentOutOfRangeException(nameof(major), "MUST be zero or positive");
            if (minor < 0)
                throw new ArgumentOutOfRangeException(nameof(minor), "MUST be zero or positive");
            if (patch < 0)
                throw new ArgumentOutOfRangeException(nameof(patch), "MUST be zero or positive");

            Major = major;
            Minor = minor;
            Patch = patch;
            version = Major + "." + minor + "." + patch;

            if (preRelease != null)
            {
                ValidateIdentifiers(preRelease, true);
                PreRelease = preRelease;
                version += "-" + preRelease;
            }
            if (buildMetadata != null)
            {
                ValidateIdentifiers(buildMetadata, false);
                BuildMetadata = buildMetadata;
                version += "+" + buildMetadata;
            }
        }

        private readonly string version;

        /// <summary>
        /// Major version
        /// </summary>
        public int Major { get; private set; } = 0;
        /// <summary>
        /// Minor version
        /// </summary>
        public int Minor { get; private set; } = 0;
        /// <summary>
        /// Patch version
        /// </summary>
        public int Patch { get; private set; } = 0;

        public string PreRelease { get; private set; } = string.Empty;
        public string BuildMetadata { get; private set; } = string.Empty;

        public bool IsPreRelease => !string.IsNullOrEmpty(PreRelease);
        private IReadOnlyList<string> preReleaseIdentifiers;
        public IReadOnlyList<string> PreReleaseIdentifiers =>
            preReleaseIdentifiers ??
            (preReleaseIdentifiers = ReadIdentifiers(PreRelease));

        private IReadOnlyList<string> buildMetadataIdentifiers;
        public IReadOnlyList<string> BuildMetadataIdentifiers =>
            buildMetadataIdentifiers ??
            (buildMetadataIdentifiers = ReadIdentifiers(BuildMetadata));

        private int pos;
        private bool eof => pos >= version.Length;

        private void Parse()
        {
            Major = ReadNumber();
            Minor = ReadNumber();
            Patch = ReadNumber();
            if (eof)
                return;
            if (version[pos] == '.')
                throw new ParseException("Empty identifier pos=" + pos + " in '" + version + "'");
            if (version[pos] == '-')
            {
                pos++;
                PreRelease = ReadIdentifiers('+', true);
                if (eof)
                    return;
            }
            if (version[pos] == '+')
            {
                pos++;
                BuildMetadata = ReadBuildMetaData();
            }
        }

        private int ReadNumber()
        {
            if (eof)
                throw new ParseException("Expected extra version number");
            if (version[pos] == '.')
                pos++;
            int start = pos;
            int result = 0;
            while (!eof)
            {
                var c = version[pos];
                if (c >= '0' && c <= '9')
                    result = (result * 10) + (c - '0');
                else if (c == '.' || c == '-' || c == '+')
                    break;
                else
                    throw new ParseException("unexpected character '" + c + "' pos=" + pos);
                pos++;
            }
            if (pos == start)
                throw new ParseException("Empty identifier pos=" + pos);
            if (result != 0 && version[start] == '0')
                throw new ParseException("Leading zero pos=" + start);
            return result;
        }

        private string ReadBuildMetaData() => ReadIdentifiers((char)0, false);

        private string ReadIdentifiers(char separator, bool checkLeadingZeros)
        {
            if (eof)
                throw new ParseException("Empty identifier pos=end in '" + version + "'");
            int start = pos;
            int dotPos = pos - 1;
            bool isNumeric = true;
            char prev = (char)0;
            var c = version[pos];
            while (c != separator)
            {
                if (c == '.')
                {
                    // identifiers MUST NOT be empty
                    if (prev == '.' || pos == start)
                        throw new ParseException("Empty identifier pos=" + pos + " in '" + version + "'");

                    // Numeric identifiers MUST NOT include leading zeroes (in PreRelease)
                    if (checkLeadingZeros && isNumeric && (pos - dotPos > 2) && version[dotPos + 1] == '0')
                        throw new ParseException("Leading zero in identifier pos=" + pos + " in '" + version + "'");
                    dotPos = pos;
                    isNumeric = true;
                }
                else if (!IsValidIdentifierChar(c))
                    throw new ParseException("Invalid identifier character '" + c + "' pos=" + pos + " in '" + version + "'");
                if (c != '.' && !(c >= '0' && c <= '9'))
                    isNumeric = false;

                pos++;
                if (eof)
                    break;
                prev = c;
                c = version[pos];
            }
            if (c == '.' || pos == start)
                throw new ParseException("Empty identifier pos=" + pos + " in '" + version + "'");
            if (checkLeadingZeros && isNumeric && (pos - dotPos > 2) && version[dotPos + 1] == '0')
                throw new ParseException("Leading zero in identifier pos=" + pos + " in '" + version + "'");

            return version.Substring(start, pos - start);
        }

        /// <summary>
        /// Check that the char is valid within an indentifier [0-9A-Za-z-]
        /// <seealso href="http://semver.org/spec/v2.0.0.html#spec-item-9">SemVer spec</seealso>
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        static bool IsValidIdentifierChar(char c)
        {
            if (c == '-')
                return true;
            if (c >= '0' && c <= '9')
                return true;
            if (c >= 'A' && c <= 'Z')
                return true;
            if (c >= 'a' && c <= 'z')
                return true;
            return false;
        }

        /// <summary>
        /// Validates that an indentifier string contains only valid chars and no empty parts
        /// <seealso href="http://semver.org/spec/v2.0.0.html#spec-item-9">SemVer spec</seealso>
        /// <para>
        /// Only used via the multi-part ctor.
        /// Must be kept insync with <see cref="ReadTo(char)"/>
        /// </para>
        /// </summary>
        /// <param name="identifiers"></param>
        static void ValidateIdentifiers(string identifiers, bool checkLeadingZeros)
        {
            if (identifiers == null || identifiers.Length == 0)
                throw new ParseException("Empty identifier (null or empty)");
            int idPos = 0;
            int dotPos = -1;
            bool isNumeric = true;
            char prev = (char)0;
            var c = identifiers[idPos];
            while (true)
            {
                if (c == '.')
                {
                    // identifiers MUST NOT be empty
                    if (prev == '.' || idPos == 0)
                        throw new ParseException("Empty identifier pos=" + idPos + " in '" + identifiers + "'");

                    // Numeric identifiers MUST NOT include leading zeroes (in PreRelease)
                    if (checkLeadingZeros && isNumeric && (idPos - dotPos > 2) && identifiers[dotPos + 1] == '0')
                        throw new ParseException("Leading zero in identifier pos=" + idPos + " in '" + identifiers + "'");
                    dotPos = idPos;
                    isNumeric = true;
                }
                else if (!IsValidIdentifierChar(c))
                    throw new ParseException("Invalid identifier character '" + c + "' pos=" + idPos + " in '" + identifiers + "'");
                if (c != '.' && !(c >= '0' && c <= '9'))
                    isNumeric = false;

                if (++idPos >= identifiers.Length)
                    break;

                prev = c;
                c = identifiers[idPos];
            }
            // identifiers MUST NOT be empty
            if (c == '.')
                throw new ParseException("Empty identifier pos=end in '" + identifiers + "'");
            // Numeric identifiers MUST NOT include leading zeroes (in PreRelease)
            if (checkLeadingZeros && isNumeric && (idPos - dotPos > 2) && identifiers[dotPos + 1] == '0')
                throw new ParseException("Leading zero in identifier pos=" + idPos + " in '" + identifiers + "'");
        }

        /// <summary>
        /// Parse the identifiers out of a string
        /// <para>
        /// A simplified (quicker) version of String.Split
        /// </para>
        /// </summary>
        /// <param name="identifiers">Either PrePrelease of BuildMetadata</param>
        /// <returns></returns>
        static IReadOnlyList<string> ReadIdentifiers(string identifiers)
        {
            if (string.IsNullOrEmpty(identifiers))
                return EmptyIdentifiers;

            // assume there will be rarely be more than 3 identifiers
            // this means there will be no extra array allocations/copies within List if there are 3 or less
            var result = new List<string>(3);

            // we can assume that there are no invalid chars or empty identifiers due to ReadTo
            int idPos = 0;
            int idStart = 0;
            while (idPos < identifiers.Length)
            {
                var c = identifiers[idPos];
                if (c == '.')
                {
                    result.Add(identifiers.Substring(idStart, idPos - idStart));
                    idStart = idPos + 1;
                }
                idPos++;
            }
            result.Add(identifiers.Substring(idStart, idPos - idStart));

            return result;
        }

        /// <summary>
        /// <seealso href="http://semver.org/#spec-item-11">SemVer spec</seealso>
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

            // a pre-release version has lower precedence than a normal version
            if (!IsPreRelease && other.IsPreRelease)
                return 1;
            if (IsPreRelease && !other.IsPreRelease)
                return -1;

            return CompareIdentifiers(PreReleaseIdentifiers, other.PreReleaseIdentifiers);
        }

        private static int CompareIdentifiers(IReadOnlyList<string> thisIds, IReadOnlyList<string> otherIds)
        {
            // http://semver.org/spec/v2.0.0.html#spec-item-11
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

                // identifiers consisting of only digits are compared numerically
                if (thisIsNum && otherIsNum)
                {
                    if (thisNum == otherNum)
                        continue;
                    return thisNum > otherNum ? 1 : -1;
                }

                // Numeric identifiers always have lower precedence than non-numeric identifiers
                if (thisIsNum && !otherIsNum)
                    return -1;
                if (!thisIsNum && otherIsNum)
                    return 1;

                // identifiers with letters or hyphens are compared lexically in ASCII sort order
                int compare = string.CompareOrdinal(thisId, otherId);
                if (compare != 0)
                    return compare > 0 ? 1 : -1;
            }

            // A larger set of pre-release fields has a higher precedence than a smaller set
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

        /// <summary>
        /// Converts a string into a <see cref="SemVer"/>
        /// </summary>
        /// <param name="version"></param>
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