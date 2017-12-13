using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ElfReader.Dwarf;
using ElfUtils;

namespace ElfReader.Elf {
    public class ElfSection {
        private string _name;
        public Elf32SectionHeader Header;
        private List<ElfSymbol> _symbols = new List<ElfSymbol>();
        public List<DwarfCompilationUnitItem> Items = new List<DwarfCompilationUnitItem>();
        public Elf32ProgramHeader? ProgramHeader { get; set; }

        public override string ToString() {
            return string.Format("{0}: s={1}", _name, Header.size);
        }

        public bool IsInsideSection(uint bytePosition) {
            if (bytePosition >= Header.addr) {
                return bytePosition < Header.addr + Header.size;
            }
            return false;
        }

        public ulong ItemsTotalSize {
            get {
                return Items.Aggregate<DwarfCompilationUnitItem, ulong>(0, (current, itm) => current + itm.Size);
            }
        }

        public uint SymbolsTotalSize {
            get {
                return _symbols.Aggregate<ElfSymbol, uint>(0, (current, elf32Symbol) => current + elf32Symbol.Header.Size);
            }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public List<ElfSymbol> Symbols
        {
            get { return _symbols; }
            set { _symbols = value; }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Elf32SectionHeader {
        public uint name;
        public Elf32SectionType type;
        public Elf32SectionFlags flags;
        public uint addr;
        public uint offset;
        public uint size;
        public uint link;
        public uint info;
        public uint addralign;
        public uint entsize;

        public bool UsingFlashMemory {
            get { return type != Elf32SectionType.SHT_NOBITS; }
        }
    }

    public enum Elf32SectionType : uint {
        SHT_NULL = 0,
        SHT_PROGBITS = 1,
        SHT_SYMTAB = 2,
        SHT_STRTAB = 3,
        SHT_RELA = 4,
        SHT_HASH = 5,
        SHT_DYNAMIC = 6,
        SHT_NOTE = 7,
        SHT_NOBITS = 8,
        SHT_REL = 9,
        SHT_SHLIB = 10,
        SHT_DYNSYM = 11,
        SHT_LOPROC = 0x70000000,
        SHT_HIPROC = 0x7fffffff,
        SHT_LOUSER = 0x80000000,
        SHT_HIUSER = 0xffffffff
    }

    [Flags]
    public enum Elf32SectionFlags : uint {
        SHF_WRITE = 1,//The section contains data that should be writable during process execution.
        SHF_ALLOC = 2,//The section occupies Memory during process execution.
        SHF_EXECINSTR = 4,//The section contains executable machine instructions.
        SHF_MASKPROC = 0xf0000000
    }
}
