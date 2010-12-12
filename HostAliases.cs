using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hosts
{
	public class HostAliases : List<HostName>
	{
		public HostAliases() { }

		public HostAliases(string[] hosts)
		{
			Add(hosts);
		}

		public HostAliases(string line)
		{
			Add(line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));
		}

		public new bool Add(HostName host)
		{
			if (Contains(host)) return false;
			RemoveAll(item => String.IsNullOrEmpty(item));
			base.Add(host);
			return true;
		}

		public void Add(HostName[] hosts)
		{
			foreach (HostName host in hosts) Add(host);
		}

		public bool Add(string value)
		{
			return Add(new HostName(value));
		}

		public void Add(string[] hosts)
		{
			foreach (string host in hosts) Add(host);
		}

		public new HostName this[int index]
		{
			get { if (index == 0 && Count == 0) return ""; else return base[index]; }
			set	{ if (index == 0 && Count == 0) Add(value); else base[index] = value; }
		}

		public override string ToString()
		{
			return ToString(true);
		}

		public string ToString(bool idn)
		{
			List<string> hosts = new List<string>(this.Count);
			foreach (HostName hn in this) hosts.Add(idn ? hn.Unicode : hn.Ascii);
			return String.Join("  ", hosts.ToArray());
		}
	}
}
