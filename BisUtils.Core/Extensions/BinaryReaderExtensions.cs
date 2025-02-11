﻿
// ReSharper disable once CheckNamespace

using System.Diagnostics;
using System.Text;
using BisUtils.Core.Compression.Options;
using BisUtils.Core.Serialization;

// ReSharper disable once CheckNamespace
namespace System.IO 
{
    public static class BinaryReaderExtensions 
    {
        public static uint ReadUInt24(this BinaryReader reader) => (uint)(reader.ReadByte() + (reader.ReadByte() << 8) + (reader.ReadByte() << 16));
        
        public static int ReadCompactInteger(this BinaryReader reader) 
        {
            var value = 0;
            for (var i = 0;; ++i) 
            {
                var v = reader.ReadByte();
                value |= v & 0x7F << (7 * i);
                if((v & 0x80) == 0) 
                    break;
            }

            return value;
        }
        
        public static string ReadAsciiZ(this BinaryReader reader) 
        {
            var bytes = new List<byte>();
            byte b;
            while ((b = reader.ReadByte()) != '\0') bytes.Add(b);
            return Encoding.UTF8.GetString(bytes.ToArray());
        }
        
        public static byte[] ReadCompressedIndices(this BinaryReader reader, int bytesToRead, uint expectedSize)
        {
            var result = new byte[expectedSize];
            var outputI = 0;
            for(var i = 0; i < bytesToRead; i++)
            {
                var b = reader.ReadByte();
                if( (b & 128) != 0 )
                {
                    var n = (byte)(b - 127);
                    var value = reader.ReadByte();
                    for (var j = 0; j < n; j++) result[outputI++] = value;
                }
                else
                {
                    for (var j = 0; j < b + 1; j++) result[outputI++] = reader.ReadByte();
                }
            }

            Debug.Assert(outputI == expectedSize);

            return result;
        }

        public static T ReadBinarized<T>(this BinaryReader reader) where T : IBisBinarizable, new() => (T) new T().ReadBinary(reader);

        public static MemoryStream ReadCompressedData<T>(this BinaryReader reader, BisDecompressionOptions options) {
            var decompressedDataStream = new MemoryStream();
            var decompressedDataWriter = new BinaryWriter(decompressedDataStream, Encoding.UTF8);

            typeof(T).GetMethod("Decompress")!.Invoke(null, new object[] {
                new MemoryStream(reader.ReadBytes(options.ExpectedSize)),
                decompressedDataWriter,
                options
            });

            return decompressedDataStream;
        }
        
        public static bool AssertMagic(this BinaryReader reader, string magic) => 
            new string(reader.ReadChars(magic.Length)).Equals(magic);

        public static int ReadInt32BE(this BinaryReader reader) {
            var data = reader.ReadBytes(4);
            Array.Reverse(data);
            
            return BitConverter.ToInt32(data, 0);
        } 

    }
}

