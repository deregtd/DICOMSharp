using System;
using System.IO;
using DICOMSharp.Data;

namespace DICOMSharp.Util
{
    internal class SwappableBinaryReader : BinaryReader
    {
        public SwappableBinaryReader(Stream stream)
            : base(stream)
        {
            swapped = false;
        }
        public SwappableBinaryReader(Stream stream, bool swapped)
            : base(stream)
        {
            this.swapped = swapped;
        }

        public void ToggleSwapped()
        {
            swapped ^= true;
        }

        public ushort ReadUInt16MSB()
        {
            //Always do it in MSB, for VRs
            return base.ReadUInt16();
        }

        public byte[] ReadBytesUpToMax(int length)
        {
            try
            {
                // See if we're going to go out of bounds
                long lengthLeft = this.BaseStream.Length - this.BaseStream.Position;
                if (lengthLeft < length)
                {
                    length = (int) lengthLeft;
                }
            }
            catch (Exception)
            {
                // Not supported, will just have to try it out
            }

            return this.ReadBytes(length);
        }

        public byte[] ReadWords(int length)
        {
            byte[] data = ReadBytes(length);

            if (swapped)
            {
                for (int i = 0; i < length; i += 2)
                {
                    byte temp = data[i];
                    data[i] = data[i + 1];
                    data[i + 1] = temp;
                }
            }

            return data;
        }

        public override ushort ReadUInt16()
        {
            if (swapped)
                return MSBSwapper.SwapW(base.ReadUInt16());
            else
                return base.ReadUInt16();
        }

        public override uint ReadUInt32()
        {
            if (swapped)
                return MSBSwapper.SwapDW(base.ReadUInt32());
            else
                return base.ReadUInt32();
        }

        public override float ReadSingle()
        {
            //RxIMP: See if there's a less retarded way to do this
            if (swapped)
                return BitConverter.ToSingle(BitConverter.GetBytes(MSBSwapper.SwapDW(base.ReadUInt32())), 0);
            else
                return base.ReadSingle();
        }

        public override double ReadDouble()
        {
            if (swapped)
                return BitConverter.Int64BitsToDouble(MSBSwapper.SwapL(BitConverter.DoubleToInt64Bits(base.ReadDouble())));
            else
                return base.ReadDouble();
        }

        private bool swapped;
    }
}
