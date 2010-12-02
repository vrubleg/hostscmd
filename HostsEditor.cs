using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace Hosts
{
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

		static private bool IsUTF8(byte[] data)
		{
			try
			{
				new UTF8Encoding(false, true).GetCharCount(data);
				return true;
			}
			catch
			{
				return false;
			}

		}

		public string FileName;
		public Encoding Encoding = new UTF8Encoding(false);
		public List<HostLine> Lines = new List<HostLine>();

		public HostsEditor(string fileName)
		{
			if (String.IsNullOrEmpty(fileName)) throw new ArgumentNullException("fileName");
			FileName = fileName;
		}

		public void Clear()
		{
			Lines.Clear();
		}

		public void Load()
		{
			Clear();
			if (!File.Exists(FileName)) throw new FileNotFoundException("hosts file not found", FileName);
			byte[] HostsData = File.ReadAllBytes(FileName);
			Encoding = (IsUTF8(HostsData)) ? new UTF8Encoding(false) : Encoding.Default;
			var HostsReader = new StreamReader(new MemoryStream(HostsData), Encoding);
			string line;
			while ((line = HostsReader.ReadLine()) != null)
			{
				HostLine item = new HostLine(line);
				Lines.Add(item);
			}
		}

		public void Save()
		{
			var HostsStream = new FileStream(FileName, FileMode.Create, FileAccess.Write);
			var HostsWriter = new StreamWriter(HostsStream, Encoding);
 			foreach (HostLine item in Lines) HostsWriter.WriteLine(item.ToString());
			HostsWriter.Close();
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
