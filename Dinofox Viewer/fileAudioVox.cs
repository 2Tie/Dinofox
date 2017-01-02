using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Dinofox_Viewer
{
    class fileAudioVox
    {
        BinaryReader binReader, tabReader;

        public String binfileLoc;
        public String tabfileLoc;

        public List<uint> addresses = new List<uint>();
        List<Byte[]> tracks = new List<Byte[]>();

        bool valid = false;

        public fileAudioVox(String fileLoc)
        {
            valid = false;

            binfileLoc = fileLoc;
            tabfileLoc = fileLoc.Replace(".bin", ".tab");

            if (!File.Exists(tabfileLoc))
            {
                return;
            }

            valid = true;

            binReader = new BinaryReader(File.Open(fileLoc, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            tabReader = new BinaryReader(File.Open(tabfileLoc, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

            //Read the table file and populate the address list
            uint curAddress = 0;
            while (tabReader.BaseStream.Position != tabReader.BaseStream.Length)
            {
                curAddress = Endian.SwapUInt32(tabReader.ReadUInt32());
                if ((int)curAddress == 0 && addresses.Count > 0) break;
                addresses.Add(curAddress);
            }

            System.Console.WriteLine("Addresses found: " + addresses.Count.ToString());
            tabReader.Close();

            //read and store the audio clip data from the binary file
            for (int i = 0; i < addresses.Count-1; i++)
            {
                binReader.BaseStream.Seek(addresses[i], SeekOrigin.Begin);
                if (addresses[i] == addresses.Count)
                    break;
                uint clipLength = addresses[i + 1] - addresses[i];
                byte[] tempStor = new byte[clipLength];
                binReader.BaseStream.Read(tempStor, 0, (int)clipLength);

                tracks.Add(tempStor);

            }
            System.Console.WriteLine("Finished reading");
            binReader.Close();
            System.Console.WriteLine("Closed Stream. " + tracks.Count + " tracks stored.");
        }

        public byte[] returnClip(int index)
        {
            return tracks[index];
        }

        public bool isValid()
        {
            return valid;
        }
    }
}
