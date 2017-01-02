using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Dinofox_Viewer
{
    class fileSoundBankDP
    {
        BinaryReader binReader, tabReader;

        public String binfileLoc;
        public String tabfileLoc;

        public List<uint> addresses = new List<uint>();

        public short[] prevSmp = new short[8];

        int position = 0;

        bool valid = false;
        ushort usePred;

        byte[] predArr1 = new byte[8];
        byte[] predArr2 = new byte[8];

        short[] decData;

        public List<byte> audioData = new List<byte>();

        public List<typeInstrument> instruments = new List<typeInstrument>();

        UInt32 bankOffset;
        UInt16 bankInstruments;
        UInt16 bankFlags;
        UInt16 bankPad;
        UInt16 bankSampleRate;
        UInt32 percussionOffset;




        public fileSoundBankDP(string fileLoc)
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

            //Read the table file and populate the address list; for this specifically, the addresses should be (CTL start, tbl start, tbl end, end of tab)
            uint curAddress = 0;
            while (tabReader.BaseStream.Position != tabReader.BaseStream.Length)
            {
                curAddress = Endian.SwapUInt32(tabReader.ReadUInt32());
                if ((int)curAddress == 0 && addresses.Count > 0) break;
                addresses.Add(curAddress);
            }

            tabReader.Close();

            //start reading the data from the binary file. beginning should be the magic number, and then ctl
            //all three audio sample files only have one bank, i think.


            //TODO: in every seek, if flags = 0 it's an offset from the start of the TBL file?
            if(Endian.SwapUInt32(binReader.ReadUInt32()) != 0x42310001)
            {
                return; //invalid start to the file
            }

            bankOffset = Endian.SwapUInt32(binReader.ReadUInt32());
            binReader.BaseStream.Seek(bankOffset, SeekOrigin.Begin);
            bankInstruments = Endian.SwapUInt16(binReader.ReadUInt16());
            bankFlags = Endian.SwapUInt16(binReader.ReadUInt16());
            bankPad = Endian.SwapUInt16(binReader.ReadUInt16());
            bankSampleRate = Endian.SwapUInt16(binReader.ReadUInt16());
            percussionOffset = Endian.SwapUInt32(binReader.ReadUInt32());

            Console.WriteLine("Bank Offset: {0:X}", bankOffset);
            Console.WriteLine("Percussion Offset: {0:X}", percussionOffset);

            if(bankFlags == 0 || bankFlags == 1) //yay standard format!
            {
                for(int i = 0; i < bankInstruments; i++)//for each instrument in the bank, add the data to array.
                {
                    var tempInstrument = new typeInstrument();

                    binReader.BaseStream.Seek(bankOffset + 0xC + (i * 4),SeekOrigin.Begin); //pull the address and store it
                    UInt32 instrumentAddress = Endian.SwapUInt32(binReader.ReadUInt32());
                    binReader.BaseStream.Seek(instrumentAddress, SeekOrigin.Begin);

                    //Console.WriteLine("Instrument Address: {0:X}", instrumentAddress);

                    tempInstrument.volume = binReader.ReadByte();
                    tempInstrument.pan = binReader.ReadByte();
                    tempInstrument.priority = binReader.ReadByte();
                    tempInstrument.flags = binReader.ReadByte();
                    tempInstrument.tremType = binReader.ReadByte();
                    tempInstrument.tremRate = binReader.ReadByte();
                    tempInstrument.tremDepth = binReader.ReadByte();
                    tempInstrument.tremDelay = binReader.ReadByte();
                    tempInstrument.vibType = binReader.ReadByte();
                    tempInstrument.vibRate = binReader.ReadByte();
                    tempInstrument.vibDepth = binReader.ReadByte();
                    tempInstrument.vibDelay = binReader.ReadByte();
                    tempInstrument.bendRange = Endian.SwapUInt16(binReader.ReadUInt16());
                    tempInstrument.soundCount = Endian.SwapUInt16(binReader.ReadUInt16());

                    for(int j = 0; j < tempInstrument.soundCount; j++)//for each sound in the instrument, add the data to the array.
                    {
                        var tempSound = new typeSound();

                        tempSound.wavetable.predictors = new List<ushort>();
                        tempSound.wavetable.state = new List<ushort>();
                        tempSound.wavetable.waveData = new List<byte>();

                        binReader.BaseStream.Seek(instrumentAddress + 0x10 + (j*4), SeekOrigin.Begin); //goto and read sound data
                        UInt32 soundAddress = Endian.SwapUInt32(binReader.ReadUInt32());

                        //Console.WriteLine("Sound Address #" + j + ": {0:X}", soundAddress);

                        binReader.BaseStream.Seek(soundAddress, SeekOrigin.Begin);

                        tempSound.envelopeAddress = Endian.SwapUInt32(binReader.ReadUInt32());
                        tempSound.keymapAddress = Endian.SwapUInt32(binReader.ReadUInt32());
                        tempSound.wavetableAddress = Endian.SwapUInt32(binReader.ReadUInt32());
                        tempSound.pan = binReader.ReadByte();
                        tempSound.volume = binReader.ReadByte();
                        tempSound.flags = binReader.ReadByte();

                        binReader.BaseStream.Seek(tempSound.envelopeAddress, SeekOrigin.Begin); //goto and read envelope data
                        tempSound.envelope.attackTime = Endian.SwapUInt32(binReader.ReadUInt32());
                        tempSound.envelope.decayTime = Endian.SwapUInt32(binReader.ReadUInt32());
                        tempSound.envelope.releaseTime = Endian.SwapUInt32(binReader.ReadUInt32());
                        tempSound.envelope.attackVolume = Endian.SwapUInt16(binReader.ReadUInt16());
                        tempSound.envelope.decayVolume = Endian.SwapUInt16(binReader.ReadUInt16());

                        binReader.BaseStream.Seek(tempSound.keymapAddress, SeekOrigin.Begin); //goto and read keymap data
                        tempSound.keymap.velocityMin = binReader.ReadByte();
                        tempSound.keymap.velocityMax = binReader.ReadByte();
                        tempSound.keymap.keyMin = binReader.ReadByte();
                        tempSound.keymap.keyMax = binReader.ReadByte();
                        tempSound.keymap.keyBase = binReader.ReadByte();
                        tempSound.keymap.detune = binReader.ReadByte();

                        binReader.BaseStream.Seek(tempSound.wavetableAddress, SeekOrigin.Begin);//goto and read wavetable data
                        tempSound.wavetable.waveBase = Endian.SwapUInt32(binReader.ReadUInt32());
                        tempSound.wavetable.waveLength = Endian.SwapUInt32(binReader.ReadUInt32());
                        tempSound.wavetable.type = binReader.ReadByte();
                        tempSound.wavetable.flags = binReader.ReadByte();
                        binReader.ReadUInt16(); //padding to end of 32-bit chunk

                        tempSound.wavetable.loopAddress = Endian.SwapUInt32(binReader.ReadUInt32());
                        if (tempSound.wavetable.type == 0)
                        {
                            tempSound.wavetable.predictorAddress = Endian.SwapUInt32(binReader.ReadUInt32());

                            if (tempSound.wavetable.loopAddress != 0)
                            {
                                binReader.BaseStream.Seek(tempSound.wavetable.loopAddress + (bankOffset * tempSound.wavetable.flags), SeekOrigin.Begin); //goto and read loop data
                                tempSound.wavetable.start = Endian.SwapUInt32(binReader.ReadUInt32());
                                tempSound.wavetable.end = Endian.SwapUInt32(binReader.ReadUInt32());
                                tempSound.wavetable.count = Endian.SwapUInt32(binReader.ReadUInt32());

                                for (int k = 0; k < 16; k++) //fill the state array
                                {
                                    tempSound.wavetable.state.Add(Endian.SwapUInt16(binReader.ReadUInt16()));
                                }
                            }

                            if (tempSound.wavetable.predictorAddress != 0)
                            {
                                binReader.BaseStream.Seek(tempSound.wavetable.predictorAddress + (bankOffset * tempSound.wavetable.flags), SeekOrigin.Begin); //goto and read predictor data
                                tempSound.wavetable.order = Endian.SwapUInt32(binReader.ReadUInt32());
                                tempSound.wavetable.numPredictors = Endian.SwapUInt32(binReader.ReadUInt32());

                                //tempSound.wavetable.predictors.Add((ushort)(0x00000002)); //THESE ARE HERE CUZ IDK
                                //tempSound.wavetable.predictors.Add((ushort)(0x00000004));

                                for (int k = 0; k < tempSound.wavetable.order * tempSound.wavetable.numPredictors * 8; k++ )
                                {
                                    tempSound.wavetable.predictors.Add(Endian.SwapUInt16(binReader.ReadUInt16()));
                                }
                            }
                        }  
                        else
                        {
                            if (tempSound.wavetable.loopAddress != 0)
                            {
                                binReader.BaseStream.Seek(tempSound.wavetable.loopAddress + (bankOffset * tempSound.wavetable.flags), SeekOrigin.Begin); //goto and read loop data
                                tempSound.wavetable.start = Endian.SwapUInt32(binReader.ReadUInt32());
                                tempSound.wavetable.end = Endian.SwapUInt32(binReader.ReadUInt32());
                                tempSound.wavetable.count = Endian.SwapUInt32(binReader.ReadUInt32());
                            }
                        }

                        if (i == 52 && j == 0)
                        {
                            Console.WriteLine("WaveData Address: {0:X}", tempSound.wavetable.waveBase + (bankOffset * tempSound.wavetable.flags));
                            Console.WriteLine("Key Max: {0:X}", tempSound.keymap.keyMax);
                            Console.WriteLine("Order:( should be 2) {0:X}", tempSound.wavetable.order);
                            Console.WriteLine("predictors: (should be 4) {0:X}", tempSound.wavetable.numPredictors);
                            //throw new Exception("time to overview"); //for debugging.
                        }

                        binReader.BaseStream.Seek(tempSound.wavetable.waveBase + (bankOffset /* * tempSound.wavetable.flags*/) + 0x210, SeekOrigin.Begin);//goto and read wave data
                        for(int k = 0; k < tempSound.wavetable.waveLength; k++)
                        {
                            tempSound.wavetable.waveData.Add(binReader.ReadByte());
                        }

                        

                        tempInstrument.sounds.Add(tempSound);
                    }

                    instruments.Add(tempInstrument);
                }
                //Console.WriteLine("Instruments added: " + instruments.Count);
            }

            binReader.Close();
        }

        public bool isValid()
        {
            return valid;
        }

        public void dcom(int tmpIns, int tmpSnd)
        {

            audioData.Clear();
            
            short[] itable =
            {
            	0,1,2,3,4,5,6,7,
            	-8,-7,-6,-5,-4,-3,-2,-1,
            };



            var wave = instruments[tmpIns].sounds[tmpSnd].wavetable;

            
            int indx, pred;

            if(wave.type == 0)
            {
                if(true)//decompress the data
                {
                    int wavLeft = (int)(wave.waveLength / 9) * 9;

                    while (wavLeft > position)
                    {
                        indx = (wave.waveData[position] >> 4) & 0xF;
                        pred = (wave.waveData[position] & 0xF);

                        //pred = (int)(pred % wave.numPredictors); //necessary in this case???
                        //Console.WriteLine(indx + " " + pred);

                        position++;

                        for (int p = 0; p < 2; p++ )
                        {
                            short[] temp = new short[8];
                            decData = new short[8];
                            long total;

                            for (int j = 0; j < 8; j++)
                            {
                                //temp[j] = (short)((((wave.waveData[position + (int)(j / 2)] >> ((j % 2) * 4) & 0xF) >> 3 == 1) ? ((wave.waveData[position + (int)(j / 2)] >> ((j % 2) * 4))) : ((0xFFFFFFF0 + (wave.waveData[position + (int)(j / 2)] >> ((j % 2) * 4) & 0xF)))) << indx);//get the sign of the wavdata nybble and extend it

                                

                                byte nybble = (byte)(wave.waveData[position + (int)(j / 2)] >> ((j % 2) * 4) & 0x0F);
                                
                                temp[j] = (short)((((nybble >> 3 == 1) ? (0xFFF0 + nybble) : (nybble)) << indx) & 0xFFFF);//works.

                                //throw new NotImplementedException();
                            }

                            for (int j = 0; j < 8; j++)
                            {
                                total = wave.predictors[(pred * 16) + j] * prevSmp[6];
                                
                                //if (j < 7)
                                    total += wave.predictors[(pred * 16) + j + 1] * prevSmp[7];//dunno if this works as intended

                                

                                if (j > 0)
                                {
                                    for (int k = 0; k < j; k++)
                                    {
                                        
                                        total += temp[k] * wave.predictors[(pred * 16) + j - k];//works
                                    }
                                }

                                decData[j] = (short)(temp[j] + (total >> 0xb));

                                audioData.Add((byte)((decData[j] >> 8)));
                                audioData.Add((byte)decData[j]);

                                //throw new NotImplementedException();
                            }
                            prevSmp = decData;
                            position += 4;

                            //throw new NotImplementedException();
                        }

                        //throw new NotImplementedException();
                    }
                }
                Console.WriteLine("Address: " + wave.waveBase);

                //Console.WriteLine(wave.waveData[0]);
                //Console.WriteLine(wave.waveData[1]);
                //Console.WriteLine(wave.waveData[2]);
                //Console.WriteLine(wave.waveData[3]);
                //Console.WriteLine(audioData[0]);

                

                //make wave header
                int size = audioData.Count + 0x44 - 0x08;
                int tempsize = audioData.Count;

                //throw new NotImplementedException();
                audioData.InsertRange(0, new byte[] { 0x52, 0x49, 0x46, 0x46, (byte)(size & 0xFF), (byte)(size >> 8 & 0xFF), (byte)(size >> 16 & 0xFF), (byte)(size >> 24 & 0xFF), 0x57, 0x41, 0x56, 0x45, 0x66, 0x6D, 0x74, 0x20, 0x10, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, (byte)(bankSampleRate & 0xFF), (byte)(bankSampleRate >> 8 & 0xFF), (byte)(bankSampleRate >> 16 & 0xFF), (byte)(bankSampleRate >> 24 & 0xFF), (byte)(bankSampleRate * 2 >> 0 & 0xFF), (byte)(bankSampleRate * 2 >> 8 & 0xFF), (byte)(bankSampleRate * 2 >> 16 & 0xFF), (byte)(bankSampleRate * 2 >> 24 & 0xFF), 0x02, 0x00, 0x10, 0x00, 0x64, 0x61, 0x74, 0x61, (byte)(tempsize & 0xFF), (byte)(tempsize >> 8 & 0xFF), (byte)(tempsize >> 16 & 0xFF), (byte)(tempsize >> 24 & 0xFF)});//insert the header
                //start feeding data

                //make wave footer?

                //close and pass

                //throw new NotImplementedException();
            }
            else if (wave.type == 1)
            {
                throw new NotImplementedException();
            }
            else
            {
                throw new NotImplementedException();
            }

            //throw new NotImplementedException();

        }

        private void decode(typeSound.waveTableST wave)
        {
            short[] temp = new short[8];
            decData = new short[8];
            long total;
            int sample = 0;

            for (int j = 0; j < 8; j++ )
            {
                //wave.waveData[position]
                temp[j] = ((wave.waveData[position + (int)(j / 2)] >> ((j % 2) * 4) & 0xF) >> 3 == 1) ? ((short)(wave.waveData[position + (int)(j / 2)] >> ((j % 2) * 4))) : ((short)(0xF0 + wave.waveData[position + (int)(j / 2)] >> ((j % 2) * 4)));//get the sign of the wavdata nybble and extend it
            }

            for (int j = 0; j < 8; j++ )
            {
                total = predArr1[j] * prevSmp[6];
                if (j < 7)
                    total += predArr1[j+1] * prevSmp[7];

                if (j > 0)
                {
                    for(int k = j-1; k > -1; k--)
                    {
                        total += (temp[((j - 1) - k)] * predArr2[k]);
                    }
                }

                float result = temp[j] + (total >> 0xb);
	
                if (result > 32767)
                    sample = 32767;
                else if (result < -32768)
                    sample = -32768;
                else
                    sample = (short)result;

                decData[j] = (short)sample;

                audioData.Add((byte)((decData[j] >> 8)));
                audioData.Add((byte)decData[j]);
            }
            prevSmp = decData;

            return;
        }
        short SignExtend(ushort b, // number of bits representing the number in x
						int x      // sign extend this b-bit number to r
)
{
	

	int m = 1 << (b - 1); // mask can be pre-computed if b is fixed

	x = x & ((1 << b) - 1);  // (Skip this if bits in x above position b are already zero.)
    short result = (short)((x ^ m) - m);
    //throw new NotImplementedException();
            return result;
}
    }

}
