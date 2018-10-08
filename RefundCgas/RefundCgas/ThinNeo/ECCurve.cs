using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace ThinNeo.Cryptography.ECC
{
    /// <summary>
    /// ECC椭圆曲线参数
    /// </summary>
    public class ECCurve
    {
        internal readonly BigInteger Q;
        internal readonly ECFieldElement A;
        internal readonly ECFieldElement B;
        internal readonly BigInteger N;
        /// <summary>
        /// 无穷远点
        /// </summary>
        public readonly ECPoint Infinity;
        /// <summary>
        /// 基点
        /// </summary>
        public readonly ECPoint G;

        private ECCurve(BigInteger Q, BigInteger A, BigInteger B, BigInteger N, byte[] G)
        {
            this.Q = Q;
            this.A = new ECFieldElement(A, this);
            this.B = new ECFieldElement(B, this);
            this.N = N;
            this.Infinity = new ECPoint(null, null, this);
            this.G = ECPoint.DecodePoint(G, this);
        }

        /// <summary>
        /// 曲线secp256k1
        /// </summary>
        public static readonly ECCurve Secp256k1 = new ECCurve
        (
            BigInteger.Parse("00FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEFFFFFC2F", NumberStyles.AllowHexSpecifier),
            BigInteger.Zero,
            7,
            BigInteger.Parse("00FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEBAAEDCE6AF48A03BBFD25E8CD0364141", NumberStyles.AllowHexSpecifier),


            Helper.HexString2Bytes("04" + "79BE667EF9DCBBAC55A06295CE870B07029BFCDB2DCE28D959F2815B16F81798" + "483ADA7726A3C4655DA4FBFC0E1108A8FD17B448A68554199C47D08FFB10D4B8")
        );

        /// <summary>
        /// 曲线secp256r1
        /// </summary>
        public static readonly ECCurve Secp256r1 = new ECCurve
        (
            BigInteger.Parse("00FFFFFFFF00000001000000000000000000000000FFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.AllowHexSpecifier),
            BigInteger.Parse("00FFFFFFFF00000001000000000000000000000000FFFFFFFFFFFFFFFFFFFFFFFC", NumberStyles.AllowHexSpecifier),
            BigInteger.Parse("005AC635D8AA3A93E7B3EBBD55769886BC651D06B0CC53B0F63BCE3C3E27D2604B", NumberStyles.AllowHexSpecifier),
            BigInteger.Parse("00FFFFFFFF00000000FFFFFFFFFFFFFFFFBCE6FAADA7179E84F3B9CAC2FC632551", NumberStyles.AllowHexSpecifier),
             Helper.HexString2Bytes("04" + "6B17D1F2E12C4247F8BCE6E563A440F277037D812DEB33A0F4A13945D898C296" + "4FE342E2FE1A7F9B8EE7EB4A7C0F9E162BCE33576B315ECECBB6406837BF51F5")
        );
    }

    public class ECPoint : IComparable<ECPoint>, IEquatable<ECPoint>
    {
        internal ECFieldElement X, Y;
        internal readonly ECCurve Curve;

        /// <summary>
        /// 判断是否为无穷远点
        /// </summary>
        public bool IsInfinity
        {
            get { return X == null && Y == null; }
        }

        public int Size => IsInfinity ? 1 : 33;

        public ECPoint()
            : this(null, null, ECCurve.Secp256r1)
        {
        }

        internal ECPoint(ECFieldElement x, ECFieldElement y, ECCurve curve)
        {
            if ((x != null && y == null) || (x == null && y != null))
                throw new ArgumentException("Exactly one of the field elements is null");
            this.X = x;
            this.Y = y;
            this.Curve = curve;
        }

        /// <summary>
        /// 与另一对象进行比较
        /// </summary>
        /// <param name="other">另一对象</param>
        /// <returns>返回比较的结果</returns>
        public int CompareTo(ECPoint other)
        {
            if (ReferenceEquals(this, other)) return 0;
            int result = X.CompareTo(other.X);
            if (result != 0) return result;
            return Y.CompareTo(other.Y);
        }

        /// <summary>
        /// 从字节数组中解码
        /// </summary>
        /// <param name="encoded">要解码的字节数组</param>
        /// <param name="curve">曲线参数</param>
        /// <returns></returns>
        public static ECPoint DecodePoint(byte[] encoded, ECCurve curve)
        {
            ECPoint p = null;
            int expectedLength = (curve.Q.GetBitLength() + 7) / 8;
            switch (encoded[0])
            {
                case 0x00: // infinity
                    {
                        if (encoded.Length != 1)
                            throw new ArgumentException("Incorrect length for infinity encoding", "encoded");
                        p = curve.Infinity;
                        break;
                    }
                case 0x02: // compressed
                case 0x03: // compressed
                    {
                        if (encoded.Length != (expectedLength + 1))
                            throw new ArgumentException("Incorrect length for compressed encoding", "encoded");
                        int yTilde = encoded[0] & 1;
                        BigInteger X1 = new BigInteger(encoded.Skip(1).Reverse().Concat(new byte[1]).ToArray());
                        p = DecompressPoint(yTilde, X1, curve);
                        break;
                    }
                case 0x04: // uncompressed
                case 0x06: // hybrid
                case 0x07: // hybrid
                    {
                        if (encoded.Length != (2 * expectedLength + 1))
                            throw new ArgumentException("Incorrect length for uncompressed/hybrid encoding", "encoded");
                        BigInteger X1 = new BigInteger(encoded.Skip(1).Take(expectedLength).Reverse().Concat(new byte[1]).ToArray());
                        BigInteger Y1 = new BigInteger(encoded.Skip(1 + expectedLength).Reverse().Concat(new byte[1]).ToArray());
                        p = new ECPoint(new ECFieldElement(X1, curve), new ECFieldElement(Y1, curve), curve);
                        break;
                    }
                default:
                    throw new FormatException("Invalid point encoding " + encoded[0]);
            }
            return p;
        }

        private static ECPoint DecompressPoint(int yTilde, BigInteger X1, ECCurve curve)
        {
            ECFieldElement x = new ECFieldElement(X1, curve);
            ECFieldElement alpha = x * (x.Square() + curve.A) + curve.B;
            ECFieldElement beta = alpha.Sqrt();

            //
            // if we can't find a sqrt we haven't got a point on the
            // curve - run!
            //
            if (beta == null)
                throw new ArithmeticException("Invalid point compression");

            BigInteger betaValue = beta.Value;
            int bit0 = betaValue.IsEven ? 0 : 1;

            if (bit0 != yTilde)
            {
                // Use the other root
                beta = new ECFieldElement(curve.Q - betaValue, curve);
            }

            return new ECPoint(x, beta, curve);
        }

        //void ISerializable.Deserialize(BinaryReader reader)
        //{
        //    ECPoint p = DeserializeFrom(reader, Curve);
        //    X = p.X;
        //    Y = p.Y;
        //}

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="reader">数据来源</param>
        /// <param name="curve">椭圆曲线参数</param>
        /// <returns></returns>
        public static ECPoint DeserializeFrom(BinaryReader reader, ECCurve curve)
        {
            int expectedLength = (curve.Q.GetBitLength() + 7) / 8;
            byte[] buffer = new byte[1 + expectedLength * 2];
            buffer[0] = reader.ReadByte();
            switch (buffer[0])
            {
                case 0x00:
                    return curve.Infinity;
                case 0x02:
                case 0x03:
                    reader.Read(buffer, 1, expectedLength);
                    return DecodePoint(buffer.Take(1 + expectedLength).ToArray(), curve);
                case 0x04:
                case 0x06:
                case 0x07:
                    reader.Read(buffer, 1, expectedLength * 2);
                    return DecodePoint(buffer, curve);
                default:
                    throw new FormatException("Invalid point encoding " + buffer[0]);
            }
        }

        /// <summary>
        /// 将对象编码到字节数组
        /// </summary>
        /// <param name="commpressed">是否为压缩格式的编码</param>
        /// <returns>返回编码后的字节数组</returns>
        public byte[] EncodePoint(bool commpressed)
        {
            if (IsInfinity) return new byte[1];
            byte[] data;
            if (commpressed)
            {
                data = new byte[33];
            }
            else
            {
                data = new byte[65];
                byte[] yBytes = Y.Value.ToByteArray().Reverse().ToArray();
                Buffer.BlockCopy(yBytes, 0, data, 65 - yBytes.Length, yBytes.Length);
            }
            byte[] xBytes = X.Value.ToByteArray().Reverse().ToArray();
            Buffer.BlockCopy(xBytes, 0, data, 33 - xBytes.Length, xBytes.Length);
            data[0] = commpressed ? Y.Value.IsEven ? (byte)0x02 : (byte)0x03 : (byte)0x04;
            return data;
        }

        /// <summary>
        /// 比较与另一个对象是否相等
        /// </summary>
        /// <param name="other">另一个对象</param>
        /// <returns>返回比较的结果</returns>
        public bool Equals(ECPoint other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (ReferenceEquals(null, other)) return false;
            if (IsInfinity && other.IsInfinity) return true;
            if (IsInfinity || other.IsInfinity) return false;
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        /// <summary>
        /// 比较与另一个对象是否相等
        /// </summary>
        /// <param name="obj">另一个对象</param>
        /// <returns>返回比较的结果</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as ECPoint);
        }

        /// <summary>   
        /// 从指定的字节数组中解析出公钥，这个字节数组可以是任意形式的公钥编码、或者包含私钥的内容
        /// </summary>
        /// <param name="pubkey">要解析的字节数组</param>
        /// <param name="curve">椭圆曲线参数</param>
        /// <returns>返回解析出的公钥</returns>
        public static ECPoint FromBytes(byte[] pubkey, ECCurve curve)
        {
            switch (pubkey.Length)
            {
                case 33:
                case 65:
                    return DecodePoint(pubkey, curve);
                case 64:
                case 72:
                    return DecodePoint(new byte[] { 0x04 }.Concat(pubkey.Skip(pubkey.Length - 64)).ToArray(), curve);
                case 96:
                case 104:
                    return DecodePoint(new byte[] { 0x04 }.Concat(pubkey.Skip(pubkey.Length - 96).Take(64)).ToArray(), curve);
                default:
                    throw new FormatException();
            }
        }

        /// <summary>
        /// 获取HashCode
        /// </summary>
        /// <returns>返回HashCode</returns>
        public override int GetHashCode()
        {
            return X.GetHashCode() + Y.GetHashCode();
        }

        internal static ECPoint Multiply(ECPoint p, BigInteger k)
        {
            // floor(log2(k))
            int m = k.GetBitLength();

            // width of the Window NAF
            sbyte width;

            // Required length of precomputation array
            int reqPreCompLen;

            // Determine optimal width and corresponding length of precomputation
            // array based on literature values
            if (m < 13)
            {
                width = 2;
                reqPreCompLen = 1;
            }
            else if (m < 41)
            {
                width = 3;
                reqPreCompLen = 2;
            }
            else if (m < 121)
            {
                width = 4;
                reqPreCompLen = 4;
            }
            else if (m < 337)
            {
                width = 5;
                reqPreCompLen = 8;
            }
            else if (m < 897)
            {
                width = 6;
                reqPreCompLen = 16;
            }
            else if (m < 2305)
            {
                width = 7;
                reqPreCompLen = 32;
            }
            else
            {
                width = 8;
                reqPreCompLen = 127;
            }

            // The length of the precomputation array
            int preCompLen = 1;

            ECPoint[] preComp = preComp = new ECPoint[] { p };
            ECPoint twiceP = p.Twice();

            if (preCompLen < reqPreCompLen)
            {
                // Precomputation array must be made bigger, copy existing preComp
                // array into the larger new preComp array
                ECPoint[] oldPreComp = preComp;
                preComp = new ECPoint[reqPreCompLen];
                Array.Copy(oldPreComp, 0, preComp, 0, preCompLen);

                for (int i = preCompLen; i < reqPreCompLen; i++)
                {
                    // Compute the new ECPoints for the precomputation array.
                    // The values 1, 3, 5, ..., 2^(width-1)-1 times p are
                    // computed
                    preComp[i] = twiceP + preComp[i - 1];
                }
            }

            // Compute the Window NAF of the desired width
            sbyte[] wnaf = WindowNaf(width, k);
            int l = wnaf.Length;

            // Apply the Window NAF to p using the precomputed ECPoint values.
            ECPoint q = p.Curve.Infinity;
            for (int i = l - 1; i >= 0; i--)
            {
                q = q.Twice();

                if (wnaf[i] != 0)
                {
                    if (wnaf[i] > 0)
                    {
                        q += preComp[(wnaf[i] - 1) / 2];
                    }
                    else
                    {
                        // wnaf[i] < 0
                        q -= preComp[(-wnaf[i] - 1) / 2];
                    }
                }
            }

            return q;
        }

        public static ECPoint Parse(string value, ECCurve curve)
        {
            return DecodePoint(Helper.HexString2Bytes(value), curve);
        }

        //void ISerializable.Serialize(BinaryWriter writer)
        //{
        //    writer.Write(EncodePoint(true));
        //}

        public override string ToString()
        {
            return Helper.Bytes2HexString(EncodePoint(true));
        }

        internal ECPoint Twice()
        {
            if (this.IsInfinity)
                return this;
            if (this.Y.Value.Sign == 0)
                return Curve.Infinity;
            ECFieldElement TWO = new ECFieldElement(2, Curve);
            ECFieldElement THREE = new ECFieldElement(3, Curve);
            ECFieldElement gamma = (this.X.Square() * THREE + Curve.A) / (Y * TWO);
            ECFieldElement x3 = gamma.Square() - this.X * TWO;
            ECFieldElement y3 = gamma * (this.X - x3) - this.Y;
            return new ECPoint(x3, y3, Curve);
        }

        private static sbyte[] WindowNaf(sbyte width, BigInteger k)
        {
            sbyte[] wnaf = new sbyte[k.GetBitLength() + 1];
            short pow2wB = (short)(1 << width);
            int i = 0;
            int length = 0;
            while (k.Sign > 0)
            {
                if (!k.IsEven)
                {
                    BigInteger remainder = k % pow2wB;
                    if (remainder.TestBit(width - 1))
                    {
                        wnaf[i] = (sbyte)(remainder - pow2wB);
                    }
                    else
                    {
                        wnaf[i] = (sbyte)remainder;
                    }
                    k -= wnaf[i];
                    length = i;
                }
                else
                {
                    wnaf[i] = 0;
                }
                k >>= 1;
                i++;
            }
            length++;
            sbyte[] wnafShort = new sbyte[length];
            Array.Copy(wnaf, 0, wnafShort, 0, length);
            return wnafShort;
        }

        public static ECPoint operator -(ECPoint x)
        {
            return new ECPoint(x.X, -x.Y, x.Curve);
        }

        public static ECPoint operator *(ECPoint p, byte[] n)
        {
            if (p == null || n == null)
                throw new ArgumentNullException();
            if (n.Length != 32)
                throw new ArgumentException();
            if (p.IsInfinity)
                return p;
            //BigInteger的内存无法被保护，可能会有安全隐患。此处的k需要重写一个SecureBigInteger类来代替
            BigInteger k = new BigInteger(n.Reverse().Concat(new byte[1]).ToArray());
            if (k.Sign == 0)
                return p.Curve.Infinity;
            return Multiply(p, k);
        }

        public static ECPoint operator +(ECPoint x, ECPoint y)
        {
            if (x.IsInfinity)
                return y;
            if (y.IsInfinity)
                return x;
            if (x.X.Equals(y.X))
            {
                if (x.Y.Equals(y.Y))
                    return x.Twice();
                System.Diagnostics.Debug.Assert(x.Y.Equals(-y.Y));
                return x.Curve.Infinity;
            }
            ECFieldElement gamma = (y.Y - x.Y) / (y.X - x.X);
            ECFieldElement x3 = gamma.Square() - x.X - y.X;
            ECFieldElement y3 = gamma * (x.X - x3) - x.Y;
            return new ECPoint(x3, y3, x.Curve);
        }

        public static ECPoint operator -(ECPoint x, ECPoint y)
        {
            if (y.IsInfinity)
                return x;
            return x + (-y);
        }
    }

    internal class ECFieldElement : IComparable<ECFieldElement>, IEquatable<ECFieldElement>
    {
        internal readonly BigInteger Value;
        private readonly ECCurve curve;

        public ECFieldElement(BigInteger value, ECCurve curve)
        {
            if (value >= curve.Q)
                throw new ArgumentException("x value too large in field element");
            this.Value = value;
            this.curve = curve;
        }

        public int CompareTo(ECFieldElement other)
        {
            if (ReferenceEquals(this, other)) return 0;
            return Value.CompareTo(other.Value);
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
                return true;

            ECFieldElement other = obj as ECFieldElement;

            if (other == null)
                return false;

            return Equals(other);
        }

        public bool Equals(ECFieldElement other)
        {
            return Value.Equals(other.Value);
        }

        private static BigInteger[] FastLucasSequence(BigInteger p, BigInteger P, BigInteger Q, BigInteger k)
        {
            int n = Helper.GetBitLength(k);// k.GetBitLength();
            int s = Helper.GetLowestSetBit(k);


            System.Diagnostics.Debug.Assert(Helper.TestBit(k, s));

            BigInteger Uh = 1;
            BigInteger Vl = 2;
            BigInteger Vh = P;
            BigInteger Ql = 1;
            BigInteger Qh = 1;

            for (int j = n - 1; j >= s + 1; --j)
            {
                Ql = Helper.Mod((Ql * Qh), p);

                if (Helper.TestBit(k, j))
                {
                    Qh = (Ql * Q).Mod(p);
                    Uh = (Uh * Vh).Mod(p);
                    Vl = (Vh * Vl - P * Ql).Mod(p);
                    Vh = ((Vh * Vh) - (Qh << 1)).Mod(p);
                }
                else
                {
                    Qh = Ql;
                    Uh = (Uh * Vl - Ql).Mod(p);
                    Vh = (Vh * Vl - P * Ql).Mod(p);
                    Vl = ((Vl * Vl) - (Ql << 1)).Mod(p);
                }
            }

            Ql = (Ql * Qh).Mod(p);
            Qh = (Ql * Q).Mod(p);
            Uh = (Uh * Vl - Ql).Mod(p);
            Vl = (Vh * Vl - P * Ql).Mod(p);
            Ql = (Ql * Qh).Mod(p);

            for (int j = 1; j <= s; ++j)
            {
                Uh = Uh * Vl * p;
                Vl = ((Vl * Vl) - (Ql << 1)).Mod(p);
                Ql = (Ql * Ql).Mod(p);
            }

            return new BigInteger[] { Uh, Vl };
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public ECFieldElement Sqrt()
        {
            if (curve.Q.TestBit(1))
            {
                ECFieldElement z = new ECFieldElement(BigInteger.ModPow(Value, (curve.Q >> 2) + 1, curve.Q), curve);
                return z.Square().Equals(this) ? z : null;
            }
            BigInteger qMinusOne = curve.Q - 1;
            BigInteger legendreExponent = qMinusOne >> 1;
            if (BigInteger.ModPow(Value, legendreExponent, curve.Q) != 1)
                return null;
            BigInteger u = qMinusOne >> 2;
            BigInteger k = (u << 1) + 1;
            BigInteger Q = this.Value;
            BigInteger fourQ = (Q << 2).Mod(curve.Q);
            BigInteger U, V;
            do
            {
                Random rand = new Random();
                BigInteger P;
                do
                {
                    P = rand.NextBigInteger(curve.Q.GetBitLength());
                }
                while (P >= curve.Q || BigInteger.ModPow(P * P - fourQ, legendreExponent, curve.Q) != qMinusOne);
                BigInteger[] result = FastLucasSequence(curve.Q, P, Q, k);
                U = result[0];
                V = result[1];
                if ((V * V).Mod(curve.Q) == fourQ)
                {
                    if (V.TestBit(0))
                    {
                        V += curve.Q;
                    }
                    V >>= 1;
                    System.Diagnostics.Debug.Assert((V * V).Mod(curve.Q) == Value);
                    return new ECFieldElement(V, curve);
                }
            }
            while (U.Equals(BigInteger.One) || U.Equals(qMinusOne));
            return null;
        }

        public ECFieldElement Square()
        {
            return new ECFieldElement((Value * Value).Mod(curve.Q), curve);
        }

        public byte[] ToByteArray()
        {
            byte[] data = Value.ToByteArray();
            if (data.Length == 32)
                return data.Reverse().ToArray();
            if (data.Length > 32)
                return data.Take(32).Reverse().ToArray();
            return Enumerable.Repeat<byte>(0, 32 - data.Length).Concat(data.Reverse()).ToArray();
        }

        public static ECFieldElement operator -(ECFieldElement x)
        {
            return new ECFieldElement((-x.Value).Mod(x.curve.Q), x.curve);
        }

        public static ECFieldElement operator *(ECFieldElement x, ECFieldElement y)
        {
            return new ECFieldElement((x.Value * y.Value).Mod(x.curve.Q), x.curve);
        }

        public static ECFieldElement operator /(ECFieldElement x, ECFieldElement y)
        {
            return new ECFieldElement((x.Value * y.Value.ModInverse(x.curve.Q)).Mod(x.curve.Q), x.curve);
        }

        public static ECFieldElement operator +(ECFieldElement x, ECFieldElement y)
        {
            return new ECFieldElement((x.Value + y.Value).Mod(x.curve.Q), x.curve);
        }

        public static ECFieldElement operator -(ECFieldElement x, ECFieldElement y)
        {
            return new ECFieldElement((x.Value - y.Value).Mod(x.curve.Q), x.curve);
        }
    }
}
