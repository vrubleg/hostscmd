using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Hosts
{
	public class HostsList : List<HostsItem>
	{
		public string FileName { get; set; }
		public Encoding Encoding { get; protected set; }

		public HostsList(string fileName = null) : base()
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
			foreach (HostsItem item in this) HostsWriter.WriteLine(item.ToString());
			HostsWriter.Close();
		}

		public void ResetFormat(bool resetFormat = true)
		{
			foreach (HostsItem item in this)
			{
				item.ResetFormat = resetFormat;
			}
		}
	}
}
