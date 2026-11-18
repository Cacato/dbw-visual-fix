using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static class RegressionTests
{
    private static readonly BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;
    private static readonly Type Patcher = typeof(DbwPatcher);
    private static int assertions;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr VirtualAlloc(IntPtr address, UIntPtr length, uint allocation, uint protection);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool VirtualProtect(IntPtr address, UIntPtr length, uint protection, out uint previous);
    [DllImport("kernel32.dll")]
    private static extern bool VirtualFree(IntPtr address, UIntPtr length, uint freeType);
    [DllImport("kernel32.dll")]
    private static extern bool FlushInstructionCache(IntPtr process, IntPtr address, UIntPtr length);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int SelectModel(IntPtr unit, int display);

    private static int Main(string[] args)
    {
        string root = Path.Combine(Path.GetTempPath(), "dbw-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            TestOutput(root);
            TestHooks();
            ExpectFailure(() => Invoke("Build", Path.Combine(root, "junk.exe"), Path.Combine(root, "rejected.exe")), typeof(FileNotFoundException));
            File.WriteAllBytes(Path.Combine(root, "junk.exe"), new byte[] {77, 90});
            ExpectFailure(() => Invoke("Build", Path.Combine(root, "junk.exe"), Path.Combine(root, "rejected.exe")), typeof(InvalidDataException));
            Check(!File.Exists(Path.Combine(root, "rejected.exe")), "Malformed input created output");
            for (int i = 0; i < args.Length; i++) TestOriginal(root, args[i], i);
            Console.WriteLine("Passed " + assertions + " checks. Hook helpers execute as native x86 against synthetic unit data.");
            if (args.Length == 0) Console.WriteLine("Pass supported original executable paths to also test complete builds and 4 GB variants.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        finally
        {
            foreach (string file in Directory.GetFiles(root)) File.Delete(file);
            Directory.Delete(root);
        }
    }

    private static void Invoke(string name, params object[] args)
    {
        try { Patcher.GetMethod(name, PrivateStatic).Invoke(null, args); }
        catch (TargetInvocationException error) { throw error.InnerException; }
    }

    private static string Hash(byte[] bytes)
    {
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
    }

    private static void TestOutput(string root)
    {
        byte[] bytes = {1, 2, 3, 4};
        string output = Path.Combine(root, "completed.exe");
        Invoke("WriteOutput", output, bytes, Hash(bytes));
        Check(File.ReadAllBytes(output).SequenceEqual(bytes), "Wrong output contents");
        ExpectFailure(() => Invoke("WriteOutput", output, new byte[] {9}, Hash(new byte[] {9})), typeof(IOException));
        Check(File.ReadAllBytes(output).SequenceEqual(bytes), "Existing output was overwritten");
        string rejected = Path.Combine(root, "bad-hash.exe");
        ExpectFailure(() => Invoke("WriteOutput", rejected, bytes, new string('0', 64)), typeof(InvalidDataException));
        Check(!File.Exists(rejected), "Failed verification published a final file");
        Check(Directory.GetFiles(root, ".dbw-*.tmp").Length == 0, "Failed verification left a temporary file");
        // Existing directory forces File.Move to fail after the temporary file has been fully written.
        string directory = Path.Combine(root, "occupied");
        Directory.CreateDirectory(directory);
        try { ExpectFailure(() => Invoke("WriteOutput", directory, bytes, Hash(bytes)), typeof(IOException)); }
        finally { Directory.Delete(directory); }
        Check(Directory.GetFiles(root, ".dbw-*.tmp").Length == 0, "Failed publication left a temporary file");
    }

    private static void TestOriginal(string root, string originalPath, int index)
    {
        byte[] original = File.ReadAllBytes(originalPath);
        string output = Path.Combine(root, "built-" + index + ".exe");
        Invoke("Build", originalPath, output);
        string inputHash = Hash(original);
        string expected = inputHash == "70176f278a802761049e6f07ce5921d081e3dde59769e03037de80a455a21bad"
            ? "906e2e25e413116d73f365fbd93be49c0e6d0b44342ff55c661907da8627f523"
            : "b6d715e35c72bbd26cfdb88ba1a1d1dff1e2fc1bac6bf14baa3762c42dc3fe88";
        Check(Hash(File.ReadAllBytes(output)) == expected, "Complete build hash mismatch");
        Check(File.ReadAllBytes(originalPath).SequenceEqual(original), "Original changed");
        int pe = BitConverter.ToInt32(original, 60);
        if ((BitConverter.ToUInt16(original, pe + 22) & 0x20) != 0) return;
        byte[] laa = (byte[])original.Clone();
        laa[pe + 22] |= 0x20;
        for (int variant = 0; variant < 2; variant++)
        {
            if (variant == 1) Array.Clear(laa, pe + 24 + 64, 4);
            string input = Path.Combine(root, "laa-input-" + variant + ".exe");
            string target = Path.Combine(root, "laa-output-" + variant + ".exe");
            File.WriteAllBytes(input, laa);
            Invoke("Build", input, target);
            byte[] result = File.ReadAllBytes(target);
            Check(Hash(result) == "59c086a0ed22cdc51ff0e56504a6bfb4cf385989769bb0649202d4c7e1afa89a", "LAA output mismatch");
            Check((BitConverter.ToUInt16(result, pe + 22) & 0x20) != 0, "LAA flag was lost");
            // A second, unrelated change must still be refused.
            byte[] unrelated = (byte[])laa.Clone(); unrelated[0x400] ^= 1;
            File.WriteAllBytes(input, unrelated);
            ExpectFailure(() => Invoke("Build", input, Path.Combine(root, "unsafe.exe")), typeof(InvalidDataException));
            Check(!File.Exists(Path.Combine(root, "unsafe.exe")), "Unrelated modification was accepted");
        }
    }

    private static void TestHooks()
    {
        Check(IntPtr.Size == 4, "Compile the tests for x86");
        IntPtr memory = VirtualAlloc(IntPtr.Zero, new UIntPtr(0x10000), 0x3000, 4);
        if (memory == IntPtr.Zero) throw new Exception("VirtualAlloc failed");
        try
        {
            IntPtr unit = memory + 0x2000, fields = memory + 0x3000;
            IntPtr connection = memory + 0x4000, manager = memory + 0x7000, cached = memory + 0x8000;
            Marshal.WriteIntPtr(memory + 0x9000, connection);
            Marshal.WriteIntPtr(connection + 0x2ED0, manager);
            Marshal.WriteInt32(manager + 0xC0, 123);
            Marshal.WriteInt32(manager + 0xC4, 456);
            Marshal.WriteInt32(unit + 0x30, 123);
            Marshal.WriteInt32(unit + 0x34, 456);
            Marshal.WriteIntPtr(unit + 0xD0, fields);
            byte[] code = (byte[])((byte[])Patcher.GetField("Payload", PrivateStatic).GetValue(null)).Clone();
            int replacements = 0;
            for (int i = 0; i + 4 <= code.Length; i++)
            {
                if (BitConverter.ToUInt32(code, i) != 0xC79CE0) continue;
                Array.Copy(BitConverter.GetBytes((memory + 0x9000).ToInt32()), 0, code, i, 4);
                replacements++;
            }
            Check(replacements == 2, "Unexpected connection-reference count");
            Marshal.Copy(code, 0, memory, code.Length);
            // Save nonvolatile registers, pass thiscall arguments, call the actual helper, restore, return.
            byte[] wrapper = {0x55,0x53,0x56,0x57,0x8B,0x4C,0x24,0x14,0x8B,0x54,0x24,0x18,
                0xE8,0,0,0,0,0x5F,0x5E,0x5B,0x5D,0xC2,0x08,0x00};
            SelectModel[] helpers = new SelectModel[2];
            int[] entries = {62, 706};
            for (int i = 0; i < helpers.Length; i++)
            {
                int offset = 0x1000 + i * 64;
                Array.Copy(BitConverter.GetBytes(entries[i] - offset - 17), 0, wrapper, 13, 4);
                Marshal.Copy(wrapper, 0, memory + offset, wrapper.Length);
                helpers[i] = (SelectModel)Marshal.GetDelegateForFunctionPointer(memory + offset, typeof(SelectModel));
            }
            uint previous;
            if (!VirtualProtect(memory, new UIntPtr(0x2000), 0x20, out previous)
                || !FlushInstructionCache(new IntPtr(-1), memory, new UIntPtr(0x2000))) throw new Exception("Could not prepare x86 test code");
            foreach (int native in new[] {52, 1478, 10142})
            foreach (int display in new[] {23643, 23653, 26212, 31015, 31182, 31197})
            {
                Marshal.WriteInt32(fields + 0xF8, native);
                Marshal.WriteInt32(fields + 0xF4, display);
                Marshal.WriteInt32(unit + 0xB4, 1);
                Marshal.WriteIntPtr(unit + 0x968, cached);
                Marshal.WriteInt32(cached, native);
                Check(helpers[0](unit, display) == 1, "Gear guard rejected target");
                Marshal.WriteInt32(cached, 999);
                Check(helpers[0](unit, display) == 0, "Guard accepted another loaded form");
                Marshal.WriteInt32(unit + 0xB4, 0);
                Marshal.WriteIntPtr(unit + 0x968, IntPtr.Zero);
                Check(helpers[1](unit, display) == native, "Initial load failed without an existing model");
                Check(Marshal.ReadInt32(fields + 0xF4) == native, "Login display field not reconciled");
                Marshal.WriteInt32(fields + 0xF4, display);
                Marshal.WriteInt32(unit + 0x34, 789);
                Check(helpers[1](unit, display) == display, "Another unit was changed");
                Check(Marshal.ReadInt32(fields + 0xF4) == display, "Another unit's display field changed");
                Marshal.WriteInt32(unit + 0x34, 456);
            }
            Marshal.WriteInt32(fields + 0xF4, 31182);
            Marshal.WriteInt32(fields + 0xF8, 0);
            Check(helpers[1](unit, 31182) == 31182, "Zero native display accepted");
            Check(helpers[1](unit, 118) == 118, "Unrelated display accepted");
        }
        finally { VirtualFree(memory, UIntPtr.Zero, 0x8000); }
    }

    private static void ExpectFailure(Action action, Type expected)
    {
        try { action(); }
        catch (Exception error)
        {
            Check(expected.IsAssignableFrom(error.GetType()), "Unexpected error: " + error);
            return;
        }
        throw new Exception("Expected " + expected.Name);
    }

    private static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception(message);
    }
}
