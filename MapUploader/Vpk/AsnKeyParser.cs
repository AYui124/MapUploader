using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MapUploader.Vpk
{
    internal sealed class BerDecodeException : Exception
    {
        private readonly int _position;

        public BerDecodeException(string message, int position)
            : base(message)
        {
            _position = position;
        }

        public BerDecodeException(string message, int position, Exception ex)
            : base(message, ex)
        {
            _position = position;
        }

        public override string Message
        {
            get
            {
                var sb = new StringBuilder(base.Message);

                sb.AppendFormat(" (Position {0}){1}",
                                _position, Environment.NewLine);

                return sb.ToString();
            }
        }
    }

    internal sealed class AsnKeyParser
    {
        private readonly AsnParser _parser;

        public AsnKeyParser(ICollection<byte> contents)
        {
            _parser = new AsnParser(contents);
        }

        public static byte[] TrimLeadingZero(byte[] values)
        {
            byte[] r;
            if ((0x00 == values[0]) && (values.Length > 1))
            {
                r = new byte[values.Length - 1];
                Array.Copy(values, 1, r, 0, values.Length - 1);
            }
            else
            {
                r = new byte[values.Length];
                Array.Copy(values, r, values.Length);
            }

            return r;
        }

        public static bool EqualOid(byte[] first, byte[] second)
        {
            if (first.Length != second.Length)
            {
                return false;
            }

            return !first.Where((t, i) => t != second[i]).Any();
        }

        public RSAParameters ParseRsaPublicKey()
        {
            var parameters = new RSAParameters();

            // Current value

            // Sanity Check

            // Checkpoint
            int position = _parser.CurrentPosition();

            // Ignore Sequence - PublicKeyInfo
            int length = _parser.NextSequence();
            if (length != _parser.RemainingBytes())
            {
                var sb = new StringBuilder("Incorrect Sequence Size. ");
                sb.AppendFormat("Specified: {0}, Remaining: {1}",
                                length.ToString(CultureInfo.InvariantCulture),
                                _parser.RemainingBytes().ToString(CultureInfo.InvariantCulture));
                throw new BerDecodeException(sb.ToString(), position);
            }

            // Checkpoint
            position = _parser.CurrentPosition();

            // Ignore Sequence - AlgorithmIdentifier
            length = _parser.NextSequence();
            if (length > _parser.RemainingBytes())
            {
                var sb = new StringBuilder("Incorrect AlgorithmIdentifier Size. ");
                sb.AppendFormat("Specified: {0}, Remaining: {1}",
                                length.ToString(CultureInfo.InvariantCulture),
                                _parser.RemainingBytes().ToString(CultureInfo.InvariantCulture));
                throw new BerDecodeException(sb.ToString(), position);
            }

            // Checkpoint
            position = _parser.CurrentPosition();
            // Grab the OID
            byte[] value = _parser.NextOid();
            byte[] oid = { 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d, 0x01, 0x01, 0x01 };
            if (!EqualOid(value, oid))
            {
                throw new BerDecodeException("Expected OID 1.2.840.113549.1.1.1", position);
            }

            // Optional Parameters
            if (_parser.IsNextNull())
            {
                _parser.NextNull();
                // Also OK: value = _parser.Next();
            }
            else
            {
                // Gracefully skip the optional data
                _parser.Next();
            }

            // Checkpoint
            position = _parser.CurrentPosition();

            // Ignore BitString - PublicKey
            length = _parser.NextBitString();
            if (length > _parser.RemainingBytes())
            {
                var sb = new StringBuilder("Incorrect PublicKey Size. ");
                sb.AppendFormat("Specified: {0}, Remaining: {1}",
                                length.ToString(CultureInfo.InvariantCulture),
                                _parser.RemainingBytes().ToString(CultureInfo.InvariantCulture));
                throw new BerDecodeException(sb.ToString(), position);
            }

            // Checkpoint
            position = _parser.CurrentPosition();

            // Ignore Sequence - RSAPublicKey
            length = _parser.NextSequence();
            if (length < _parser.RemainingBytes())
            {
                var sb = new StringBuilder("Incorrect RSAPublicKey Size. ");
                sb.AppendFormat("Specified: {0}, Remaining: {1}",
                                length.ToString(CultureInfo.InvariantCulture),
                                _parser.RemainingBytes().ToString(CultureInfo.InvariantCulture));
                throw new BerDecodeException(sb.ToString(), position);
            }

            parameters.Modulus = TrimLeadingZero(_parser.NextInteger());
            parameters.Exponent = TrimLeadingZero(_parser.NextInteger());

            return parameters;
        }
    }

    internal sealed class AsnParser
    {
        private readonly int _initialCount;
        private readonly List<byte> _octets;

        public AsnParser(ICollection<byte> values)
        {
            _octets = new List<byte>(values.Count);
            _octets.AddRange(values);

            _initialCount = _octets.Count;
        }

        public int CurrentPosition()
        {
            return _initialCount - _octets.Count;
        }

        public int RemainingBytes()
        {
            return _octets.Count;
        }

        private int GetLength()
        {
            int length = 0;

            // Checkpoint
            int position = CurrentPosition();

            try
            {
                byte b = GetNextOctet();

                if (b == (b & 0x7f))
                {
                    return b;
                }

                int i = b & 0x7f;

                if (i > 4)
                {
                    var sb = new StringBuilder("Invalid Length Encoding. ");
                    sb.AppendFormat("Length uses {0} _octets",
                                    i.ToString(CultureInfo.InvariantCulture));
                    throw new BerDecodeException(sb.ToString(), position);
                }

                while (0 != i--)
                {
                    // shift left
                    length <<= 8;

                    length |= GetNextOctet();
                }
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new BerDecodeException("Error Parsing Key", position, ex);
            }

            return length;
        }

        public byte[] Next()
        {
            int position = CurrentPosition();

            try
            {
                byte b = GetNextOctet();

                int length = GetLength();
                if (length > RemainingBytes())
                {
                    var sb = new StringBuilder("Incorrect Size. ");
                    sb.AppendFormat("Specified: {0}, Remaining: {1}",
                                    length.ToString(CultureInfo.InvariantCulture),
                                    RemainingBytes().ToString(CultureInfo.InvariantCulture));
                    throw new BerDecodeException(sb.ToString(), position);
                }

                return GetOctets(length);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new BerDecodeException("Error Parsing Key", position, ex);
            }
        }

        private byte GetNextOctet()
        {
            int position = CurrentPosition();

            if (0 == RemainingBytes())
            {
                var sb = new StringBuilder("Incorrect Size. ");
                sb.AppendFormat("Specified: {0}, Remaining: {1}",
                                1.ToString(CultureInfo.InvariantCulture),
                                RemainingBytes().ToString(CultureInfo.InvariantCulture));
                throw new BerDecodeException(sb.ToString(), position);
            }

            byte b = GetOctets(1)[0];

            return b;
        }

        private byte[] GetOctets(int octetCount)
        {
            int position = CurrentPosition();

            if (octetCount > RemainingBytes())
            {
                var sb = new StringBuilder("Incorrect Size. ");
                sb.AppendFormat("Specified: {0}, Remaining: {1}",
                                octetCount.ToString(CultureInfo.InvariantCulture),
                                RemainingBytes().ToString(CultureInfo.InvariantCulture));
                throw new BerDecodeException(sb.ToString(), position);
            }

            var values = new byte[octetCount];

            try
            {
                _octets.CopyTo(0, values, 0, octetCount);
                _octets.RemoveRange(0, octetCount);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new BerDecodeException("Error Parsing Key", position, ex);
            }

            return values;
        }

        public bool IsNextNull()
        {
            return _octets[0] == 0x05;
        }

        public int NextNull()
        {
            int position = CurrentPosition();

            try
            {
                byte b = GetNextOctet();
                if (0x05 != b)
                {
                    var sb = new StringBuilder("Expected Null. ");
                    sb.AppendFormat("Specified Identifier: {0}", b.ToString(CultureInfo.InvariantCulture));
                    throw new BerDecodeException(sb.ToString(), position);
                }

                // Next octet must be 0
                b = GetNextOctet();
                if (0x00 != b)
                {
                    var sb = new StringBuilder("Null has non-zero size. ");
                    sb.AppendFormat("Size: {0}", b.ToString(CultureInfo.InvariantCulture));
                    throw new BerDecodeException(sb.ToString(), position);
                }

                return 0;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new BerDecodeException("Error Parsing Key", position, ex);
            }
        }

        public int NextSequence()
        {
            int position = CurrentPosition();

            try
            {
                byte b = GetNextOctet();
                if (0x30 != b)
                {
                    var sb = new StringBuilder("Expected Sequence. ");
                    sb.AppendFormat("Specified Identifier: {0}",
                                    b.ToString(CultureInfo.InvariantCulture));
                    throw new BerDecodeException(sb.ToString(), position);
                }

                int length = GetLength();
                if (length > RemainingBytes())
                {
                    var sb = new StringBuilder("Incorrect Sequence Size. ");
                    sb.AppendFormat("Specified: {0}, Remaining: {1}",
                                    length.ToString(CultureInfo.InvariantCulture),
                                    RemainingBytes().ToString(CultureInfo.InvariantCulture));
                    throw new BerDecodeException(sb.ToString(), position);
                }

                return length;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new BerDecodeException("Error Parsing Key", position, ex);
            }
        }

        public int NextBitString()
        {
            int position = CurrentPosition();

            try
            {
                byte b = GetNextOctet();
                if (0x03 != b)
                {
                    var sb = new StringBuilder("Expected Bit String. ");
                    sb.AppendFormat("Specified Identifier: {0}", b.ToString(CultureInfo.InvariantCulture));
                    throw new BerDecodeException(sb.ToString(), position);
                }

                int length = GetLength();

                // We need to consume unused bits, which is the first
                //   octet of the remaing values
                b = _octets[0];
                _octets.RemoveAt(0);
                length--;

                if (0x00 != b)
                {
                    throw new BerDecodeException("The first octet of BitString must be 0", position);
                }

                return length;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new BerDecodeException("Error Parsing Key", position, ex);
            }
        }

        public byte[] NextInteger()
        {
            int position = CurrentPosition();

            try
            {
                byte b = GetNextOctet();
                if (0x02 != b)
                {
                    var sb = new StringBuilder("Expected Integer. ");
                    sb.AppendFormat("Specified Identifier: {0}", b.ToString(CultureInfo.InvariantCulture));
                    throw new BerDecodeException(sb.ToString(), position);
                }

                int length = GetLength();
                if (length > RemainingBytes())
                {
                    var sb = new StringBuilder("Incorrect Integer Size. ");
                    sb.AppendFormat("Specified: {0}, Remaining: {1}",
                                    length.ToString(CultureInfo.InvariantCulture),
                                    RemainingBytes().ToString(CultureInfo.InvariantCulture));
                    throw new BerDecodeException(sb.ToString(), position);
                }

                return GetOctets(length);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new BerDecodeException("Error Parsing Key", position, ex);
            }
        }

        public byte[] NextOid()
        {
            int position = CurrentPosition();

            try
            {
                byte b = GetNextOctet();
                if (0x06 != b)
                {
                    var sb = new StringBuilder("Expected Object Identifier. ");
                    sb.AppendFormat("Specified Identifier: {0}",
                                    b.ToString(CultureInfo.InvariantCulture));
                    throw new BerDecodeException(sb.ToString(), position);
                }

                int length = GetLength();
                if (length > RemainingBytes())
                {
                    var sb = new StringBuilder("Incorrect Object Identifier Size. ");
                    sb.AppendFormat("Specified: {0}, Remaining: {1}",
                                    length.ToString(CultureInfo.InvariantCulture),
                                    RemainingBytes().ToString(CultureInfo.InvariantCulture));
                    throw new BerDecodeException(sb.ToString(), position);
                }

                var values = new byte[length];

                for (int i = 0; i < length; i++)
                {
                    values[i] = _octets[0];
                    _octets.RemoveAt(0);
                }

                return values;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new BerDecodeException("Error Parsing Key", position, ex);
            }
        }
    }
}