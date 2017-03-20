using System.Collections.Generic;
using DICOMSharp.Data.Transfers;
using DICOMSharp.Network.Abstracts;
using DICOMSharp.Util;
using DICOMSharp.Data;

namespace DICOMSharp.Network.Presentations
{
    /// <summary>
    /// Represents a DICOM Presentation Context.
    /// </summary>
    public class PresentationContext
    {
        /// <summary>
        /// Creates an empty PresentationContext object.
        /// </summary>
        public PresentationContext()
        {
            TransferSyntaxesProposed = new List<TransferSyntax>();
            TransferSyntaxAccepted = null;
        }

        /// <summary>
        /// Creates a Presentation Context with an Abstract syntax and a list of Transfer Syntaxes
        /// </summary>
        /// <param name="abstractSyntax">An Abstract Syntax</param>
        /// <param name="transferSyntaxes">A list of TransferSyntaxes to provide as options for the Abstract Syntax</param>
        public PresentationContext(AbstractSyntax abstractSyntax, ICollection<TransferSyntax> transferSyntaxes)
            : this()
        {
            AbstractSyntaxSpecified = abstractSyntax;
            TransferSyntaxesProposed.AddRange(transferSyntaxes);
        }

        internal PresentationContext(SwappableBinaryReader dataSource, int itemLength)
            : this()
        {
            ParsePacket(dataSource, itemLength);
        }

        internal void ParsePacket(SwappableBinaryReader dataSource, int itemLength)
        {
            long startOffset = dataSource.BaseStream.Position;

            ContextID = dataSource.ReadByte();
            byte reserved = dataSource.ReadByte();
            Result = (PresentationResult)dataSource.ReadByte();
            reserved = dataSource.ReadByte();

            while (dataSource.BaseStream.Position - startOffset < itemLength)
            {
                //read sub-item
                byte subItemType = dataSource.ReadByte();
                byte subItemReserved = dataSource.ReadByte();
                ushort subItemLength = dataSource.ReadUInt16();

                if (subItemType == 0x30)
                {
                    string rawSyntax = Uid.UidRawToString(dataSource.ReadBytes(subItemLength));
                    AbstractSyntaxSpecified = AbstractSyntaxes.Lookup(rawSyntax);
                }
                else if (subItemType == 0x40)
                {
                    string rawSyntax = Uid.UidRawToString(dataSource.ReadBytes(subItemLength));
                    TransferSyntax syntax = TransferSyntaxes.Lookup(rawSyntax);
                    if (syntax != null)
                        TransferSyntaxesProposed.Add(syntax);
                }
                else
                {
                    //no idea what it is, or we don't care
                    dataSource.ReadBytes(itemLength);
                }
            }
        }

        /// <summary>
        /// The Context ID for the Presentation Context
        /// </summary>
        public byte ContextID { get; set; }
        /// <summary>
        /// The abstract syntax for the Presentation Context
        /// </summary>
        public AbstractSyntax AbstractSyntaxSpecified { get; set; }
        /// <summary>
        /// Whether or not the presentation context was accepted
        /// </summary>
        public PresentationResult Result { get; set; }
        /// <summary>
        /// A list of allowed transfer syntaxes being proposed
        /// </summary>
        public List<TransferSyntax> TransferSyntaxesProposed { get; private set; }
        /// <summary>
        /// The transfer syntax that was accepted during association negotiation
        /// </summary>
        public TransferSyntax TransferSyntaxAccepted { get; set; }
    }
}
