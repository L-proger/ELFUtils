using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ElfReader.Elf {
    public enum Elf32FileType : ushort {
        ET_NONE = 0,
        ET_REL = 1,
        ET_EXEC = 2,
        ET_DYN = 3,
        ET_CORE = 4,
        ET_LOPROC = 0xff00,
        ET_HIPROC = 0xffff
    }

    public enum ElfClass : byte {
        ELFCLASSNONE = 0,
        ELFCLASS32 = 1,
        ELFCLASS64 = 2
    }

    public enum ElfDataEncoding : byte {
        ELFDATANONE = 0,
        ELFDATA2LSB = 1,
        ELFDATA2MSB = 2
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Elf32Header {
        [StructLayout(LayoutKind.Sequential)]
        public struct Ident {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] private byte[] magic;
            public ElfClass fileClass;
            public ElfDataEncoding dataEncoding;
            public byte fileVersion;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
            public byte[] _pad;
            public bool IsMagicCorrect {
                get {
                    if (Magic == null || Magic.Length != 4) {
                        return false;
                    }
                    return
                        Magic[0] == 0x7f &&
                        Magic[1] == 'E' &&
                        Magic[2] == 'L' &&
                        Magic[3] == 'F';
                }
            }

            public byte[] Magic
            {
                get { return magic; }
                set { magic = value; }
            }

            public ElfClass FileClass
            {
                get { return fileClass; }
                set { fileClass = value; }
            }

            public ElfDataEncoding DataEncoding
            {
                get { return dataEncoding; }
                set { dataEncoding = value; }
            }

            public byte FileVersion
            {
                get { return fileVersion; }
                set { fileVersion = value; }
            }

            public byte[] Pad
            {
                get { return _pad; }
                set { _pad = value; }
            }
        }
        public Ident ident;
        public Elf32FileType type;
        public Machine machine;
        public uint version;
        public uint entry;
        public uint phoff;
        public uint shoff;
        public uint flags;
        public ushort ehsize;
        public ushort phentsize;
        public ushort phnum;
        public ushort shentsize;
        public ushort shnum;
        public ushort shstrndx;

        public static int StructSize {
            get { return Marshal.SizeOf(typeof(Elf32Header)); }
        }

        public Ident IdentProp
        {
            get { return ident; }
            set { ident = value; }
        }

        public Elf32FileType FileType
        {
            get { return type; }
            set { type = value; }
        }

        public Machine Machine
        {
            get { return machine; }
            set { machine = value; }
        }

        public uint Version
        {
            get { return version; }
            set { version = value; }
        }
    }
}
