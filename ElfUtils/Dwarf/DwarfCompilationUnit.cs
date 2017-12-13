using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElfReader.Elf;
using ElfUtils;

namespace ElfReader.Dwarf {
    public class DwarfCompilationUnit {
        public ulong Length;
        public ushort Version;
        public ulong AbbrevOffset;
        public byte AddressSize;
        public DwarfAbbreviation Abbrev;
        public ulong DataOffset;
        public long DataEnd;
        public bool Is64Bit;
        public ulong LineNumberOffset;
        public string FileName;
        public string CompilationDirectory;
        public string[] Lines;
        public int PtrSize => Is64Bit ? 8 : 4;
        public List<DwarfSymbolDescpription> Symbols = new List<DwarfSymbolDescpription>();
        public List<DwarfCompilationUnitItem> Items = new List<DwarfCompilationUnitItem>();

        

        private void ParseItems(ElfFile file) {
            Items.Clear();
            #region ParseItems
            foreach (var symbol in Symbols) {
                var entry = symbol.Abbreviation;

                if (entry.tag == DwarfConstants.DwarfTag.DW_TAG_compile_unit) {//fill this class fields from compile unit abbreviation
                    foreach (var value in symbol.Values) {
                        if (value.attribute == DwarfConstants.DwarfAttribute.DW_AT_stmt_list)
                            LineNumberOffset = value.AsUlong();
                        if (value.attribute == DwarfConstants.DwarfAttribute.DW_AT_name)
                            FileName = (value as DwarfStringValue).value;
                        if (value.attribute == DwarfConstants.DwarfAttribute.DW_AT_comp_dir)
                            CompilationDirectory = (value as DwarfStringValue).value;
                    }
                } else if (entry.tag == DwarfConstants.DwarfTag.DW_TAG_subprogram) { //parse subprogram
                    var item = new DwarfCompilationUnitItem();
                    ulong hiPc = 0;
                    foreach (var value in symbol.Values) {
                        if (value.attribute == DwarfConstants.DwarfAttribute.DW_AT_name) {
                            item.Name = (value as DwarfStringValue).value;
                        } else if (value.attribute == DwarfConstants.DwarfAttribute.DW_AT_low_pc) {
                            item.Pc = value.AsUlong();
                        } else if (value.attribute == DwarfConstants.DwarfAttribute.DW_AT_high_pc) {
                            hiPc = value.AsUlong();
                        } else if (value.attribute == DwarfConstants.DwarfAttribute.DW_AT_linkage_name) {
                            item.LinkageName = (value as DwarfStringValue).value;
                        } else if (value.attribute == DwarfConstants.DwarfAttribute.DW_AT_decl_file) {
                            item.File = (long)value.AsUlong();
                        } else if (value.attribute == DwarfConstants.DwarfAttribute.DW_AT_decl_line) {
                            item.Line = (long)value.AsUlong();
                        }
                    }
                    if (item.Name != null && (long)item.Pc != 0L && (long)hiPc != 0L) {

                        item.Size = Version >= 4 || (long)hiPc == 0L ? (uint)hiPc : (uint)((long)hiPc - (long)item.Pc);
                    }
                    item.Entry = entry;
                    item.Symbol = symbol;
                    Items.Add(item);

                } else if (entry.tag == DwarfConstants.DwarfTag.DW_TAG_variable) { //parse variable
                    var item = new DwarfCompilationUnitItem();
                    ulong hiPc = 0;
                    foreach (var value in symbol.Values) {
                        if (value.attribute == DwarfConstants.DwarfAttribute.DW_AT_name) {
                            item.Name = (value as DwarfStringValue).value;
                        } else if (value.attribute == DwarfConstants.DwarfAttribute.DW_AT_low_pc) {
                            item.Pc = value.AsUlong();
                        } else if (value.attribute == DwarfConstants.DwarfAttribute.DW_AT_linkage_name) {
                            item.LinkageName = (value as DwarfStringValue).value;
                        } else if (value.attribute == DwarfConstants.DwarfAttribute.DW_AT_decl_file) {
                            item.File = (long)value.AsUlong();
                        } else if (value.attribute == DwarfConstants.DwarfAttribute.DW_AT_decl_line) {
                            item.Line = (long)value.AsUlong();
                        } else if (value.attribute == DwarfConstants.DwarfAttribute.DW_AT_high_pc) {
                            hiPc = value.AsUlong();
                        } else if (value.attribute == DwarfConstants.DwarfAttribute.DW_AT_location) {
                            if (value is DwarfAttribBufferValue) {
                                byte[] numArray = (value as DwarfAttribBufferValue).value;
                                if (numArray != null && numArray.Length >= 3 && numArray[0] == 3) {
                                    if (numArray.Length == 3)
                                        item.Pc = BitConverter.ToUInt16(numArray, 1);
                                    else if (numArray.Length == 5)
                                        item.Pc = BitConverter.ToUInt32(numArray, 1);
                                    else if (numArray.Length == 7)
                                        item.Pc = BitConverter.ToUInt64(numArray, 1);
                                }
                            }

                        }
                    }
                    if (item.Name != null && (long)item.Pc != 0L) {
                        item.Size = Version >= 4 || (long)hiPc == 0L ? (uint)hiPc : (uint)((long)hiPc - (long)item.Pc);
                    }
                    item.Entry = entry;
                    item.Symbol = symbol;
                    Items.Add(item);
                }
            }
            #endregion
        }

