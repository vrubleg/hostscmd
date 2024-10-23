using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Hosts;

public class HostsItem
{
	protected string SourceString;
	protected ulong SourceHash;

	public bool Valid { get; protected set; }
	public bool Enabled { get; set; }
	public bool Hidden { get; set; }
	public NetAddress IP { get; set; }
	public HostAliases Aliases { get; set; }
	public string Comment { get; set; }
	public bool ResetFormat { get; set; }
	public bool Deleted => Valid && (Aliases == null || Aliases.Count == 0);

	public string RawString
	{
		get
		{
			bool format = ResetFormat || String.IsNullOrEmpty(SourceString) || SourceHash != HalfMD5.ComputeHash(ToString());
			return format ? ToString(false) : SourceString;
		}
		set
		{
			Parse(value);
		}
	}

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
		Comment = comment ?? "";
		Valid = true;
	}

	public HostsItem(string ip, string[] hosts, string comment = "")
	{
		Enabled = true;
		IP = new NetAddress(ip);
		Aliases = new HostAliases(hosts);
		Comment = comment ?? "";
		Valid = true;
	}

	public HostsItem(NetAddress ip, HostName host, string comment = "")
	{
		Enabled = true;
		IP = ip;
		Aliases = new HostAliases(host);
		Comment = comment ?? "";
		Valid = true;
	}

	public HostsItem(NetAddress ip, HostAliases hosts, string comment = "")
	{
		Enabled = true;
		IP = ip;
		Aliases = hosts;
		Comment = comment ?? "";
		Valid = true;
	}

	public HostsItem(HostsItem source)
	{
		this.Valid = source.Valid;
		this.Enabled = source.Enabled;
		this.Hidden = source.Hidden;
		this.IP = source.IP;
		this.Aliases = source.Aliases == null ? null : new HostAliases(source.Aliases);
		this.Comment = source.Comment;
		this.ResetFormat = source.ResetFormat;
		this.SourceString = source.SourceString;
		this.SourceHash = source.SourceHash;
	}

	public HostsItem Clone()
	{
		return new HostsItem(this);
	}

	public bool Parse(string value)
	{
		SourceString = value;
		try
		{
			value = value.Trim();
			if (String.IsNullOrEmpty(value)) { throw new FormatException(); }
			if (value.IndexOfAny(new char[] {'\r', '\n'}) != -1) { throw new FormatException(); }

			// Check if enabled.
			Enabled = value[0] != '#';
			if (!Enabled) { value = value.Substring(1).TrimStart(); }
			if (String.IsNullOrEmpty(value)) { throw new FormatException(); }

			// Parse comment part.
			Hidden = false;
			Comment = null;
			var comment_pos = value.IndexOf('#');
			if (comment_pos != -1)
			{
				Comment = value.Substring(comment_pos + 1).Trim();
				if (!String.IsNullOrEmpty(Comment))
				{
					Hidden = Comment[0] == '!';
					if (Hidden) Comment = Comment.Substring(1).Trim();
				}
				value = value.Substring(0, comment_pos).TrimEnd();
				if (String.IsNullOrEmpty(value)) { throw new FormatException(); }
			}

			// Parse IP and hosts.
			var parts = value.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length < 2) { throw new FormatException(); }

			IP = new NetAddress(parts[0]);
			Aliases = new HostAliases(parts.Skip(1).ToArray());
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
		SourceHash = HalfMD5.ComputeHash(ToString());
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
			if (!String.IsNullOrWhiteSpace(Comment) || Hidden) result += "#";
			if (Hidden) result += "!"; else result += " ";
			if (!String.IsNullOrWhiteSpace(Comment)) result += Comment;
			return result.Trim();
		}
		else
		{
			string result = SourceString.Trim();
			if (result.Length > 0 && result[0] != '#') result = "# " + result;
			return result;
		}
	}
}
