using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Hosts;

public enum NetAddressType { None, IPv4, IPv6 };

public class NetAddress
{
	private string IP;

	public NetAddressType Type { get; protected set; }

	/// <summary>
	/// Normalize IPv6 address to compact format
	/// </summary>
	/// <param name="ip">IPv6 string</param>
	/// <returns>Normalized IPv6 string</returns>
	protected string NormalizeIPv6(string ip)
	{
		var hex = ip.Split(':');
		var dec = new uint[8];
		var gap = false;

		// Parse "head"
		for (var i = 0; i < hex.Length; i++)
		{
			if (String.IsNullOrEmpty(hex[i]))
			{
				gap = true;
				break;
			}
			dec[i] = Convert.ToUInt32(hex[i], 16);
		}

		// Parse "tail"
		if (gap) for (var i = 0; i < hex.Length; i++)
		{
			if (String.IsNullOrEmpty(hex[hex.Length - i - 1]))
			{
				break;
			}
			dec[dec.Length - i - 1] = Convert.ToUInt32(hex[hex.Length - i - 1], 16);
		}


		// Find longest zeroes part
		var max_offset = -1;
		var max_length = 0;
		var offset = -1;
		var length = 0;

		for (var i = 0; i < dec.Length; i++)
		{
			if (dec[i] == 0)
			{
				if (offset == -1)
				{
					offset = i;
					length = 1;
				}
				else
				{
					length++;
				}
			}
			else if (offset > -1)
			{
				if (length > max_length)
				{
					max_offset = offset;
					max_length = length;
				}
				offset = -1;
				length = 0;
			}
		}
		if (length > max_length)
		{
			max_offset = offset;
			max_length = length;
		}

		// Ignore one zero
		if (max_length == 1)
		{
			max_length = 0;
			max_offset = -1;
		}

		// Format normalized IPv6 address
		ip = "";
		for (int i = 0; i < dec.Length; i++)
		{
			if (i >= max_offset && i < (max_offset + max_length))
			{
				if (i == max_offset)
				{
					ip += "::";
				}
				continue;
			}

			if (ip.Length > 0 && !ip.EndsWith(":"))
			{
				ip += ":";
			}

			ip += dec[i].ToString("x");
		}

		return ip.ToLower();
	}

	public NetAddress(string ip)
	{
		if (ip == null) throw new ArgumentNullException();
		try
		{
			var parsed = IPAddress.Parse(ip);
			switch (parsed.AddressFamily)
			{

				case AddressFamily.InterNetwork:
					Type = NetAddressType.IPv4;
					IP = parsed.ToString();
				break;

				case AddressFamily.InterNetworkV6:
					Type = NetAddressType.IPv6;
					IP = NormalizeIPv6(ip);
				break;

				default: throw new Exception();
			}
		}
		catch
		{
			throw new FormatException(String.Format("Invalid IP address '{0}'", IP));
		}
	}

	public static NetAddress TryCreate(string ip)
	{
		try { return new NetAddress(ip); }
		catch { return null; }
	}

	public static implicit operator NetAddress(string ip)
	{
		return new NetAddress(ip);
	}

	public static implicit operator string(NetAddress ip)
	{
		return ip.ToString();
	}

	public override string ToString()
	{
		return IP;
	}

	public override int GetHashCode()
	{
		return ToString().GetHashCode();
	}

	public bool Equals(NetAddress ip)
	{
		if ((object)ip == null) return false;
		return ip.IP == this.IP;
	}

	public bool Equals(string ip)
	{
		return Equals(NetAddress.TryCreate(ip));
	}

	public override bool Equals(object obj)
	{
		if (obj == null) return false;
		if (obj is string) return Equals((string)obj);
		if (obj is NetAddress) return Equals((NetAddress)obj);
		return false;
	}

	public static bool operator ==(NetAddress na1, NetAddress na2)
	{
		return Object.ReferenceEquals(na1, na2) || na1.Equals(na2);
	}

	public static bool operator !=(NetAddress na1, NetAddress na2)
	{
		return !(Object.ReferenceEquals(na1, na2) || na1.Equals(na2));
	}
}
