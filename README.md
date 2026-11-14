# Deathbringer’s Will Visual Fix

Keep your character’s normal appearance when Deathbringer’s Will procs in World of Warcraft 3.3.5a, build 12340.

I tested the fix with female Orc and female Troll characters. Appearance and animations stayed normal while the trinket buffs worked.

## How it works

During play, the patch preserves your normal rendered body, hair, and armor when a targeted DBW model arrives for your character. It keeps the local display field consistent with that model so equipment updates work. A second hook handles initial model loading when you log in with the buff already active.

The stat buff remains active. Other players continue to see the transformation on their clients.

The patch targets display IDs 23643, 23653, 26212, 31015, 31182, and 31197. Other effects using those exact model IDs on your character can also have their transformation suppressed.

## Race testing

✅ Confirmed by my testing · ⬜ Untested

| Race | Female | Male |
| --- | --- | --- |
| Human | ⬜ Untested | ⬜ Untested |
| Dwarf | ⬜ Untested | ⬜ Untested |
| Night Elf | ⬜ Untested | ⬜ Untested |
| Gnome | ⬜ Untested | ⬜ Untested |
| Draenei | ⬜ Untested | ⬜ Untested |
| Orc | ✅ Confirmed | ⬜ Untested |
| Undead | ⬜ Untested | ⬜ Untested |
| Tauren | ⬜ Untested | ⬜ Untested |
| Troll | ✅ Confirmed | ⬜ Untested |
| Blood Elf | ⬜ Untested | ⬜ Untested |

## Test coverage

I reported successful Taunka suppression on V2 and successful Vrykul and Iron Dwarf suppression on V3. The current release preserves the normal rendered model and sets the local display field to the native display ID when suppression succeeds. This allows equipment appearance updates to pass the client’s model check. I confirmed that equipment updates during the buff while normal appearance and animations remain intact. Structural checks and isolated guard tests also passed.

The following situations await testing.

- [ ] Every proc on every race and gender
- [ ] Druid forms and overlapping transformations
- [ ] Other customized 3.3.5a clients

## Create your fixed executable

Download and extract the [Windows patcher package](https://github.com/Cacato/dbw-visual-fix/raw/refs/heads/main/downloads/dbw-visual-fix.zip). The patcher requires .NET Framework 4.8 or a compatible newer .NET Framework installation. If needed, get it from [Microsoft’s official .NET Framework 4.8 download page](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48) and select **Download .NET Framework 4.8 Runtime**. Its C# source, build script, exact patch details, and instruction listing are included for inspection.

1. Close WoW and run `DbwPatcher.exe`.
2. Select your original `Wow.exe` or supported `Wow (Allow Interface Edits x64).exe` in your game folder.
3. Read the output location shown by the patcher.
4. The patcher creates `Wow (DBW Fix).exe` beside the original.
5. Launch the new copy from your game folder, beside `DivxDecoder.dll`.

The patcher accepts only the matching original hash listed below, checks the expected instructions, and verifies the generated release hash before writing. If the output already exists, move or rename that copy first.

## Exactly which files change?

| File or location | Action |
| --- | --- |
| Original executable selected by you | Read to build the fixed copy |
| `Wow (DBW Fix).exe` beside that original | Created as the one output file |

The modifications are contained in the generated executable. Two instruction sites redirect to the added patch code. The executable headers are updated to describe that code, and the original signature directory is cleared because the original signature does not cover the modified copy.

See [CHANGES.md](CHANGES.md) for exact offsets, bytes, and the complete file-change scope. To switch back, close the game and launch your original executable.

## Verify the files

The patcher supports the two original executables identified below, regardless of their filenames.

| File | SHA256 |
| --- | --- |
| Modified original executable | `70176f278a802761049e6f07ce5921d081e3dde59769e03037de80a455a21bad` |
| Fixed executable from modified original | `906e2e25e413116d73f365fbd93be49c0e6d0b44342ff55c661907da8627f523` |
| Clean-client original Wow.exe | `aa63a5750d60ef16746c686b3d5e26876d98953eab08b1c026cd0faf78e88cb8` |
| Fixed executable from clean original | `b6d715e35c72bbd26cfdb88ba1a1d1dff1e2fc1bac6bf14baa3762c42dc3fe88` |

Check the generated file in PowerShell.

```powershell
Get-FileHash -Algorithm SHA256 -LiteralPath "C:\Games\World of Warcraft\Wow (DBW Fix).exe"
```

A matching hash confirms that your output matches this build. Review the source to assess what the patch does. Source file hashes are listed in [SHA256SUMS.txt](SHA256SUMS.txt).

## Inspect or build the patcher

| File | Purpose |
| --- | --- |
| [src/DbwPatcher.cs](src/DbwPatcher.cs) | Complete patcher source, including readable hexadecimal patch bytes |
| [payload.asm.txt](payload.asm.txt) | Instruction listing of all 1,272 bytes of added x86 code |
| [CHANGES.md](CHANGES.md) | File-change scope, executable offsets, and before/after bytes |
| [build.ps1](build.ps1) | Compiles the patcher with the .NET Framework C# compiler |

To build from source, open PowerShell in the repository folder and run the script. The script requires the .NET Framework C# compiler and Windows Forms reference assemblies. It uses the local compiler and the included source.

```powershell
.\build.ps1
```

This creates `DbwPatcher.exe` in the repository folder. The patcher can also be called with an original path and a new output path for verification.

```powershell
.\DbwPatcher.exe "C:\Games\World of Warcraft\Wow (Allow Interface Edits x64).exe" "C:\Games\World of Warcraft\Wow (DBW Fix).exe"
```

## Report a test

Send your test results directly to **@Matcha on Discord**. Include your race, gender, realm, client build, fix version, and the proc names you tested. The [test report form](TESTING.md) can also be used for a public GitHub issue in this repository.

Check your face, hair, armor, weapons, and animations before, during, and after the proc. Verify the stat increase in the character panel. Record any crashes or appearance glitches and return to the original executable if a problem occurs.

The current release also handles the initial model load. I confirmed that logging back in while a DBW buff is active preserves normal appearance, equipment changes update correctly, and appearance and animations remain normal when the buff expires.

## Clean-client verification

I tested the fix in a separate clean 3.3.5a client without addons copied from the modified installation. Iron Dwarf, Vrykul, and Taunka procs preserved normal appearance. Equipment changes worked during a buff and after logging out and back in with the buff active. Taunka also remained suppressed after relogging. Appearance after buff expiration in the clean client remains to be explicitly confirmed.

## Project status

The patcher supports the tested standard 3.3.5a executable and one tested modified executable. Other modified executables require compatibility verification. Clean-client testing has confirmed the main proc, equipment-change, and relog paths. The Windows patcher reproduced the tested release byte for byte. Race and gender coverage will be updated as testing continues.
