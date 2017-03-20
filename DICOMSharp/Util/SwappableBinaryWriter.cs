using System;
using System.IO;
using DICOMSharp.Data;
using System.Text;

namespace DICOMSharp.Util
{
    internal class SwappableBinaryWriter : BinaryWriter
    {
        public SwappableBinaryWriter(Stream stream)
            : base(stream)
        {
            swapped = false;
        }

        public SwappableBinaryWriter(Stream stream, bool swapped)
            : base(stream)
        {
            this.swapped = swapped;
        }

        public void ToggleSwapped()
        {
            swapped ^= true;
        }

        public void WriteWords(byte[] data)
        {
            if (swapped)
            {
                //Ugh... copy to a temp array and then write it.
                int length = data.Length;
                byte[] newstr = new byte[length];
                for (int i = 0; i < length; i += 2)
                {
                    newstr[i] = data[i + 1];
                    newstr[i + 1] = data[i];
                }
                Write(newstr);
            }
            else
            {
                //Just dump it out as bytes.
                Write(data);
            }
        }

        public override void Write(short value)
        {
            if (swapped)
                base.Write((short)MSBSwapper.SwapW(value));
            else
                base.Write(value);
        }

        public override void Write(ushort value)
        {
            if (swapped)
                base.Write(MSBSwapper.SwapW(value));
            else
                base.Write(value);
        }

        public override void Write(int value)
        {
            if (swapped)
                base.Write((int)MSBSwapper.SwapDW(value));
            else
                base.Write(value);
        }

        public override void Write(uint value)
        {
            if (swapped)
                base.Write(MSBSwapper.SwapDW(value));
            else
                base.Write(value);
        }

        public override void Write(double value)
        {
            if (swapped)
                base.Write(MSBSwapper.SwapL(BitConverter.DoubleToInt64Bits(value)));    //can just write it as a long for speed's sake...
            else
                base.Write(value);
        }

        public override void Write(float value)
        {
            //RxIMP: See if there's a less retarded way to do this
            if (swapped)
                base.Write(MSBSwapper.SwapDW(BitConverter.ToUInt32(BitConverter.GetBytes(value), 0)));
            else
                base.Write(value);
        }

        public override void Write(ulong value)
        {
            if (swapped)
                base.Write(MSBSwapper.SwapL(value));
            else
                base.Write(value);
        }

        public override void Write(long value)
        {
            if (swapped)
                base.Write(MSBSwapper.SwapL(value));
            else
                base.Write(value);
        }

        public void Write(Uid value)
        {
            this.Write(value.ToBytes());
        }

        private bool swapped;
    }
}
