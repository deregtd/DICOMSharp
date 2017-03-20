using System.IO;
using DICOMSharp.Network.Connections;

namespace DICOMSharp.Util
{
    internal class PDUBuilder : SwappableBinaryWriter
    {
        public PDUBuilder(PDUType type)
            : base(new MemoryStream(), true)
        {
            Write((byte)type);  //pdu-type
            Write((byte)0);  //reserved - 0
            Write((uint)0);  //temp pdu-len
        }

        private void SetDWordLength()
        {
            BaseStream.Seek(2, SeekOrigin.Begin);
            Write((uint)(BaseStream.Length - 6));
        }

        public byte[] Build()
        {
            SetDWordLength();
            return ((MemoryStream)BaseStream).ToArray();
        }
    }
}
