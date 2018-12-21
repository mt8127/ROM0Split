using System;
using ROM0Split.Implementations;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using YamlDotNet.RepresentationModel;

namespace ROM0Split.Model
{
	class MainWindowModel : SuperViewModel
	{
		const int BUFFER_SIZE = 20 * 1024;
		private string _scatterFile;
		private string _rom0File;
		private DelegateCommand _scatterFileDialog;
		private DelegateCommand _rom0FileDialog;
		private DelegateCommand _split;

		public string scatterFile
		{
			get { return _scatterFile; }
			set { SetProperty(ref _scatterFile, value); }
		}

		public string rom0File
		{
			get { return _rom0File; }
			set { SetProperty(ref _rom0File, value); }
		}

		public DelegateCommand scatterFileDialog
		{
			get { return _scatterFileDialog; }
			set { SetProperty(ref _scatterFileDialog, value); }
		}

		public DelegateCommand rom0FileDialog
		{
			get { return _rom0FileDialog; }
			set { SetProperty(ref _rom0FileDialog, value); }
		}

		public DelegateCommand split
		{
			get { return _split; }
			set { SetProperty(ref _split, value); }
		}
		
		private void OnscatterFileDialogExecute()
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.ShowDialog();
			scatterFile = openFileDialog.FileName;
			openFileDialog = null;
		}

		private void Onrom0FileDialogExecute()
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.ShowDialog();
			rom0File = openFileDialog.FileName;
			openFileDialog = null;
		}

		private void OnsplitExecute()
		{
			if (scatterFile == null)
			{
				MessageBox.Show("Please select a Scatter file", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			else if (rom0File == null)
			{
				MessageBox.Show("Please select a ROM_0 file", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			else
			{
				string basepath = System.AppDomain.CurrentDomain.BaseDirectory + "\\out";
				FileStream stream = File.OpenRead(rom0File);
				StringReader reader = new StringReader(File.ReadAllText(scatterFile));

				if (!Directory.Exists(basepath))
					Directory.CreateDirectory(basepath);

				// Load the stream
				var yaml = new YamlStream();
				yaml.Load(reader);

				// Examine the stream
				var mapping = (YamlSequenceNode)yaml.Documents[0].RootNode;
				foreach (YamlMappingNode entry in mapping.Children)
				{
					var node = entry.Children;
					//Console.WriteLine(entry.Children);
					if (node.ContainsKey("general") || node["partition_name"].ToString() == "PRELOADER")
						continue;


					string fileName = node["file_name"].ToString();
					if (fileName == "NONE")
						fileName = node["partition_name"].ToString();

					byte[] buffer = new byte[BUFFER_SIZE];

					Stream outFile = File.Create(basepath + "\\" + fileName);

					stream.Position = 
						long.Parse(node["linear_start_addr"].ToString().Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);

					UInt64 size = UInt64.Parse(node["partition_size"].ToString().Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
					UInt64 remaining = size;
					int bytesRead;
					while (remaining > 0 && (bytesRead = stream.Read(buffer, 0,
							int.Parse(Math.Min(remaining, BUFFER_SIZE).ToString()))) > 0)
					{
						outFile.Write(buffer, 0, bytesRead);
						remaining -= (uint)bytesRead;
					}

					outFile.Flush();
					outFile.Close();
					outFile.Dispose();
					Console.WriteLine(node["partition_index"]);
				}

				MessageBox.Show("Splitting Done", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}

		private void OndecryptExecute()
		{
		}

		public MainWindowModel()
		{
			scatterFileDialog = new DelegateCommand(new Action(OnscatterFileDialogExecute));
			rom0FileDialog = new DelegateCommand(new Action(Onrom0FileDialogExecute));
			split = new DelegateCommand(new Action(OnsplitExecute));
		}
	}
}
