using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;

namespace Seristerll
{
	public class MainWindowViewModel
	{
		private SerialPort serial;
		private CancellationTokenSource tokenSource;

		private string[] memory = new string[4];

		public MainWindowViewModel()
		{
			this.serial = new SerialPort()
			{
				WriteTimeout = 3000,
			};

			this.UpdatePorts();
			//this.Port = this.Ports.FirstOrDefault();
			this.LoadSettings();

			this.UpdatePortsCommand.Subscribe(this.UpdatePorts);

			this.StartAsyncCommand.Subscribe(this.StarAsync);

			this.StopCommand = this.IsRunning.ToReactiveCommand();
			this.StopCommand.Subscribe(this.Stop);
			this.CloseCommand.Subscribe(this.CloseApplication);

			this.SendCommand.Subscribe(this.SendMessage);
			this.SendControlCodeCommand.Subscribe(this.SendControlCode);
			this.SaveMemoryCommand.Subscribe(this.SaveMemory);
			this.LoadMemoryCommand.Subscribe(this.LoadMemory);
			this.ClearMessageCommand.Subscribe(this.ClearMessage);

			this.ClearLogCommand.Subscribe(this.ClearLogs);

			this.ClearReceiveBufferCommand.Subscribe(() => this.ClearBufferProperty(this.RemainReseiveBuffer));
			this.ClearRemainSendBufferCommand.Subscribe(() => this.ClearBufferProperty(this.RemainSendBuffer));
		}

		public string Port { get; set; }

		public int BaudRate { get; set; } = 115200;

		public Parity Parity { get; set; } = Parity.None;

		public int DataBits { get; set; } = 8;

		public StopBits StopBits { get; set; } = StopBits.One;

		public Handshake Handshake { get; set; } = Handshake.None;

		public ObservableCollection<string> Ports { get; set; } = new ObservableCollection<string>();

		public int[] BaudRates { get; } = new int[] { 100, 300, 600, 1200, 2400, 4800, 9600, 14400, 19200, 38400, 56000, 57600, 115200, 128000, 256000};

		public int[] DataBitsSet { get; } = new int[] { 5, 6, 7, 8 };

		public ReactiveProperty<string> HEXLog { get; set; } = new ReactiveProperty<string>(string.Empty);

		public ReactiveProperty<string> CharLog { get; set; } = new ReactiveProperty<string>(string.Empty);

		public ReactiveProperty<string> TextLog { get; set; } = new ReactiveProperty<string>(string.Empty);

		public ReactiveProperty<int> RemainReseiveBuffer { get; set; } = new ReactiveProperty<int>();

		public ReactiveProperty<int> RemainSendBuffer { get; set; } = new ReactiveProperty<int>();

		public ReactiveProperty<bool> IsCTS { get; set; } = new ReactiveProperty<bool> (false);

		public ReactiveProperty<bool> IsDSR { get; set; } = new ReactiveProperty<bool>(false);
		public ReactiveProperty<bool> IsRLSD { get; set; } = new ReactiveProperty<bool>(false);
		public ReactiveProperty<bool> IsXOffR { get; set; } = new ReactiveProperty<bool>(false);
		public ReactiveProperty<bool> IsXOffS { get; set; } = new ReactiveProperty<bool>(false);

		public bool DoDirectSendControlCode { get; set; }

		public bool DoDirectSendMemory { get; set; }

		public bool DtrEnable { get; set; }

		public bool RtsEnable { get; set; }

		public ReactiveProperty<bool> IsRunning { get; } = new ReactiveProperty<bool>(false);

		public ReactiveProperty<string> Message { get; set; } = new ReactiveProperty<string>(string.Empty);

		public ReactiveCommand ClearLogCommand { get; } = new ReactiveCommand();

		public ReactiveCommand SaveLogCommand { get; } = new ReactiveCommand();

		public ReactiveCommand UpdatePortsCommand { get; } = new ReactiveCommand();

		public AsyncReactiveCommand StartAsyncCommand { get; } = new AsyncReactiveCommand();

		public ReactiveCommand StopCommand { get; } = new ReactiveCommand();

		public ReactiveCommand<Window> CloseCommand { get; } = new ReactiveCommand<Window>();

		public ReactiveCommand ClearSendBufferCommand { get; } = new ReactiveCommand();

		public ReactiveCommand ClearReceiveBufferCommand { get; } = new ReactiveCommand();

		public ReactiveCommand<string> SendControlCodeCommand { get; } = new ReactiveCommand<string>();

		public ReactiveCommand<string> LoadMemoryCommand { get; } = new ReactiveCommand<string>();

		public ReactiveCommand<string> SaveMemoryCommand { get; } = new ReactiveCommand<string>();

		public ReactiveCommand SendCommand { get; } = new ReactiveCommand();

		public ReactiveCommand ClearMessageCommand { get; } = new ReactiveCommand();

		public ReactiveCommand ClearRemainReceiveBufferCommand { get; } = new ReactiveCommand();

		public ReactiveCommand ClearRemainSendBufferCommand { get; } = new ReactiveCommand();

		public ReactiveCommand<bool> ControlDTRCommand { get; } = new ReactiveCommand<bool>();

		public ReactiveCommand<bool> ControlRTSCommand { get; } = new ReactiveCommand<bool>();

		public ReactiveCommand<bool> ControlXONOFFCommand { get; } = new ReactiveCommand<bool>();

		public ReactiveCommand<bool> ControlBRCommand { get; } = new ReactiveCommand<bool>();

		


