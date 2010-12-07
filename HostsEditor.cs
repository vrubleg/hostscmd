using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Hosts
{
	public class HostsEditor : HostsList
	{
		public HostsEditor(string fileName) : base (fileName) { }

		public HostsItem Get(string host)
		{
			return this.Find(item => item.Valid && item.Host == host);
		}

		public List<HostsItem> GetMatch(string pattern)
		{
			pattern = "^" + pattern.Replace(".", @"\.").Replace("?", ".").Replace("*", ".*") + "$";
			Regex regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
			return this.FindAll(item => item.Valid && regex.IsMatch(item.Host));
		}

		public bool Exists(string host)
		{
			return (Get(host) != null);
		}

		public void Add(string host, string ip, string comment = "")
		{
			this.Add(new HostsItem(ip, host, comment));
		}

		public void Remove(string host)
		{
			HostsItem Found = Get(host);
			if (Found != null)
			{
				this.Remove(Found);
			}
		}
	}
}
