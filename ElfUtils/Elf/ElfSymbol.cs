using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ElfUtils;

namespace ElfReader.Elf {
    public class ElfSymbol {
        public Elf32SymbolHeader _header;
        private string _name;

        public Elf32SymbolHeader Header
        {
            get { return _header; }
            set { _header = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public override string ToString() {
            return Name;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Elf32SymbolInfo {
        public byte val;

        public Elf32SymbolBinding Binding {
            get { return (Elf32SymbolBinding)(val >> 4); }
            set { val = (byte)((val & 0xf) | ((byte)value << 4)); }
        }

        public Elf32SymbolType SymbolType {
            get { return (Elf32SymbolType)(val & 0xf); }
            set { val = (byte)((val & 0xf0) | ((byte)value & 0xf)); }
        }

        public override string ToString() {
            return string.Format("{0} | {1}", Binding, SymbolType);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Elf32SymbolHeader {
        public uint nameOffset;
        public uint ValueAddr { get; set; }
        public uint Size { get; }
        public Elf32SymbolInfo Info { get; set; }
        public byte other;
        public ushort shndx;
    }

    public enum Elf32SymbolBinding : byte {
        STB_LOCAL   = 0,
        STB_GLOBAL  = 1,
        STB_WEAK    = 2,
        STB_LOPROC  = 13,
        STB_HIPROC  = 15
    }

    public enum Elf32SymbolType : byte {
        STT_NOTYPE  = 0,
        STT_OBJECT  = 1,
        STT_FUNC    = 2,
        STT_SECTION = 3,
        STT_FILE    = 4,
        STT_LOPROC  = 13,
        STT_HIPROC  = 15
    }

}
