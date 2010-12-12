using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Hosts
{
	public class HostsItem
	{
		public bool Valid { get; protected set; }
		public bool Enabled { get; set; }
		public bool Hidden { get; set; }
		public NetAddress IP { get; set; }
		public HostAliases Aliases { get; set; }
		public string Comment { get; set; }
		public bool ResetFormat { get; set; }
		public bool Deleted { get { return Valid && (Aliases == null || Aliases.Count == 0); } }

		public HostsItem(string text, bool resetFormat = false)
		{
			ResetFormat = resetFormat;
			Parse(text);
		}

		public HostsItem(string ip, string hosts, string comment = "")
		{
			Enabled = true;
			IP = new NetAddress(ip);
			Aliases = new HostAliases(hosts);
			Comment = comment;
			Valid = true;
		}

		public HostsItem(string ip, string[] hosts, string comment = "")
		{
			Enabled = true;
			IP = new NetAddress(ip);
			Aliases = new HostAliases(hosts);
			Comment = comment;
			Valid = true;
		}

		public HostsItem(NetAddress ip, HostName host, string comment = "")
		{
			Enabled = true;
			IP = ip;
			Aliases = new HostAliases(host);
			Comment = comment;
			Valid = true;
		}

		public HostsItem(NetAddress ip, HostAliases hosts, string comment = "")
		{
			Enabled = true;
			IP = ip;
			Aliases = hosts;
			Comment = comment;
			Valid = true;
		}

		private static Regex HostRowPattern = new Regex(@"^#?\s*"
				+ @"(?<ip>\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}|[0-9a-f:]+)\s+"
				+ @"(?<hosts>(([a-z0-9][-_a-z0-9]*\.?)+\s*)+)"
				+ @"(?:#\s*(?<comment>.*?)\s*)?$",
				RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

		private string ParsedString;
		protected ulong ParsedHash;
		public string RawString 
		{
			get 
			{
				bool format = ResetFormat || String.IsNullOrEmpty(ParsedString) || ParsedHash != HalfMD5.ComputeHash(ToString());
				return format ? ToString(false) : ParsedString;
			}
			set	
			{
				Parse(value);
			}
		}

		public bool Parse(string value)
		{
			ParsedString = value;
			try
			{
				var match = HostRowPattern.Match(value);
				if (!match.Success) throw new FormatException();
				Enabled = value[0] != '#';
				IP = new NetAddress(match.Groups["ip"].Value);
				Aliases = new HostAliases(match.Groups["hosts"].Value);
				Comment = match.Groups["comment"].Value;
				Hidden = false;
				if (!String.IsNullOrEmpty(Comment))
				{
					Hidden = Comment[0] == '!';
					if (Hidden) Comment = Comment.Substring(1).Trim();
				}
				Valid = true;
			}
			catch
			{
				Enabled = false;
				Hidden = false;
				IP = null;
				Aliases = null;
				Comment = null;
				Valid = false;
			}
			ParsedHash = HalfMD5.ComputeHash(ToString());
			return Valid;
		}

		public override string ToString()
		{
			return ToString(true);
		}

		public string ToString(bool idn)
		{
			if (Valid)
			{
				string result = String.Format("{0,-18} {1,-31} ", (Enabled ? "" : "# ") + IP.ToString(), Aliases.ToString(idn));
				if (!String.IsNullOrEmpty(Comment) || Hidden) result += "#";
				if (Hidden) result += "!"; else result += " ";
				if (!String.IsNullOrEmpty(Comment)) result += Comment;
				return result.Trim();
			}
			else
			{
				string result = ParsedString.Trim();
				if (result.Length > 0 && result[0] != '#') result = "# " + result;
				return result;
			}
		}
	}
}
