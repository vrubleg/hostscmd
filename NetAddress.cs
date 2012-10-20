using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Hosts
{
	public class NetAddress
	{
		private string IP;

		public bool IPv4 { get; protected set; }
		public bool IPv6 { get; protected set; }

		public NetAddress(string ip)
		{
			if (ip == null) throw new ArgumentNullException();
			try
			{
				switch (IPAddress.Parse(ip).AddressFamily)
				{
					case AddressFamily.InterNetwork:
						IPv4 = true;
						IPv6 = false;
						IP = IPAddress.Parse(ip).ToString();
					break;
					case AddressFamily.InterNetworkV6:
						IPv4 = false;
						IPv6 = true;
						IP = ip.ToLower(); // TODO: Normalize IPv6 address to compact format
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
}
