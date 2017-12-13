using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
/*
ELFCLASS32 Addr      4 4     uint
ELFCLASS32 Half      2 2     ushort
ELFCLASS32 Off       4 4     uint
ELFCLASS32 Sword     4 4     int
ELFCLASS32 Word      4 4     uint
unsigned char        1 1     byte */

/*
uint64_t Elf64_Addr;
uint64_t Elf64_Off;
uint16_t Elf64_Half;
uint32_t Elf64_Word;
 int32_t Elf64_Sword;
uint64_t Elf64_Xword;
 int64_t Elf64_Sxword;
 int16_t Elf64_Section;*/

/*
uint32_t	Elf32_Addr;
uint16_t	Elf32_Half;
uint32_t	Elf32_Off;
int32_t		Elf32_Sword;
uint32_t	Elf32_Word;
uint64_t	Elf32_Lword;
Elf32_Word	Elf32_Hashelt;
Elf32_Word  Elf32_Size;
Elf32_Sword Elf32_Ssize;*/

namespace ElfReader.Elf {

    public enum ProgramHeaderType : uint {
        PT_NULL = 0,
        PT_LOAD = 1,
        PT_DYNAMIC = 2,
        PT_INTERP = 3,
        PT_NOTE = 4,
        PT_SHLIB = 5,
        PT_PHDR = 6,
        PT_TLS = 7,
        PT_LOOS = 0x60000000,
        PT_HIOS = 0x6fffffff,
        PT_LOPROC = 0x70000000,
        PT_HIPROC = 0x7fffffff
    }

    [Flags]
    public enum ProgramHeaderFlags : uint {
        PF_X = 0x1,                 //Execute,
        PF_W = 0x2,	                //Write,
        PF_R = 0x4,	                //Read,
        PF_MASKOS = 0x0ff00000,	    //Unspecified,
        PF_MASKPROC = 0xf0000000    //Unspecified
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Elf64ProgramHeader {
        public ProgramHeaderType p_type;
        public ProgramHeaderFlags p_flags;
        public UInt64 p_offset; //Offset from the beginning of the file at which the first byte of the segment resides
        public UInt64 p_vaddr;  //Virtual address at which the first byte of the segment resides in memory
        public UInt64 p_paddr;  //On systems for which physical addressing is relevant, this member is reserved for the segment's physical address
        public UInt64 p_filesz; //Number of bytes in the file image of the segment
        public UInt64 p_memsz;  //Number of bytes in the memory image of the segment; it may be zero
        public UInt64 p_align;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Elf32ProgramHeader {
        private ProgramHeaderType p_type;  
        private UInt32 p_offset;
        private UInt32 p_vaddr;
        private UInt32 p_paddr; 
        private UInt32 p_filesz;
        private UInt32 p_memsz; 
        private ProgramHeaderFlags p_flags; 
        private UInt32 p_align;

        public ProgramHeaderType PType
        {
            get { return p_type; }
            set { p_type = value; }
        }

        public uint POffset
        {
            get { return p_offset; }
            set { p_offset = value; }
        }

        public uint PVaddr
        {
            get { return p_vaddr; }
            set { p_vaddr = value; }
        }

        public uint PPaddr
        {
            get { return p_paddr; }
            set { p_paddr = value; }
        }

        public uint PFilesz
        {
            get { return p_filesz; }
            set { p_filesz = value; }
        }

        public uint PMemsz
        {
            get { return p_memsz; }
            set { p_memsz = value; }
        }

        public ProgramHeaderFlags PFlags
        {
            get { return p_flags; }
            set { p_flags = value; }
        }

        public uint PAlign
        {
            get { return p_align; }
            set { p_align = value; }
        }
    }
}
