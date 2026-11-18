# Exact patch changes

## Files affected when running DbwPatcher.exe

The patcher reads the selected original executable and creates one output, normally `Wow (DBW Fix).exe` in the same folder. Existing output files cause an error. The original executable is opened for reading only. The output is first written to a uniquely named `.dbw-<random>.tmp` in the destination folder, flushed to disk and SHA256-verified, then renamed without overwriting. Handled failures remove the temporary file. Abrupt process termination or power loss can leave a temporary file, but the final executable is not published until verification succeeds.

Game archives, DLLs, addons, configuration, saved variables, cache, shortcuts, registry, and server files receive no writes from the patcher. The patcher creates no logs, installs no service, accesses no network, and launches no game process. Normal WoW activity after launching the generated client can update its usual settings, cache, and logs.

## Code changes in the generated copy

The supported originals and their documented LAA variants have matching instructions at both hook sites and matching section layouts. All offsets below apply to both originals. Virtual addresses assume image base `0x00400000`.

| Purpose | File offset | Virtual address | Original bytes | Patched bytes |
| --- | --- | --- | --- | --- |
| In-world display callback | `0x00315CDF` | `0x007168DF` | `8B 50 1C 6A 00 8B CE FF D2` | `E9 1C 67 6E 00 90 90 90 90` |
| Initial model lookup | `0x0032CD74` | `0x0072D974` | `A1 C8 34 AD 00` | `E9 2F F9 6C 00` |

The first hook preserves a loaded native model and reconciles the client display field so gear updates continue. The second selects the native model during initial loading and reconciles the same field before appearance construction.

At runtime the fix writes your local character’s current display field to the native display ID only when one of the six targeted model IDs is recognized and the applicable guards pass. Other clients still receive the server’s model. Other effects sharing those model IDs on your character are also affected.

## Added section and headers

- Section count changes from 6 to 7.
- New `.dbw` section starts at virtual address `0x00DFD000`, with 1,272 bytes of patch code and read/execute permissions.
- Payload is documented in [payload.asm.txt](payload.asm.txt) and embedded as hexadecimal bytes in [src/DbwPatcher.cs](src/DbwPatcher.cs).
- SizeOfCode and SizeOfImage are updated for the new section.
- The PE checksum is cleared.
- The security directory is cleared because the original signature no longer applies. The old certificate bytes remain in the copied file.
- Final size is 7,706,112 bytes, compared with 7,704,216 bytes for the original.

SHA256 hashes for the input, output, patcher, and source files are documented in README.md and SHA256SUMS.txt. A matching hash establishes file identity. Source review is how you assess the behavior of those bytes.

## Patcher hardening

Exact SHA256 allowlisting includes the two documented standard LAA variants. The generated copy retains the input LAA flag. The injected gameplay payload is unchanged. Regression tests cover output verification failures, rename failures, existing-file protection, malformed inputs, and native x86 display helpers.
