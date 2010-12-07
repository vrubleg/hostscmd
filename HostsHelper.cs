using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Hosts
{
	enum IpType { Invalid, IPv4, IPv6 }

	static class HostsHelper
	{
		static public IpType GetIpType(string ip)
		{
			IPAddress ipa;
			if(!IPAddress.TryParse(ip, out ipa)) return IpType.Invalid;
			switch (ipa.AddressFamily)
			{
				case AddressFamily.InterNetwork:   return IpType.IPv4;
				case AddressFamily.InterNetworkV6: return IpType.IPv6;
				default: return IpType.Invalid;
			}
		}

		static public bool CheckIP(string ip)
		{
			return GetIpType(ip) != IpType.Invalid;
		}

		static private Regex HostPattern = new Regex(@"^([-_a-z0-9]+\.?)+$", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
		static public bool CheckHost(string host)
		{
			return HostPattern.IsMatch(host);
		}
	}
}
