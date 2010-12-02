using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Hosts
{
	public class HostLine
	{
		static private Regex HostRowPattern = new Regex(@"^#?\s*"
				+ @"(?<ip>\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}|[0-9a-f:]+)\s+"
				+ @"(?<host>([-_a-z0-9]+\.?)+)\s*"
				+ @"(?:#\s*(?<comment>.*?)\s*)?$",
				RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

		public HostLine(string ip, string host)
		{
			Enabled = true;
			IP = ip;
			Host = host;
			Comment = "";
			Valid = true;
		}

		public HostLine(string text, bool resetFormat = false)
		{
			Text = text;
			if (resetFormat) ResetFormat();
		}

		public HostLine(bool enabled, string ip, string host, string comment)
		{
			Enabled = enabled;
			IP = ip;
			Host = host;
			Comment = comment;
			Valid = true;
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
			set { if (ip != value) { ip = value; changed = true; } }
		}

		private string host;
		public string Host
		{
			get { return host; }
			set { if (host != value) { host = value; changed = true; } }
		}

		private string comment;
		public string Comment
		{
			get { return comment; }
			set { if (comment != value) { comment = value; changed = true; } }
		}

		public void ResetFormat()
		{
			if (Valid)
			{
				text = null;
				changed = true;
			}
		}

		private string text;
		private bool changed;
		public string Text
		{
			get
			{
				if (Valid && (changed || String.IsNullOrEmpty(text)))
				{
					string result = String.Format("{0,-18} {1,-31} ", (Enabled ? "" : "# ") + IP, Host);
					if (!String.IsNullOrEmpty(Comment) || Hidden) result += "#";
					if (Hidden) result += "!"; else result += " ";
					if (!String.IsNullOrEmpty(Comment)) result += Comment;
					return result.Trim();
				}
				else return text;
			}
			set
			{
				text = value;
				changed = false;
				try
				{
					var match = HostRowPattern.Match(value);
					if (!match.Success) throw new FormatException();
					enabled = value[0] != '#';
					ip = match.Groups["ip"].Value;
					if (!HostsEditor.CheckIP(ip)) throw new FormatException();
					host = match.Groups["host"].Value;
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
					host = null;
					comment = null;
					Valid = false;
				}
			}
		}

		public override string ToString()
		{
			return Text;
		}
	}
}
