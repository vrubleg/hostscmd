using System;
using System.Text;
using System.Security.Cryptography;

static class HalfMD5
{
	private static MD5 md5 = new MD5CryptoServiceProvider();

	private static byte[] MD5(byte[] Data)
	{
		return md5.ComputeHash(Data);
	}

	private static byte[] MD5(string Text)
	{
		return md5.ComputeHash(Encoding.UTF8.GetBytes(Text));
	}

	private static ulong ReverseBytes(ulong value)
	{
		return (value & 0x00000000000000FFUL) << 56 | (value & 0x000000000000FF00UL) << 40 |
			   (value & 0x0000000000FF0000UL) << 24 | (value & 0x00000000FF000000UL) << 8 |
			   (value & 0x000000FF00000000UL) >> 8  | (value & 0x0000FF0000000000UL) >> 24 |
			   (value & 0x00FF000000000000UL) >> 40 | (value & 0xFF00000000000000UL) >> 56;
	}

	public static ulong ComputeHash(byte[] Data)
	{
		return ReverseBytes(BitConverter.ToUInt64(MD5(Data), 0));
	}

	public static ulong ComputeHash(string Text)
	{
		return ReverseBytes(BitConverter.ToUInt64(MD5(Text), 0));
	}
}