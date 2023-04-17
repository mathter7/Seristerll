using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seristerll
{
	public class SerialSettings
	{
		public SerialSettings() { }

		public SerialSettings(string port, int baudRate, int dataBits, StopBits stopBits, Handshake handshake, bool dtrEnable, bool rtsEnable)
		{
			Port = port;
			BaudRate = baudRate;
			DataBits = dataBits;
			StopBits = stopBits;
			Handshake = handshake;
			DtrEnable = dtrEnable;
			RtsEnable = rtsEnable;
		}

		public string Port { get; set; }

		public int BaudRate { get; set; }

		public int DataBits { get; set; }

		public StopBits StopBits { get; set; } = StopBits.One;

		public Handshake Handshake { get; set; } = Handshake.None;

		public bool DtrEnable { get; set; }

		public bool RtsEnable { get; set; }
	}
}
