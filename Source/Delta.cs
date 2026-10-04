using System;
using System.IO;
using System.IO.Compression;

// MLD1: little-endian sizes; opcode 1 = original copy, 2 = literal, 0 = end.
public static class MaoDelta {
    public static void Apply(string originalPath, string deltaPath, string outputPath) {
        byte[] original = File.ReadAllBytes(originalPath);
        using (FileStream source = File.OpenRead(deltaPath))
        using (GZipStream gzip = new GZipStream(source, CompressionMode.Decompress))
        using (BinaryReader reader = new BinaryReader(gzip))
        using (FileStream output = new FileStream(outputPath, FileMode.CreateNew)) {
            if (reader.ReadUInt32() != 0x31444c4d) throw new InvalidDataException("Invalid MLD1 header.");
            ulong originalSize = reader.ReadUInt64(), outputSize = reader.ReadUInt64();
            if (originalSize != (ulong)original.Length || outputSize > 268435456) throw new InvalidDataException("Invalid delta file sizes.");
            while (true) {
                byte opcode = reader.ReadByte();
                if (opcode == 0) break;
                if (opcode == 1) {
                    ulong offset = reader.ReadUInt64(); uint length = reader.ReadUInt32();
                    if (offset > (ulong)original.Length || length > (ulong)original.Length-offset || length > outputSize-(ulong)output.Position) throw new InvalidDataException("Copy range is out of bounds.");
                    output.Write(original, (int)offset, (int)length);
                } else if (opcode == 2) {
                    uint length = reader.ReadUInt32();
                    if (length > outputSize-(ulong)output.Position) throw new InvalidDataException("Literal range is out of bounds.");
                    byte[] literal = reader.ReadBytes((int)length);
                    if (literal.Length != length) throw new EndOfStreamException("Truncated literal.");
                    output.Write(literal, 0, literal.Length);
                } else throw new InvalidDataException("Unknown delta opcode.");
            }
            if ((ulong)output.Length != outputSize || gzip.ReadByte() != -1) throw new InvalidDataException("Invalid delta length.");
        }
    }
}