        public void ParseLines(ElfFile file) {
            var stm = file.DataStream;
            var lineSection = file.GetSection(DwarfSections.DebugLine);
            stm.Position = lineSection.Header.offset + (long)LineNumberOffset;

            ulong tmp = stm.ReadStructure<UInt32>();
            bool is64Bit = false;
            if (tmp == UInt32.MaxValue) {
                tmp = stm.ReadStructure<UInt64>();
                is64Bit = true;
            }

            ushort cuVersion = stm.ReadStructure<ushort>();

            //read abbrev table offset
            var abbrevOffset = is64Bit ? stm.ReadStructure<UInt64>() : stm.ReadStructure<UInt32>();
            if (cuVersion >= 4)
                stm.Seek(1, SeekOrigin.Current);

            stm.Seek(4, SeekOrigin.Current);
            int num4 = stm.ReadByte();

            for (int i = 1; i <= num4 - 1; ++i) {
                ulong num3 = stm.ReadLeb128Unsigned();
            }

            List<string> list1 = new List<string>();
            while (true) {
                string str = stm.ReadAnsiString();
                if (!string.IsNullOrEmpty(str))
                    list1.Add(str);
                else
                    break;
            }

            List<string> list2 = new List<string>();
            while (true) {
                string str1 = stm.ReadAnsiString();
                if (!string.IsNullOrEmpty(str1)) {
                    uint num3 = (uint)stm.ReadLeb128Unsigned();
                    int num5 = (int)stm.ReadLeb128Unsigned();
                    int num6 = (int)stm.ReadLeb128Unsigned();
                    string str2 = str1;
                    if ((int)num3 == 0)
                        str2 = CompilationDirectory + "/" + str1;
                    else if (num3 > 0U && num3 < (uint)list1.Count)
                        str2 = list1[(int)num3 - 1] + "/" + str1;
                    list2.Add(str2);
                } else
                    break;
            }

            Lines = list2.ToArray();

            foreach (var item in Items) {
                if (item.File >= 0) {
                    item.FileStr = Lines[item.File - 1];
                }
            }
        }

        public static DwarfCompilationUnit[] ReadCompilationUnits(ElfFile file) {
            var cuSection = file.GetSection(DwarfSections.DebugInfo);
            var stm = file.DataStream;
            //move stream to section start
            var sectionEnd = cuSection.Header.offset + cuSection.Header.size;
            var result = new List<DwarfCompilationUnit>();

            ulong readPosition = cuSection.Header.offset;

            //read compilation units
            while (readPosition < sectionEnd) {
                stm.Seek((long)readPosition, SeekOrigin.Begin);

                var unit = new DwarfCompilationUnit();
                bool is64BitUnit = false;

                //read unit length
                unit.Length = stm.ReadStructure<UInt32>();

                //unit len can be 64 bit, check that
                if (unit.Length == 0xffffffffU) {
                    unit.Length = stm.ReadStructure<UInt64>();
                    is64BitUnit = true;
                }

                readPosition = (ulong)stm.Position + unit.Length;
                unit.DataEnd = (long)readPosition;

                //read version
                unit.Version = stm.ReadStructure<UInt16>();
                //read abbrev offset
                unit.AbbrevOffset = is64BitUnit ? stm.ReadStructure<UInt64>() : stm.ReadStructure<UInt32>();
                //read address size
                unit.AddressSize = (byte)stm.ReadByte();

                //save offset to unit data
                unit.DataOffset = (ulong)stm.Position;
                unit.Is64Bit = is64BitUnit;

                result.Add(unit);
            }
            return result.ToArray();
        }

