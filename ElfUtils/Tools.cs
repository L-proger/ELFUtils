using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ElfUtils {
    public static class Tools {
        public static T FromByteArray<T>(byte[] data) where T : struct {
            var bufferSize = Marshal.SizeOf(typeof(T));
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            var result = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();
            return result;
        }

        public static T ReadStructure<T>(this Stream stm) where T : struct {
            var buffer = stm.ReadBuffer(Marshal.SizeOf(typeof (T)));
            if (buffer == null) {
                throw new Exception("Can't read structure from stream");
            }
            return FromByteArray<T>(buffer);
        }

        public static bool ReadStructure<T>(this Stream stm, ref T result) where T : struct {
            var buffer = stm.ReadBuffer(Marshal.SizeOf(typeof(T)));
            if (buffer == null) {
                return false;
            }
            result = FromByteArray<T>(buffer);
            return true;
        }

        public static byte[] ReadBuffer(this Stream stm, int length) {
            if (stm == null || !stm.CanRead || (stm.Length - stm.Position < length) || length <= 0) {
                return null;
            }
            var result = new byte[length];
            int done = 0;
            while (done != length) {
                var cntRead = stm.Read(result, done, length - done);
                if (cntRead <= 0) { //end of stream, not full buffer read!
                    return null;
                }
                done += cntRead;
            }
            return result;
        }

        public static byte[] ReadBytesCount(this Stream stm, int count) {
            byte[] data = new byte[count];
            for (int i = 0; i < count; ++i) {
                data[i] = (byte)stm.ReadByte();
            }
            return data;
        }

        public static bool IsEnumValue(Type enumType, long value) {
            return Enum.GetValues(enumType).Cast<object>().Any(a => Convert.ToInt64(a) == value);
        }

        public static ulong ReadLeb128Unsigned(this Stream stm) {
            ulong result = 0U;
            int shift = 0;
            while (true) {
                var data = stm.ReadByte();
                if (data < 0) {
                    throw new Exception("Can't reab byte from stream");
                }
                byte b = (byte)data;
                result |= (uint)((b & 0x7f) << shift);
                if ((b & 0x80) != 0)
                    shift += 7;
                else
                    break;
            }
            return result;
        }

        public static int ReadLeb128Signed(this Stream stm) {
            int result = 0;
            int shift = 0;
            while (true) {
                var data = stm.ReadByte();
                if (data < 0) {
                    throw new Exception("Can't reab byte from stream");
                }
                byte b = (byte)data;
                result |= (b & sbyte.MaxValue) << shift;
                if ((b & 0x80) != 0)
                    shift += 7;
                else
                    break;
            }
            if ((result & 1 << shift + 6) != 0)
                result |= ~((1 << shift + 6) - 1);
            return result;
        }

        public static string ReadAnsiString(this Stream stm) {
            var data = new List<byte>();
            byte b;
            do {
                var d = stm.ReadByte();
                if (d < 0) {
                    throw new Exception("Can't reab byte from stream");
                }
                b = (byte)d;
                if (b != 0) {
                    data.Add(b);
                }
            } while (b != 0);
            return Encoding.ASCII.GetString(data.ToArray(), 0, data.Count);
        }
    }
}
