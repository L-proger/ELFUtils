using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ElfReader.Dwarf;
using ElfUtils;

/*
ELFCLASS32 Addr      4 4     uint
ELFCLASS32 Half      2 2     ushort
ELFCLASS32 Off       4 4     uint
ELFCLASS32 Sword     4 4     int
ELFCLASS32 Word      4 4     uint
unsigned char        1 1     byte */

namespace ElfReader.Elf {
    public class ElfFile {
        public Stream DataStream { get; private set; }
        public Elf32Header header;
        public Elf32Header Header => header;
        public List<Elf32ProgramHeader> programHeaders = new List<Elf32ProgramHeader>(); 

        public List<ElfSection> Sections
        {
            get { return sections; }
            set { sections = value; }
        }

        private List<ElfSection> sections;
        
        public ElfFile(Stream fileStream) {
            Parse(fileStream);
        }

        public ElfFile(byte[] fileBytes) {
            if(fileBytes == null || fileBytes.Length < Elf32Header.StructSize) {
                throw new ArgumentException("Invalid file", "fileBytes");
            }
            Parse(new MemoryStream((byte[])fileBytes.Clone()));
        }

        private void Parse(Stream stream) {
            if(stream == null || stream.Length < Elf32Header.StructSize) {
                throw new ArgumentException("Invalid file stream", "stream");
            }
            DataStream = stream;

            //read header
            header = DataStream.ReadStructure<Elf32Header>();
            if(!header.IdentProp.IsMagicCorrect) {
                throw new Exception("ELF Magic is incorrect");
            }

            //read program headers
            for(int i = 0; i < header.phnum; ++i) {
                DataStream.Position = header.phoff + i * header.phentsize;
                programHeaders.Add( DataStream.ReadStructure<Elf32ProgramHeader>());
            }

            ReadSections();

            //find ph for sections
            foreach (var section in sections)
            {
                try
                {
                    var h =  programHeaders.First( v => v.POffset == section.Header.offset);
                    section.ProgramHeader = h;
                }
                catch{
                    
                }
            }
            ReadSymbols();
        }

        private void ReadSections() {
            //read sections
            sections = new List<ElfSection>();
            for(int i = 0; i < header.shnum; ++i) {
                DataStream.Position = header.shoff + i * header.shentsize;
                sections.Add(new ElfSection { Header = DataStream.ReadStructure<Elf32SectionHeader>() });
            }

            //read section names
            foreach(var section in sections) {
                DataStream.Position = GetStringTableSection().Header.offset + section.Header.name;
                section.Name = DataStream.ReadAnsiString();
            }
        }

        private void ReadSymbols() {
            //parse Symbols
            var symbolSections = sections.Where(v => v.Header.type == Elf32SectionType.SHT_SYMTAB).ToArray();

            foreach(var symbolSection in symbolSections) {
                var symbolHeaderSize = Marshal.SizeOf(typeof(Elf32SymbolHeader));
                var symbolsCount = symbolSection.Header.size / symbolHeaderSize;
                var stringTable = GetSection(DwarfSections.StringTable);

                //skip 0 symbol cuz it is always empty
                for(int i = 1; i < symbolsCount; ++i) {
                    DataStream.Seek(symbolSection.Header.offset + i * symbolHeaderSize, SeekOrigin.Begin);
                    var symbol = new ElfSymbol { Header = DataStream.ReadStructure<Elf32SymbolHeader>() };

                    //skip no-type Symbols
                    if(symbol.Header.Info.SymbolType == Elf32SymbolType.STT_NOTYPE) {
                        continue;
                    }

                    DataStream.Position = symbol.Header.nameOffset + stringTable.Header.offset;
                    symbol.Name = DataStream.ReadAnsiString();
                    if(symbol.Header.shndx < sections.Count) {
                        sections[symbol.Header.shndx].Symbols.Add(symbol);
                    }
                }
            }
        }
       
        public int GetSectionId(ulong pc) {
            for (int i = 0; i < sections.Count; ++i) {
                if (sections[i].IsInsideSection((uint)pc))
                    return i;
            }
            return -1;
        }

        public ElfSection GetSection(string name) {
            return sections.FirstOrDefault(v => v.Name == name);
        }

        public ElfSection GetStringTableSection() {
            return sections[header.shstrndx];
        }
    }


}
