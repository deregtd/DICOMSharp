using System.IO;
using DICOMSharp.Network.Connections;

namespace DICOMSharp.Util
{
    internal class AssociationItemBuilder : SwappableBinaryWriter
    {
        public AssociationItemBuilder(AssociationItemType type)
            : base(new MemoryStream(), true)
        {
            Write((byte)type);  //application context item
            Write((byte)0);     //reserved - 0
            Write((short)0);    //item length temp
        }

        private void SetWordLength()
        {
            BaseStream.Seek(2, SeekOrigin.Begin);
            Write((ushort)(BaseStream.Length - 4));
        }

        public byte[] Build()
        {
            SetWordLength();
            return ((MemoryStream)BaseStream).ToArray();
        }
    }
}