		private void UpdatePorts()
		{
			this.Ports.Clear();
			SerialPort.GetPortNames().ToList().ForEach(p => this.Ports.Add(p));
		}

		private async Task StarAsync()
		{
			this.serial.PortName = this.Port;
			this.serial.BaudRate = this.BaudRate;
			this.serial.Parity = this.Parity;
			this.serial.DataBits = this.DataBits;
			this.serial.StopBits = this.StopBits;
			this.serial.Handshake = this.Handshake;
			this.serial.DtrEnable = this.DtrEnable;
			this.serial.RtsEnable= this.RtsEnable;

			this.SaveSettings();

			this.serial.Open();

			this.tokenSource = new CancellationTokenSource();
			var token = this.tokenSource.Token;

			this.IsRunning.Value= true;

			await Task.Run(() =>
			{
				try
				{
					while (!token.IsCancellationRequested)
					{
						if (this.serial.BytesToRead > 0)
						{
							var buffer = new byte[this.serial.BytesToRead];
							this.serial.Read(buffer, 0, buffer.Length);
							this.AddLogs(buffer);
						}

						Thread.Sleep(10);
					}

					this.tokenSource.Dispose();
					this.tokenSource = null;
				}
				catch (Exception e)
				{
					MessageBox.Show(e.Message);
				}

			}, token);

			this.serial.Close();
			this.IsRunning.Value= false;
		}

		private void Stop()
		{
			this.tokenSource?.Cancel();
		}

		private void CloseApplication(System.Windows.Window window)
		{
			window.Close();
		}

		private void ClearLogs()
		{
			this.HEXLog.Value = string.Empty;
			this.CharLog.Value = string.Empty;
			this.TextLog.Value = string.Empty;
		}

		private void AddLogs(byte[] buffer)
		{
			// TODO キャラクタモードでは制御文字を変換する
			var textLog = Regex.Escape(Encoding.ASCII.GetString(buffer));
			var charLog = string.Join(" ",buffer.Select(b => ConvertByte2CharString(b))) + " ";
			var hexLog = string.Join(" ", buffer.Select(b => b.ToString("X2"))) + " ";

			Application.Current.Dispatcher.Invoke(() =>
			{
				this.TextLog.Value += textLog;
				this.CharLog.Value += charLog;
				this.HEXLog.Value += hexLog;
				this.UpdateStatus();
			});

		}

		private void UpdateStatus()
		{
			this.RemainSendBuffer.Value = this.serial.BytesToWrite;
			this.RemainReseiveBuffer.Value = this.serial.BytesToRead;
		}

		private  string ConvertByte2CharString(byte b)
		{
			switch (b)
			{
				case 2:
					return "[STX]";
				case 3:
					return "[ETX]";
				case 4:
					return "[EOX]";
				case 5:
					return "[ENQ]";
				case 6:
					return "[ACK]";
				case 0x15:
					return "[NAK]";
				case 0x0D:
					return "[CR]";
				case 0x0A:
					return "[LF]";
				default:
					return ((char)b).ToString();
			}
		}

		private void SendMessage()
		{
			// todo タイムアウトの例外処理
			var unEscaped = Regex.Unescape(this.Message.Value);
			this.serial.Write(unEscaped);
		}

		private void SendControlCode(string code_text)
		{
			//byte code = 0x02;
			var code = Convert.ToByte(code_text, 16);
			if (this.DoDirectSendControlCode)
			{
				this.serial.Write(new byte[] { code }, 0, 1);
			}
			else
			{
				this.Message.Value += code.ToString();
			}
		}

		private void LoadMemory(string num)
		{
			Console.WriteLine(num.ToString());
			var n = int.Parse(num);
			var message = this.memory[n];

			if (this.DoDirectSendControlCode)
			{
				// TODO SendMessage と処理の共通化
				// todo タイムアウトの例外処理
				var unEscaped = Regex.Unescape(this.Message.Value);
				this.serial.Write(unEscaped);
			}
			else
			{
				this.Message.Value = message;
			}
		}

		private void SaveMemory(string num)
		{
			Console.WriteLine(num);
			var n = int.Parse(num);
			this.memory[n] = this.Message.Value;
		}

		private void ClearMessage()
		{
			this.Message.Value = string.Empty;
		}

		private void LoadSettings()
		{
			this.Port = Properties.Settings.Default.PortName;
			this.BaudRate = Properties.Settings.Default.BaudRate;
			this.DataBits = Properties.Settings.Default.DataBits;
			this.StopBits = Properties.Settings.Default.StopBits;
			this.Handshake = Properties.Settings.Default.HandShake;
			this.serial.DtrEnable = Properties.Settings.Default.DtrEnable;
			this.serial.RtsEnable = Properties.Settings.Default.RtsEnable;
		}

		private void SaveSettings()
		{
			Properties.Settings.Default.PortName = this.Port;
			Properties.Settings.Default.BaudRate = this.BaudRate;
			Properties.Settings.Default.DataBits = this.DataBits;
			Properties.Settings.Default.StopBits = this.StopBits;
			Properties.Settings.Default.HandShake = this.Handshake;
			Properties.Settings.Default.DtrEnable = this.DtrEnable;
			Properties.Settings.Default.RtsEnable = this.RtsEnable;

			Properties.Settings.Default.Save();
		}

		private void ClearBufferProperty(ReactiveProperty<int> bufferProperty)
		{
			bufferProperty.Value = 0;
		}
	}
}
