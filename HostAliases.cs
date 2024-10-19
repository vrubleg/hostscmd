using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hosts;

public class HostAliases : List<HostName>
{
	public HostAliases()					{ }
	public HostAliases(string line)			{ Set(line); }
	public HostAliases(string[] hosts)		{ Set(hosts); }
	public HostAliases(HostName host)		{ Set(host); }
	public HostAliases(HostName[] hosts)	{ Set(hosts); }
	public HostAliases(HostAliases hosts)	{ Set(hosts); }

	public int Set(string line)			{ Clear(); return Add(line); }
	public int Set(string[] hosts)		{ Clear(); return Add(hosts); }
	public int Set(HostName host)		{ Clear(); return Add(host); }
	public int Set(HostName[] hosts)	{ Clear(); return Add(hosts); }
	public int Set(HostAliases hosts)	{ Clear(); return Add(hosts); }

	public new int Add(HostName host)
	{
		if (Contains(host)) return 0;
		RemoveAll(item => String.IsNullOrWhiteSpace(item));
		base.Add(host);
		return 1;
	}

	public int Add(HostName[] hosts)
	{
		int result = 0;
		foreach (HostName host in hosts) result += Add(host);
		return result;
	}

	public int Add(string line)
	{
		return Add(line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));
	}

	public int Add(string[] hosts)
	{
		int result = 0;
		foreach (string host in hosts) result += Add(new HostName(host));
		return result;
	}

	public int Add(HostAliases hosts)
	{
		return Add(hosts.ToArray());
	}

	public override string ToString()
	{
		return ToString(true);
	}

	public string ToString(bool idn)
	{
		List<string> hosts = new List<string>(this.Count);
		foreach (HostName hn in this) hosts.Add(idn ? hn.Unicode : hn.Ascii);
		return String.Join(" ", hosts.ToArray());
	}

	public bool IsMatch(WildcardPattern pattern)
	{
		return this.Exists(item => pattern.IsMatch(item.Unicode));
	}

	public bool ContainsAny(List<HostName> hosts)
	{
		foreach (HostName host in hosts)
		{
			if (this.Contains(host)) { return true; }
		}
		return false;
	}

	public int RemoveAll(List<HostName> hosts)
	{
		return this.RemoveAll(host => hosts.Contains(host));
	}
}
