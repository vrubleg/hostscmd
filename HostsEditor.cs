﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Hosts
{
	public class HostsEditor : List<HostsItem>
	{
		public string FileName { get; set; }
		public Encoding Encoding { get; protected set; }

		public HostsEditor(string fileName = null) : base()
		{
			FileName = fileName;
			Encoding = new UTF8Encoding(false);
		}

		private bool IsUTF8(byte[] data)
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

		public void Load()
		{
			Load(FileName);
		}

		public void Load(string fileName)
		{
			Clear();
			byte[] HostsData = File.ReadAllBytes(fileName);
			Encoding = (IsUTF8(HostsData)) ? new UTF8Encoding(false) : Encoding.Default;
			var HostsReader = new StreamReader(new MemoryStream(HostsData), Encoding);
			string line;
			while ((line = HostsReader.ReadLine()) != null)
			{
				HostsItem item = new HostsItem(line);
				this.Add(item);
			}
		}

		public void Save()
		{
			Save(FileName);
		}

		public void Save(string fileName)
		{
			var HostsStream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
			var HostsWriter = new StreamWriter(HostsStream, Encoding);
			foreach (HostsItem item in this)
			{
				if (item.Deleted) break;
				HostsWriter.WriteLine(item.RawString);
			}
			HostsWriter.Close();
		}

		public void Add(NetAddress ip, HostAliases hosts, string comment = "")
		{
			base.Add(new HostsItem(ip, hosts, comment));
		}

		public void ResetFormat(bool resetFormat = true)
		{
			this.ForEach(item => item.ResetFormat = resetFormat);
		}

		public void RemoveInvalid()
		{
			this.RemoveAll(item => !item.Valid || item.Deleted);
		}

		public void RemoveDeleted()
		{
			this.RemoveAll(item => item.Deleted);
		}

		public int RemoveLinesWithHost(HostName host)
		{
			return this.RemoveAll(item => item.Valid && item.Aliases.Contains(host));
		}

		public int RemoveLinesWithHost(HostName host, bool IPv6)
		{
			return this.RemoveAll(item => item.Valid && item.IP.IPv6 == IPv6 && item.Aliases.Contains(host));
		}

		public int RemoveLinesWithIp(NetAddress ip)
		{
			return this.RemoveAll(item => item.Valid && item.IP == ip);
		}

		public List<HostsItem> GetValid()
		{
			return this.FindAll(item => item.Valid || !item.Deleted);
		}

		public List<HostsItem> GetMatched(string pattern, Func<HostsItem, bool> check = null)
		{
			var wp = new WildcardPattern(pattern);
			if (check == null)
			{
				return this.FindAll(item => item.Valid && item.Aliases.IsMatch(wp));
			}
			else
			{
				return this.FindAll(item => item.Valid && item.Aliases.IsMatch(wp) && check(item));
			}
		}
	}
}
