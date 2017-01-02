using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Dinofox_Viewer
{
    class fileTextureDKR
    {
        BinaryReader binReader, tabReader;

        public String binfileLoc;
        public String tabfileLoc;

        public List<uint> addresses = new List<uint>();
        List<Bitmap> images = new List<Bitmap>();

        bool valid = false;

        public fileTextureDKR(string fileLoc)
        {
            valid = false;

            binfileLoc = fileLoc;
            tabfileLoc = fileLoc.Replace(".bin",".tab");

            if(!File.Exists(tabfileLoc))
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
                if ((int)curAddress == -1) break;
                addresses.Add(curAddress);
            }

            System.Console.WriteLine("Addresses found: " + addresses.Count.ToString());
            tabReader.Close();

            //read and store images from binary file
            for (int i = 0; i < addresses.Count; i++)
            {
                if (addresses[addresses.Count-1] == binReader.BaseStream.Position) return; //trusting the format to save us here
                if (addresses[i] == binReader.BaseStream.Length) return; //just in case our trust is broken

                //prepare file reader
                binReader.BaseStream.Seek(addresses[i], SeekOrigin.Begin);
                byte width = binReader.ReadByte();
                byte height = binReader.ReadByte();
                byte format = binReader.ReadByte();
                byte unk1 = binReader.ReadByte();
                byte flip = binReader.ReadByte();//unused?
                binReader.BaseStream.Seek(addresses[i] + 0x20, SeekOrigin.Begin);
                Bitmap tempImg = new Bitmap(width, height);

                //begin comparisons!
                if (format == 0)
                {
                    if (unk1 == 0x0) //normal read. RGBA, byte each
                    {
                        for (int y = tempImg.Height - 1; y >= 0; --y)
                        {
                            for (int x = 0; x < tempImg.Width; x++)
                            {
                                uint argb = binReader.ReadUInt32();
                                Color col = Color.FromArgb((byte)(argb >> 24), (byte)argb, (byte)(argb >> 8), (byte)(argb >> 16));
                                tempImg.SetPixel(x, y, col);
                            }
                        }
                    }
                    else //every other row is pair-swapped (2,3,0,1 6,7,4,5 etc)
                    {
                        for (int y = 0; y < tempImg.Height; y++)
                        {
                            if ((y % 2) == 0)
                            {
                                for (int x = 0; x < tempImg.Width; x++)
                                {
                                    uint argb = binReader.ReadUInt32();
                                    Color col = Color.FromArgb((byte)(argb >> 24), (byte)argb, (byte)(argb >> 8), (byte)(argb >> 16));
                                    tempImg.SetPixel(x, y, col);
                                }
                            }
                            else
                            {
                                for (int x = 0; x < tempImg.Width; x += 4)
                                {
                                    for (int xx = 0; xx < 4; xx++)
                                    {
                                        uint argb = binReader.ReadUInt32();
                                        Color col = Color.FromArgb((byte)(argb >> 24), (byte)argb, (byte)(argb >> 8), (byte)(argb >> 16));
                                        tempImg.SetPixel(x+((xx+2)%4), y, col);
                                    }
                                }
                            }
                        }
                    }
                }
                else if (format == 0x5 || format == 0x25)
                {
                    if (unk1 == 0x0) //greyscale image, byte is the brightness, vertically flipped
                    {
                        for (int y = tempImg.Height - 1; y >= 0; --y)
                        {
                            for (int x = 0; x < tempImg.Width; x++)
                            {
                                byte val = binReader.ReadByte();
                                tempImg.SetPixel(x, y, Color.FromArgb(val, val, val));
                            }
                        }
                    }
                    else
                    {
                        for (int y = tempImg.Height - 1; y >= 0; --y)
                        {
                            if((height % 2) != (y % 2)) //if (height is even and row is odd) or vice-versa
                            {
                                for (int x = 0; x < tempImg.Width; x++)
                                {
                                    byte val = binReader.ReadByte();
                                    tempImg.SetPixel(x, y, Color.FromArgb(val, val, val)); 
                                }
                            }
                            else //each chunk of 8 pixels are reversed in this row
                            {
                                for (int x = 0; x < tempImg.Width; x++)
                                {
                                    byte val = binReader.ReadByte();
                                    tempImg.SetPixel(x+4-((((int)x/4)%2)*8), y, Color.FromArgb(val, val, val));
                                }

                            }
                        }
                    }
                }
                else if (format == 0x15) //greyscale image, byte per pixel
                {
                    for (int y = 0; y < tempImg.Height; y++)
                    {
                        for (int x = 0; x < tempImg.Width; x++)
                        {
                            byte val = binReader.ReadByte();
                            tempImg.SetPixel(x, y, Color.FromArgb(val, val, val));
                        }
                    }
                }
                else if (format == 0x26) //greyscale image, byte per two pixels
                {
                    for (int y = 0; y < tempImg.Height; y++)
                    {
                        for (int x = 0; x < tempImg.Width; x+=2)
                        {
                            byte val = binReader.ReadByte();
                            byte in1 = (byte)(val & 0xf0);
                            byte in2 = (byte)(val << 4);
                            tempImg.SetPixel(x, y, Color.FromArgb(in1, in1, in1));
                            tempImg.SetPixel(x + 1, y, Color.FromArgb(in2, in2, in2));
                        }
                    }
                } //UNSURE FROM HERE ON
                else if (format == 0x1)
                {
                    if (unk1 != 0) //5-bit R G B, 1-bit a, every other row gets pair-swapped
                    {
                        for (int y = 0; y < tempImg.Height; y++)
                        {
                            if ((y % 2) == 0)
                            {
                                   
                               for(int x = 0; x < tempImg.Width; x++)
                               {
                                   ushort val = Endian.SwapUInt16(binReader.ReadUInt16());

                                   byte r = (byte)((val & 0xF800) >> 8);
                                   byte g = (byte)((val & 0x07C0) >> 3);
                                   byte b = (byte)((val & 0x003E) << 2);
                                   byte a = (byte)((val & 0x0001) * 0xFF);

                                   tempImg.SetPixel(x, y, Color.FromArgb(a, r, g, b));
                               }
                            }
                            else
                            {
                                for (int x = 0; x < tempImg.Width; x+=4)
                                {
                                   for (int xx = 0; xx < 4; xx++)
                                   {
                                       if (x + (xx + 2) % 4 >= width) return;
                                       ushort val = Endian.SwapUInt16(binReader.ReadUInt16());

                                       byte r = (byte)((val & 0xF800) >> 8);
                                       byte g = (byte)((val & 0x07C0) >> 3);
                                       byte b = (byte)((val & 0x003E) << 2);
                                       byte a = (byte)((val & 0x0001) * 0xFF);

                                       tempImg.SetPixel(x + ((xx + 2) % 4), y, Color.FromArgb(a, r, g, b));

                                   }
                               }
                           }
                        }
                    }
                    else //5-bit R G B, 1-bit a, vertical flip
                    {
                        for (int y = tempImg.Height - 1; y >= 0; --y)
                        {
                            for(int x = 0; x < tempImg.Width; x++)
                            {
                                ushort val = Endian.SwapUInt16(binReader.ReadUInt16());

                                byte r = (byte)((val & 0xF800) >> 8);
                                byte g = (byte)((val & 0x07C0) >> 3);
                                byte b = (byte)((val & 0x003E) << 2);
                                byte a = (byte)((val & 0x0001) * 0xFF);

                                tempImg.SetPixel(x, y, Color.FromArgb(a, r, g, b));
                            }
                        }
                    }
                }
                else if (format == 0x4) //4-bit, 2 bytes per pixel
                {
                    for (int y = 0; y < tempImg.Height; y++)
                    {
                        for (int x = 0; x < tempImg.Width; x++)
                        {
                            ushort val = binReader.ReadUInt16();
                            byte r = (byte)((val & 0xF) * 0x11);
                            byte g = (byte)(((val >> 4) & 0xF) * 0x11);
                            byte b = (byte)(((val >> 8) & 0xF) * 0x11);
                            byte a = (byte)(((val >> 12) & 0xF) * 0x11);
                            Color col = Color.FromArgb(a, r, g, b);
                            tempImg.SetPixel(x, y, col);
                        }
                    }
                }
                else if (format == 0x11) //5-bit R G B, 1-bit a, vertical flip
                {
                    for (int y = tempImg.Height - 1; y >= 0; --y)
                    {
                        for (int x = 0; x < tempImg.Width; x++)
                        {
                            ushort val = Endian.SwapUInt16(binReader.ReadUInt16());

                            byte r = (byte)((val & 0xF800) >> 8);
                            byte g = (byte)((val & 0x07C0) >> 3);
                            byte b = (byte)((val & 0x003E) << 2);
                            byte a = (byte)((val & 0x0001) * 0xFF);

                            tempImg.SetPixel(x, y, Color.FromArgb(a, r, g, b));
                        }
                    }
                }
                else
                {
                    System.Console.WriteLine("Whoops, new format: " + format);
                    continue;
                }

                images.Add(tempImg);
            }
            System.Console.WriteLine("Finished reading");

            binReader.Close();
            System.Console.WriteLine("Closed reader");
        }

        public Bitmap returnImg(int index)
        {
            return images[index];
        }

        public bool isValid()
        {
            return valid;
        }
    }
}
