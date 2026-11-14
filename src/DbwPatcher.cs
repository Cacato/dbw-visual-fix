using System;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;

internal static class DbwPatcher
{
    private const uint ImageBase = 0x00400000;
    private const uint PatchRva = 0x009FD000;
    private const string Original = "Wow (Allow Interface Edits x64).exe";
    private const string Prototype = "Wow (DBW Fix).exe";
    // Exact x86 payload, documented instruction by instruction in payload.asm.txt.
    private static readonly byte[] Payload = ParseHex(""
            + "9C 60 8B CE E8 35 00 00 00 85 C0 0F 85 10 00 00 00 61 9D 8B 50 1C 6A 00 "
            + "8B CE FF D2 E9 C7 98 91 FF 8B 4C 24 04 8B 89 D0 00 00 00 8B 81 F8 00 00 "
            + "00 89 81 F4 00 00 00 61 9D E9 AA 98 91 FF 51 8B 81 D0 00 00 00 85 C0 0F "
            + "84 40 00 00 00 8B 90 F4 00 00 00 E8 39 00 00 00 39 E8 0F 84 2D 00 00 00 "
            + "8B 0C 24 83 B9 B4 00 00 00 00 0F 84 1D 00 00 00 8B 91 68 09 00 00 85 D2 "
            + "0F 84 0F 00 00 00 39 02 0F 85 07 00 00 00 B8 01 00 00 00 59 C3 31 C0 59 "
            + "C3 89 D5 81 FA 5B 5C 00 00 0F 84 41 00 00 00 81 FA 65 5C 00 00 0F 84 35 "
            + "00 00 00 81 FA 27 79 00 00 0F 84 33 00 00 00 81 FA 64 66 00 00 0F 84 31 "
            + "00 00 00 81 FA CE 79 00 00 0F 84 2F 00 00 00 81 FA DD 79 00 00 0F 84 23 "
            + "00 00 00 E9 C5 01 00 00 BB 00 00 00 00 E9 1E 00 00 00 BB 01 00 00 00 E9 "
            + "14 00 00 00 BB 02 00 00 00 E9 0A 00 00 00 BB 03 00 00 00 E9 00 00 00 00 "
            + "A1 E0 9C C7 00 85 C0 0F 84 90 01 00 00 8B 80 D0 2E 00 00 85 C0 0F 84 82 "
            + "01 00 00 8B 90 C0 00 00 00 85 D2 0F 84 74 01 00 00 39 51 30 0F 85 6B 01 "
            + "00 00 8B 90 C4 00 00 00 39 51 34 0F 85 5C 01 00 00 8B 81 D0 00 00 00 85 "
            + "C0 0F 84 4E 01 00 00 39 A8 F4 00 00 00 0F 85 42 01 00 00 8B B8 F8 00 00 "
            + "00 85 FF 0F 84 34 01 00 00 81 FD 5B 5C 00 00 0F 84 25 01 00 00 81 FD 65 "
            + "5C 00 00 0F 84 19 01 00 00 81 FD 64 66 00 00 0F 84 0D 01 00 00 81 FD 27 "
            + "79 00 00 0F 84 01 01 00 00 81 FD CE 79 00 00 0F 84 F5 00 00 00 81 FD DD "
            + "79 00 00 0F 84 E9 00 00 00 8B 91 D0 0D 00 00 83 FA FF 0F 84 14 00 00 00 "
            + "83 FA 10 0F 87 D4 00 00 00 8D B1 50 0C 00 00 E9 0C 00 00 00 8B 91 54 0C "
            + "00 00 8B B1 58 0C 00 00 85 F6 0F 84 B5 00 00 00 81 FA 00 01 00 00 0F 87 "
            + "A9 00 00 00 85 D2 0F 84 A1 00 00 00 8B 46 08 83 FB 00 0F 85 16 00 00 00 "
            + "3D 3C 17 01 00 0F 84 87 00 00 00 3D 89 17 01 00 0F 84 7C 00 00 00 83 FB "
            + "01 0F 85 16 00 00 00 3D 3E 17 01 00 0F 84 68 00 00 00 3D 86 17 01 00 0F "
            + "84 5D 00 00 00 83 FB 02 0F 85 16 00 00 00 3D 43 17 01 00 0F 84 49 00 00 "
            + "00 3D 87 17 01 00 0F 84 3E 00 00 00 83 FB 03 0F 85 2C 00 00 00 3D 3D 17 "
            + "01 00 0F 84 2A 00 00 00 3D 44 17 01 00 0F 84 1F 00 00 00 3D 84 17 01 00 "
            + "0F 84 14 00 00 00 3D 88 17 01 00 0F 84 09 00 00 00 83 C6 18 4A E9 5A FF "
            + "FF FF 89 F8 C3 89 E8 C3 9C 60 8B CE 8B D7 E8 0F 00 00 00 89 04 24 61 9D "
            + "A1 C8 34 AD 00 E9 B7 06 93 FF 51 E8 19 00 00 00 39 E8 0F 84 0F 00 00 00 "
            + "8B 0C 24 8B 89 D0 00 00 00 89 81 F4 00 00 00 59 C3 89 D5 81 FA 5B 5C 00 "
            + "00 0F 84 41 00 00 00 81 FA 65 5C 00 00 0F 84 35 00 00 00 81 FA 27 79 00 "
            + "00 0F 84 33 00 00 00 81 FA 64 66 00 00 0F 84 31 00 00 00 81 FA CE 79 00 "
            + "00 0F 84 2F 00 00 00 81 FA DD 79 00 00 0F 84 23 00 00 00 E9 C5 01 00 00 "
            + "BB 00 00 00 00 E9 1E 00 00 00 BB 01 00 00 00 E9 14 00 00 00 BB 02 00 00 "
            + "00 E9 0A 00 00 00 BB 03 00 00 00 E9 00 00 00 00 A1 E0 9C C7 00 85 C0 0F "
            + "84 90 01 00 00 8B 80 D0 2E 00 00 85 C0 0F 84 82 01 00 00 8B 90 C0 00 00 "
            + "00 85 D2 0F 84 74 01 00 00 39 51 30 0F 85 6B 01 00 00 8B 90 C4 00 00 00 "
            + "39 51 34 0F 85 5C 01 00 00 8B 81 D0 00 00 00 85 C0 0F 84 4E 01 00 00 39 "
            + "A8 F4 00 00 00 0F 85 42 01 00 00 8B B8 F8 00 00 00 85 FF 0F 84 34 01 00 "
            + "00 81 FD 5B 5C 00 00 0F 84 25 01 00 00 81 FD 65 5C 00 00 0F 84 19 01 00 "
            + "00 81 FD 64 66 00 00 0F 84 0D 01 00 00 81 FD 27 79 00 00 0F 84 01 01 00 "
            + "00 81 FD CE 79 00 00 0F 84 F5 00 00 00 81 FD DD 79 00 00 0F 84 E9 00 00 "
            + "00 8B 91 D0 0D 00 00 83 FA FF 0F 84 14 00 00 00 83 FA 10 0F 87 D4 00 00 "
            + "00 8D B1 50 0C 00 00 E9 0C 00 00 00 8B 91 54 0C 00 00 8B B1 58 0C 00 00 "
            + "85 F6 0F 84 B5 00 00 00 81 FA 00 01 00 00 0F 87 A9 00 00 00 85 D2 0F 84 "
            + "A1 00 00 00 8B 46 08 83 FB 00 0F 85 16 00 00 00 3D 3C 17 01 00 0F 84 87 "
            + "00 00 00 3D 89 17 01 00 0F 84 7C 00 00 00 83 FB 01 0F 85 16 00 00 00 3D "
            + "3E 17 01 00 0F 84 68 00 00 00 3D 86 17 01 00 0F 84 5D 00 00 00 83 FB 02 "
            + "0F 85 16 00 00 00 3D 43 17 01 00 0F 84 49 00 00 00 3D 87 17 01 00 0F 84 "
            + "3E 00 00 00 83 FB 03 0F 85 2C 00 00 00 3D 3D 17 01 00 0F 84 2A 00 00 00 "
            + "3D 44 17 01 00 0F 84 1F 00 00 00 3D 84 17 01 00 0F 84 14 00 00 00 3D 88 "
            + "17 01 00 0F 84 09 00 00 00 83 C6 18 4A E9 5A FF FF FF 89 F8 C3 89 E8 C3 ");

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length == 2)
            {
                Build(args[0], args[1]);
                return 0;
            }
            if (args.Length != 0)
                throw new ArgumentException("Run without arguments or supply the input and output paths.");

            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Select your original WoW 3.3.5a executable";
                dialog.Filter = "WoW executable (*.exe)|*.exe";
                dialog.FileName = Original;

                if (dialog.ShowDialog() != DialogResult.OK)
                    return 0;
                string output = Path.Combine(Path.GetDirectoryName(dialog.FileName), Prototype);
                MessageBox.Show("This creates one file beside the selected original:" + Environment.NewLine
                    + output + Environment.NewLine + Environment.NewLine
                    + "The new copy contains two code hooks and an added patch section. "
                    + "Your original executable stays available. See CHANGES.md for exact offsets and hashes.",
                    "DBW Visual Fix", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Build(dialog.FileName, output);
                MessageBox.Show("Created " + output + Environment.NewLine + Environment.NewLine
                    + "Launch Wow (DBW Fix).exe from your game folder. "
                    + "To return to normal, launch your original WoW executable.",
                    "DBW Visual Fix", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            return 0;
        }
        catch (Exception error)
        {
            if (args.Length == 0)
                MessageBox.Show(error.Message, "DBW Visual Fix", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
                Console.Error.WriteLine(error.Message);
            return 1;
        }
    }

    private static void Build(string input, string output)
    {
        byte[] original = File.ReadAllBytes(input);
        string expectedOutputHash;
        using (SHA256 sha = SHA256.Create())
        {
            string hash = BitConverter.ToString(sha.ComputeHash(original)).Replace("-", "");
            switch (hash)
            {
                case "70176F278A802761049E6F07CE5921D081E3DDE59769E03037DE80A455A21BAD":
                    expectedOutputHash = "906E2E25E413116D73F365FBD93BE49C0E6D0B44342FF55C661907DA8627F523";
                    break;
                case "AA63A5750D60EF16746C686B3D5E26876D98953EAB08B1C026CD0FAF78E88CB8":
                    expectedOutputHash = "B6D715E35C72BBD26CFDB88BA1A1D1DFF1E2FC1BAC6BF14BAA3762C42DC3FE88";
                    break;
                default:
                    throw new InvalidDataException("This executable is unsupported. Select an original matching a SHA256 in README.md.");
            }
        }

        int pe = (int)Read32(original, 0x3C);
        int optional = pe + 24;
        int count = Read16(original, pe + 6);
        int sections = optional + Read16(original, pe + 20);
        int newSection = sections + 40 * count;
        uint fileAlignment = Read32(original, optional + 36);
        uint sectionAlignment = Read32(original, optional + 32);
        if (Read32(original, optional + 28) != ImageBase || Read32(original, optional + 56) != PatchRva
            || newSection + 40 > Read32(original, optional + 60)
            || Read32(original, optional + 136) != 0)
            throw new InvalidDataException("The executable layout differs from the verified build.");

        uint rawOffset = Align((uint)original.Length, fileAlignment);
        uint rawSize = Align((uint)Payload.Length, fileAlignment);
        byte[] patched = new byte[rawOffset + rawSize];
        Array.Copy(original, patched, original.Length);
        Array.Copy(Payload, 0, patched, rawOffset, Payload.Length);

        InstallJump(patched, sections, 0x7168DF, ImageBase + PatchRva,
            ParseHex("8B 50 1C 6A 00 8B CE FF D2"));
        InstallJump(patched, sections, 0x72D974, ImageBase + PatchRva + 680,
            ParseHex("A1 C8 34 AD 00"));

        Array.Copy(System.Text.Encoding.ASCII.GetBytes(".dbw"), 0, patched, newSection, 4);
        Write32(patched, newSection + 8, (uint)Payload.Length);
        Write32(patched, newSection + 12, PatchRva);
        Write32(patched, newSection + 16, rawSize);
        Write32(patched, newSection + 20, rawOffset);
        Write32(patched, newSection + 36, 0x60000020); // code, execute and read
        Write16(patched, pe + 6, (ushort)(count + 1));
        Write32(patched, optional + 4, Read32(original, optional + 4) + rawSize);
        Write32(patched, optional + 56, Align(PatchRva + (uint)Payload.Length, sectionAlignment));
        Write32(patched, optional + 64, 0); // original checksum no longer applies
        Write32(patched, optional + 128, 0); // original signature no longer applies
        Write32(patched, optional + 132, 0);

        using (SHA256 sha = SHA256.Create())
        {
            string hash = BitConverter.ToString(sha.ComputeHash(patched)).Replace("-", "");
            if (hash != expectedOutputHash)
                throw new InvalidDataException("Output differs from the verified release. No file was written.");
        }
        using (FileStream file = new FileStream(output, FileMode.CreateNew, FileAccess.Write))
            file.Write(patched, 0, patched.Length);
    }

    private static void InstallJump(byte[] bytes, int text, uint address, uint destination, byte[] expected)
    {
        uint rva = address - ImageBase;
        int offset = checked((int)(Read32(bytes, text + 20) + rva - Read32(bytes, text + 12)));
        for (int i = 0; i < expected.Length; i++)
            if (bytes[offset + i] != expected[i])
                throw new InvalidDataException("Unexpected instructions at " + address.ToString("X8"));
        bytes[offset] = 0xE9;
        Write32(bytes, offset + 1, unchecked(destination - address - 5));
        for (int i = 5; i < expected.Length; i++)
            bytes[offset + i] = 0x90;
    }

    private static byte[] ParseHex(string text)
    {
        string[] tokens = text.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
        byte[] result = new byte[tokens.Length];
        for (int i = 0; i < tokens.Length; i++)
            result[i] = Convert.ToByte(tokens[i], 16);
        return result;
    }

    private static uint Align(uint value, uint alignment) { return (value + alignment - 1) & ~(alignment - 1); }
    private static ushort Read16(byte[] bytes, int offset) { return BitConverter.ToUInt16(bytes, offset); }
    private static uint Read32(byte[] bytes, int offset) { return BitConverter.ToUInt32(bytes, offset); }
    private static void Write16(byte[] bytes, int offset, ushort value) { Array.Copy(BitConverter.GetBytes(value), 0, bytes, offset, 2); }
    private static void Write32(byte[] bytes, int offset, uint value) { Array.Copy(BitConverter.GetBytes(value), 0, bytes, offset, 4); }
}


