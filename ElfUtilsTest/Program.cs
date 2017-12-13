using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElfReader;
using ElfReader.Dwarf;
using ElfReader.Elf;
using ElfUtils;

namespace ElfUtilsTest {
    class Program {

        /*static uint GetMemorySize(Elf32File file, string memory) {
            var flashSections = file.GetSectionsByMemoryType(memory);
            var dataItems = flashSections.SelectMany(v => v.Items);
            return (uint)dataItems.Aggregate<DwarfCompilationUnitItem, ulong>(0, (current, item) => current + item.Size);
        }*/

        static uint GetSectionSize(ElfFile file, string section) {
            var dataSection = file.GetSection(section);
            var dataItems = dataSection.Items;
            return (uint)dataItems.Aggregate<DwarfCompilationUnitItem, ulong>(0, (current, item) => current + item.Size);
        }

        static void PrintSectionFiles(ElfSection section) {
            var items = section.Items;

            var fileGroups = items.GroupBy(v => Path.GetFileName(v.FileStr));
            foreach (var f in fileGroups) {
                Console.WriteLine("\tFile: {0}", f.Key);

                ulong totalSize = 0;
                foreach (var dwarfCompilationUnitItem in f) {
                    totalSize += dwarfCompilationUnitItem.Size;
                    Console.WriteLine("\t\tItem: {0}\ts:{1}", dwarfCompilationUnitItem.Name, dwarfCompilationUnitItem.Size);
                }
                Console.WriteLine("\tFile size: {0}", totalSize);
            }
        }

        static void Main(string[] args) {
            //string linkerScriptPath = @"C:/Users/Sergey/AppData/Local/VisualGDB/EmbeddedBSPs/arm-eabi/com.sysprogs.arm.stm32/STM32F4xxxx-HAL/LinkerScripts/STM32F439xI_flash.lds";
           string elfFilePath = @"D:\MotionParallaxResearch\NMarker\V03\Software\ATmega328P\ATmega328P\ATmega328P\Debug\ATmega328P.elf";
            MemoryDescriptor mem = null;// MemoryDescriptor.FromLinkerScript(linkerScriptPath);


            ElfFile elf = new ElfFile(File.ReadAllBytes(elfFilePath));
            DwarfData dwData = new DwarfData(elf);

            MemoryProfile profile = new MemoryProfile(dwData, mem);

            var flashSections = profile.GetFlashSections();
            var progSections = profile.GetProgramSections();

           


            var memNames = profile.GetMemoryNames();
            foreach (var name in memNames) {
                var sections = profile.GetSectionsByMemoryEx(name);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Memory: {0}", name);

                ulong totalSize = 0;
                foreach (var section in sections) {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\t{0} : {1}", section.Name, section.Header.size);
                    totalSize += section.Header.size;

                    //print symbols
                    var fileGroups = section.Items.GroupBy(v => Path.GetFileName(v.FileStr)).ToArray();
                    foreach (var fileGroup in fileGroups) {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine("\t\t{0} : ", fileGroup.Key);
                        Console.ResetColor();

                        foreach (var item in fileGroup) {
                            var p1 = string.Format("\t\t\t{0}:", item.Name);
                            if(p1.Length < 14) {
                                p1 += "\t";
                            }
                            if(p1.Length < 25) {
                                p1 += "\t";
                            }
                            Console.WriteLine(p1 + string.Format("\t{0}:\t0x{1:x}", item.Size, item.Pc));
                        }
                    }
                }
                Console.WriteLine("\tTotal size: {0}", totalSize);
            }
            

            var flashSize = flashSections.Aggregate<ElfSection, ulong>(0, (current, item) => current + item.Header.size);

            var prgSize = progSections.Aggregate<ElfSection, ulong>(0, (current, item) => current + item.Header.size);

            Console.ReadLine();
        }
    }
}
