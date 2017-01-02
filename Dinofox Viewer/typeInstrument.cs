using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dinofox_Viewer
{
    class typeInstrument
    {
        public byte volume, pan, priority, flags, tremType, tremRate, tremDepth, tremDelay, vibType, vibRate, vibDepth, vibDelay;
        public UInt16 bendRange, soundCount;

        public List<typeSound> sounds = new List<typeSound>();
    }
}
