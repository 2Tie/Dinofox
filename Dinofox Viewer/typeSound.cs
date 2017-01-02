using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dinofox_Viewer
{
    class typeSound
    {
        public byte pan, volume, flags;
        public UInt32 envelopeAddress, keymapAddress, wavetableAddress;
        public envelopeST envelope;
        public keymapST keymap;
        public waveTableST wavetable;

        public struct envelopeST
        {
            public UInt32 attackTime, decayTime, releaseTime;
            public UInt16 attackVolume, decayVolume;
        }

        public struct keymapST
        {
            public byte velocityMin, velocityMax, keyMin, keyMax, keyBase, detune;
        }

        public struct waveTableST
        {
            public List<UInt16> predictors;// = new List<UInt16>();
            public List<UInt16> state;// = new List<UInt16>();
            public List<byte> waveData;// = new List<byte>();
            public UInt32 waveBase, waveLength, loopAddress, predictorAddress, start, end, count, order, numPredictors;
            public byte type, flags;
        }
    }
}
