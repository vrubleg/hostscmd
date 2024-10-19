using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace Hosts
{
	public class HostName
	{
		private static IdnMapping IdnMapping = new IdnMapping();

		public string Ascii { get; protected set; }
		public string Unicode { get; protected set; }
		public bool Idn { get; protected set; }

		public HostName(string host, bool idn = true)
		{
			if (host == null) throw new ArgumentNullException();
			try
			{
				host = host.ToLower();
				if (Uri.CheckHostName(host) != UriHostNameType.Dns) { throw new Exception(); }
				if (host.StartsWith("xn--") || host.Contains(".xn--"))
				{
					Unicode = IdnMapping.GetUnicode(host);
					Ascii = IdnMapping.GetAscii(Unicode);
				}
				else
				{
					Ascii = IdnMapping.GetAscii(host);
					Unicode = IdnMapping.GetUnicode(Ascii);
				}
				Idn = idn && Ascii != Unicode;
			}
			catch (Exception e)
			{
				throw new FormatException(String.Format("Invalid host '{0}'", host), e);
			}
		}

		public static HostName TryCreate(string host, bool idn = true)
		{
			try	{ return new HostName(host, idn); }
			catch { return null; }
		}

		public static implicit operator HostName(string host)
		{
			return new HostName(host);
		}

		public static implicit operator string(HostName host)
		{
			return host.ToString();
		}

		public override string ToString()
		{
			return Idn ? Unicode : Ascii;
		}

		public override int GetHashCode()
		{
			return ToString().GetHashCode();
		}

		public bool Equals(HostName host)
		{
			if ((object)host == null) return false;
			return host.Unicode == this.Unicode;
		}

		public bool Equals(string host)
		{
			return Equals(HostName.TryCreate(host));
		}

		public override bool Equals(object obj)
		{
			if (obj == null) return false;
			if (obj is string) return Equals((string)obj);
			if (obj is HostName) return Equals((HostName)obj);
			return false;
		}

		public static bool operator ==(HostName hn1, HostName hn2)
		{
			return Object.ReferenceEquals(hn1, hn2) || hn1.Equals(hn2);
		}

		public static bool operator !=(HostName hn1, HostName hn2)
		{
			return !(Object.ReferenceEquals(hn1, hn2) || hn1.Equals(hn2));
		}
	}
}
