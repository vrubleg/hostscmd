using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Net;

namespace Hosts
{
	public class HostLine
	{
		static private Regex HostRowPattern = new Regex(@"^#?\s*"
				+ @"(?<ip>\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}|[0-9a-f:]+)\s+"
				+ @"(?<host>([-_a-z0-9]+\.?)+)\s*"
				+ @"(?:#\s*(?<comment>.*?)\s*)?$",
				RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

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

	public class HostsEditor
	{
		static public bool CheckIP(string ip)
		{
			IPAddress tempip;
			return IPAddress.TryParse(ip, out tempip);
		}

		static private Regex HostPattern = new Regex(@"^([-_a-z0-9]+\.?)+$", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
		static public bool CheckHost(string host)
		{
			return HostPattern.IsMatch(host);
		}

		public string FileName;
		public List<HostLine> Lines = new List<HostLine>();

		public HostsEditor(string fileName) 
		{
			if (fileName == null) throw new ArgumentNullException("fileName");
			FileName = fileName;
		}

		public void Clear()
		{
			Lines.Clear();
		}

		public void Load(/*bool validOnly = false, bool resetFormat = false*/)
		{
			Clear();
			string[] lines = File.ReadAllLines(FileName);
			foreach (string line in lines)
			{
				HostLine item = new HostLine(line/*, resetFormat*/);
				/*if (validOnly && !item.Valid) continue;*/
				Lines.Add(item);
			}
		}

		public void Save()
		{
			StringBuilder HostsText = new StringBuilder();
			foreach (HostLine item in Lines)
			{
				HostsText.AppendLine(item.ToString());
			}
			File.WriteAllText(FileName, HostsText.ToString());
		}

		public void ResetFormat()
		{
			foreach (HostLine Line in Lines)
			{
				if (Line.Valid) Line.ResetFormat();
			}
		}

		public void RemoveInvalid()
		{
			Lines.RemoveAll(item => !item.Valid);
		}

		public HostLine Get(string host)
		{
			return Lines.Find(item => item.Valid && item.Host == host);
		}

		public List<HostLine> GetMatch(string pattern)
		{
			pattern = "^" + pattern.Replace(".", @"\.").Replace("?", ".").Replace("*", ".*") + "$";
			Regex regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
			return Lines.FindAll(item => item.Valid && regex.IsMatch(item.Host));
		}

		public bool Exists(string host)
		{
			return (Get(host) != null);
		}

		public void Add(string host, string ip, string comment = "", bool enabled = true)
		{
			Lines.Add(new HostLine(enabled, ip, host, comment));
		}

		public void Remove(string host)
		{
			HostLine Found = Get(host);
			if (Found != null)
			{
				Lines.Remove(Found);
			}
		}
	}
}
