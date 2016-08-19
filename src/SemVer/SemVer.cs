using System;

namespace SemVer
{
    public class SemVer : IComparable<SemVer>, IEquatable<SemVer>
    {
        public class ParseException : Exception
        {
            public ParseException(string message) : base(message) { }
        }

        public SemVer(string version)
        {
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
            return result;
        }

        private string ReadToEnd() => ReadTo((char)0);

        private string ReadTo(char separator)
        {
            int start = pos;
            var c = version[pos];
            while (separator == 0 || c != separator)
            {
                if (!ValidIdentifierChar(c))
                    throw new ParseException("Invalid identifier character '" + c + "' pos=" + pos);
                pos++;
                if (eof)
                    break;
                c = version[pos];
            }
            return version.Substring(start, pos - start);
        }

        private bool ValidIdentifierChar(char c)
        {
            if (c == '.' || c == '-')
                return true;
            if (c >= '0' && c <= '9')
                return true;
            if (c >= 'a' && c <= 'z')
                return true;
            if (c >= 'A' && c <= 'Z')
                return true;
            return false;
        }

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

            compare = string.CompareOrdinal(PreRelease, other.PreRelease);
            return compare;
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
            return left == right || left > right;
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
            return left == right || left < right;
        }
    }
}