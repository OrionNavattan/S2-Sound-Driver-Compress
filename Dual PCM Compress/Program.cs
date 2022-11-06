using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dual_PCM_Compress {
	class Program {
		static void Main(string[] args) {
			if (args.Length < 4) {
				Console.Write(
					"Usage: \"S2 Sound Driver Compress\" <driver bin> <settings> <output file> <compressor> <-a>\n" + 
					"<driver bin> :  Binary output data for the Sonic 2 driver\n" +
					"<settings> :    Settings file containing the data needed to fix the driver binary\n" +
					"<output file> : File to put the compressed data at\n" +
					"<compressor> :  The file location of the saxman compressor\n" +
					"<-a> : Optional flag for accurate sound driver compression \n" + 
					"This is a modified version of Natsumi's Dual PCM Compress, tailored to the needs\n" +
					"of Sonic the Hedgehog 2. It has been modified to correctly pass the -a argument for\n" +
					"accurate compression, and to allow it to patch the Saxman decompressor in the ROM\n" +
					"with the size of the compressed driver binary.\n" 
				);

				Console.ReadKey();
				return;
			}

			try {
				// figure out if arguments are ok
				if (!File.Exists(args[0])) {
					Console.WriteLine("Unable to read input file " + args[0]);
					Console.ReadKey();
					return;
				}

				if (!File.Exists(args[1])) {
					Console.WriteLine("Unable to read input file " + args[1]); // settings will contain an additional entry: the location of the sound driver size in the Saxman decompressor
					Console.ReadKey();
					return;
				}

				if (!File.Exists(args[2])) {
					Console.WriteLine("Unable to read input file " + args[2]);
					Console.ReadKey();
					return;
				}

				// load settings and determine the addresses
				byte[] settings = File.ReadAllBytes(args[1]);
				int outaddr = ReadLong(settings, 0); 
				int maxlen = ReadLong(settings, 4);
			 	int patchaddr = ReadLong(settings, 8); // new variable containing the location of the value to patch in the Saxman decompressor

				long inlen = new FileInfo(args[2]).Length;

				if (inlen < outaddr) {
					Console.WriteLine("The destination address for Dual PCM is larger than the ROM file!");
					Console.ReadKey();
					return;
				}
				
				{ // fix the file
					byte[] data = File.ReadAllBytes(args[0]);

					for (int i = 12;i < settings.Length;) {
						data[(settings[i++] << 12) | settings[i++]] = settings[i++];

						if (settings[i++] != '>') {
							Console.WriteLine($"Unexpected delimiter {settings[i - 1]}");
							Console.ReadKey();
							return;
						}
					}

					File.WriteAllBytes(args[0], data);
				}

				{ // compress the file
					Process comp = Process.Start(new ProcessStartInfo(args[3], $"\"{args[4]}\" \"{args[0]}\" \"{args[0]}.sax\" ") { // output as .sax instead
						WorkingDirectory = Directory.GetCurrentDirectory(),
						UseShellExecute = false,
						CreateNoWindow = true,
						RedirectStandardError = true,
						RedirectStandardOutput = true,
					});

					comp.WaitForExit(3000);
					if (!comp.HasExited) comp.Kill();

					// check we succeeded
					if (!File.Exists(args[0] + ".sax")) {
						Console.WriteLine("Could not compress file correctly, unable to continue.");
						Console.ReadKey();
						return;
					}
				}
				// get length of compressed driver
				long actuallen = new FileInfo(args[0] + ".sax").Length;
				ushort sizepatchinter = Convert.ToUInt16(actuallen);

				// if it is 0 or less, it probably failed
				if (actuallen <= 0) {
					Console.WriteLine("Could not compress file correctly, unable to continue.");
					Console.ReadKey();
					return;
				}

				// check if the compressed driver fits
				if(actuallen > maxlen) {
					Console.WriteLine($"Compressed sound driver does not fit! Increase Z80_Space to ${actuallen.ToString("X")} and build again.");
					Console.ReadKey();
					return;
				}

				// convert actuallen to big-endian word for patching saxman compressor
		
				byte[] sizepatch = BitConverter.GetBytes(sizepatchinter);
    			Array.Reverse(sizepatch);

				// copy the compressed file data
				using(FileStream fs = File.OpenWrite(args[2])) { // open rom file
					fs.Seek(outaddr, SeekOrigin.Begin);			// address where compressed driver will be written

					using (FileStream cf = File.OpenRead(args[0] + ".sax")) { // open compressed driver, change to .sax
						cf.CopyTo(fs, (int)actuallen); // write compressed driver
					
					}
				
				}

				using(FileStream fs = File.OpenWrite(args[2])) { // open rom file

					fs.Seek(patchaddr, SeekOrigin.Begin); // address where size will be patched in Saxman decompressor
					
					using(BinaryWriter w = new BinaryWriter(fs)) {
						w.Write(sizepatch);
					}	

				}
				// delete files and send message
				File.Delete(args[0] + ".sax");
				File.Delete(args[0]);
				File.Delete(args[1]);
				Console.WriteLine($"Success! Compressed driver size is ${actuallen.ToString("X")}!");

			} catch (Exception e) {
				Console.WriteLine(e);
				Console.ReadKey();
			}
		}

		private static int ReadLong(byte[] arr, int off) {
			return ((arr[off++] << 24) | (arr[off++] << 16) | (arr[off++] << 8) | arr[off]);
		}
	}
}
