using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapUploader.Vpk
{
    public class ArchiveMD5SectionEntry
    {
        /// <summary>
        /// Gets or sets the CRC32 checksum of this entry.
        /// </summary>
        public uint ArchiveIndex { get; set; }

        /// <summary>
        /// Gets or sets the offset in the package.
        /// </summary>
        public uint Offset { get; set; }

        /// <summary>
        /// Gets or sets the length in bytes.
        /// </summary>
        public uint Length { get; set; }

        /// <summary>
        /// Gets or sets the expected Checksum checksum.
        /// </summary>
        public byte[] Checksum { get; set; }
    }
}