        public UInt64 ReadPointer(Stream stm) {
            byte[] buffer = new byte[AddressSize];
            stm.Read(buffer, 0, AddressSize);
            if (AddressSize <= 4) {
                return Tools.FromByteArray<UInt32>(buffer);
            } else {
                return Tools.FromByteArray<UInt64>(buffer);
            }
        }

        public override string ToString() {
            return
                Symbols[0].Values.Find(v => v.attribute == DwarfConstants.DwarfAttribute.DW_AT_name).ToString();
        }

        public void ReadValues(ElfFile file) {
            var stm = file.DataStream;
            stm.Position = (long)DataOffset;

            int hierarchyDepth = 0;

            //read all abbreviations
            while (stm.Position < DataEnd) {
                ulong abbreviationCode = stm.ReadLeb128Unsigned();
                if (abbreviationCode == 0) {
                    if (hierarchyDepth > 0) {
                        hierarchyDepth--;
                    }
                } else {
                    var symbol = new DwarfSymbolDescpription();
                    var entry = Abbrev.GetEntryByAbbreviationCode(abbreviationCode);
                    symbol.Abbreviation = entry;
                    Symbols.Add(symbol);

                    //read each entry value
                    foreach (var attribute in entry.attributes) {

                        //if (entry.tag != DwarfConstants.DwarfTag.DW_TAG_compile_unit) {
                        if (attribute.form == DwarfConstants.DwarfForm.DW_FORM_indirect) {
                            stm.ReadLeb128Unsigned();
                            continue;
                        }

                        switch (attribute.form) {
                            case DwarfConstants.DwarfForm.DW_FORM_strp:
                                var ptr = ReadPointer(stm);
                                var tmpPos = stm.Position;
                                var strSection = file.GetSection(DwarfSections.DebugStr);
                                if (strSection != null) {
                                    stm.Position = strSection.Header.offset + (uint) ptr;
                                    var str = stm.ReadAnsiString();
                                    symbol.Values.Add(new DwarfStringValue(attribute.attribute, str));
                                }
                                stm.Position = tmpPos;
                                break;
                            case DwarfConstants.DwarfForm.DW_FORM_block1:
                                symbol.Values.Add(new DwarfAttribBufferValue(attribute.attribute, stm, (byte)stm.ReadByte()));
                                break;
                            case DwarfConstants.DwarfForm.DW_FORM_block2:
                                symbol.Values.Add(new DwarfAttribBufferValue(attribute.attribute, stm, stm.ReadStructure<UInt16>()));
                                break;
                            case DwarfConstants.DwarfForm.DW_FORM_block4:
                                symbol.Values.Add(new DwarfAttribBufferValue(attribute.attribute, stm, (int)stm.ReadStructure<UInt32>()));
                                break;
                            case DwarfConstants.DwarfForm.DW_FORM_exprloc:
                            case DwarfConstants.DwarfForm.DW_FORM_block:
                                symbol.Values.Add(new DwarfAttribBufferValue(attribute.attribute, stm, (int)stm.ReadLeb128Unsigned()));
                                break;
                            case DwarfConstants.DwarfForm.DW_FORM_data1:
                                symbol.Values.Add(new DwarfScalarValue<byte>(attribute.attribute, (byte)stm.ReadByte()));
                                break;
                            case DwarfConstants.DwarfForm.DW_FORM_data2:
                                symbol.Values.Add(new DwarfScalarValue<UInt16>(attribute.attribute, stm.ReadStructure<UInt16>()));
                                break;
                            case DwarfConstants.DwarfForm.DW_FORM_data4:
                                symbol.Values.Add(new DwarfScalarValue<UInt32>(attribute.attribute, stm.ReadStructure<UInt32>()));
                                break;
                            case DwarfConstants.DwarfForm.DW_FORM_data8:
                                symbol.Values.Add(new DwarfScalarValue<UInt64>(attribute.attribute, stm.ReadStructure<UInt64>()));
                                break;
                            case DwarfConstants.DwarfForm.DW_FORM_addr:
                                if (AddressSize == 2) {
                                    symbol.Values.Add(new DwarfScalarValue<UInt16>(attribute.attribute, stm.ReadStructure<UInt16>()));
                                } else if (AddressSize == 4) {
                                    symbol.Values.Add(new DwarfScalarValue<UInt32>(attribute.attribute, stm.ReadStructure<UInt32>()));
                                } else if (AddressSize == 8) {
                                    symbol.Values.Add(new DwarfScalarValue<UInt64>(attribute.attribute, stm.ReadStructure<UInt64>()));
                                }
                                break;
                            case DwarfConstants.DwarfForm.DW_FORM_string:
                                symbol.Values.Add(new DwarfStringValue(attribute.attribute, stm.ReadAnsiString()));
                                break;
                            case DwarfConstants.DwarfForm.DW_FORM_flag:
                                symbol.Values.Add(new DwarfAttribBoolValue(attribute.attribute, stm.ReadByte() != 0));
                                break;
                            case DwarfConstants.DwarfForm.DW_FORM_sdata:
                                symbol.Values.Add(new DwarfScalarValue<Int64>(attribute.attribute, stm.ReadLeb128Signed()));
                                break;
                            case DwarfConstants.DwarfForm.DW_FORM_udata:
                                symbol.Values.Add(new DwarfScalarValue<UInt64>(attribute.attribute, stm.ReadLeb128Unsigned()));
                                break;
                            case DwarfConstants.DwarfForm.DW_FORM_ref_addr:
                                stm.Position += PtrSize;
                                break;
                            case DwarfConstants.DwarfForm.DW_FORM_ref1:
                                stm.Position += 1;
                                break;
                            case DwarfConstants.DwarfForm.DW_FORM_ref2:
                                stm.Position += 2;
                                break;
                            case DwarfConstants.DwarfForm.DW_FORM_ref4:
                                stm.Position += 4;
                                break;
                            case DwarfConstants.DwarfForm.DW_FORM_ref8:
                                stm.Position += 8;
                                break;
                            case DwarfConstants.DwarfForm.DW_FORM_sec_offset:
                                if (PtrSize == 4) {
                                    symbol.Values.Add(new DwarfScalarValue<UInt32>(attribute.attribute,
                                        stm.ReadStructure<UInt32>()));
                                } else {
                                    symbol.Values.Add(new DwarfScalarValue<UInt64>(attribute.attribute, stm.ReadStructure<UInt64>()));
                                }
                                break;
                            case DwarfConstants.DwarfForm.DW_FORM_ref_sig8:
                                symbol.Values.Add(new DwarfScalarValue<UInt64>(attribute.attribute, stm.ReadStructure<UInt64>()));
                                break;
                            case DwarfConstants.DwarfForm.DW_FORM_flag_present:
                                symbol.Values.Add(new DwarfAttribBoolValue(attribute.attribute, true));
                                break;
                            default:
                                throw new Exception("WTF");
                                break;

                        }
                    }
                    if (entry.hasChild != 0) {
                        ++hierarchyDepth;
                    }
                }
            }
            ParseItems(file);
            ParseLines(file);
        }
    }

    public class DwarfCompilationUnitItem {
        public string Name { get; set; }
        public ulong Pc;
        public ulong Size;
        public string LinkageName;
        public long File = -1;
        public long Line { get; set; } = -1;
        public DwarfAbbreviationEntry Entry;
        public DwarfSymbolDescpription Symbol;
        public string FileStr { get; set; }

        public bool HasLine() {
            if(!string.IsNullOrEmpty(FileStr))
                return Line != -1;
            else
                return false;
        }

        public DwarfCompilationUnitItem() {

        }

        public DwarfCompilationUnitItem(ElfSymbol symbol) {
            Name = symbol.Name;
            Pc = symbol.Header.ValueAddr;
            Size = symbol.Header.Size;
        }

        public override string ToString() {
            return string.Format("{0} : 0x{1:X} : size={2}", Name, Pc, Size);
        }
    }
}
