using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Hosts
{
	public class HostsItem
	{
		public HostsItem(string ip, string hosts, string comment = "")
		{
			Enabled = true;
			IP = ip;
			Aliases = new HostAliases(hosts);
			Comment = comment;
			Valid = true;
		}

		public HostsItem(string ip, string[] hosts, string comment = "")
		{
			Enabled = true;
			IP = ip;
			Aliases = new HostAliases(hosts);
			Comment = comment;
			Valid = true;
		}

		public HostsItem(string text, bool resetFormat = false)
		{
			ResetFormat = resetFormat;
			Parse(text);
		}

		public bool Valid { get; protected set; }

		private bool enabled;
		public bool Enabled
		{
			get { return enabled; }
			set { if (enabled != value) { enabled = value; changed = true; } }
		}

		private bool hidden;
		public bool Hidden
		{
			get { return hidden; }
			set { if (hidden != value) { hidden = value; changed = true; } }
		}

		private string ip;
		public string IP
		{
			get { return ip; }
			set 
			{
				if (!HostsHelper.CheckIP(value)) throw new FormatException(String.Format("Invalid IP address '{0}'", value));
				if (ip != value) 
				{
					ip = value; 
					changed = true; 
				} 
			}
		}

		public IpType IpType 
		{
			get { return (Valid) ? HostsHelper.GetIpType(ip) : IpType.Invalid; }
		}

		public HostAliases Aliases { get; protected set; }
		public bool Deleted { get { return Valid && (Aliases == null || Aliases.Count == 0); } }
		public string Host
		{
			get { return Aliases[0]; }
			set 
			{
				if (Aliases[0] != value)
				{
					Aliases[0] = value;
					changed = true;
				} 
			}
		}

		private string comment;
		public string Comment
		{
			get { return comment; }
			set { if (comment != value) { comment = value; changed = true; } }
		}

		private static Regex HostRowPattern = new Regex(@"^#?\s*"
				+ @"(?<ip>\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}|[0-9a-f:]+)\s+"
				+ @"(?<hosts>(([a-z0-9][-_a-z0-9]*\.?)+\s*)+)"
				+ @"(?:#\s*(?<comment>.*?)\s*)?$",
				RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
		private string text;
		private bool changed;
		public string Text { get { return ToString(); } set { Parse(value); } }

		public bool Parse(string value)
		{
			text = value;
			changed = false;
			try
			{
				var match = HostRowPattern.Match(value);
				if (!match.Success) throw new FormatException();
				enabled = value[0] != '#';
				ip = match.Groups["ip"].Value;
				if (!HostsHelper.CheckIP(ip)) throw new FormatException();
				Aliases = new HostAliases(match.Groups["hosts"].Value);
				comment = match.Groups["comment"].Value;
				hidden = false;
				if (!String.IsNullOrEmpty(comment))
				{
					hidden = comment[0] == '!';
					if (hidden) comment = comment.Substring(1).Trim();
				}
				Valid = true;
			}
			catch
			{
				enabled = false;
				hidden = false;
				ip = null;
				Aliases = null;
				comment = null;
				Valid = false;
			}
			return Valid;
		}

		public bool ResetFormat { get; set; }

		public override string ToString()
		{
			return ToString(true);
		}

		public string ToString(bool idn)
		{
			if (Valid && (ResetFormat || changed || String.IsNullOrEmpty(text)))
			{
				string result = String.Format("{0,-18} {1,-31} ", (Enabled ? "" : "# ") + IP, Aliases.ToString(idn));
				if (!String.IsNullOrEmpty(Comment) || Hidden) result += "#";
				if (Hidden) result += "!"; else result += " ";
				if (!String.IsNullOrEmpty(Comment)) result += Comment;
				return result.Trim();
			}
			else
			{
				if(!ResetFormat) return text;
				string result = text.Trim();
				if (result.Length > 0 && result[0] != '#') result = "# " + result;
				return result;
			}
		}
	}
}
