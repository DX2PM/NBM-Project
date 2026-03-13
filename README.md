# NBM-Project 🚀
A C#-based CPU Emulator ecosystem featuring a custom disk format and its own Assembly language!

This is an educational project designed to explore how CPUs, memory, and storage interact at a low level. The entire toolchain, including the installer, is written in C#.

You can download the ready-to-use installer from the **Releases** section, or build the source code yourself to create your own NBM fork!

---

## 🛠 NbAsM - The Assembler
NbAsM is the dedicated assembler for creating NBM boot codes.


**Usage:**
- `nbasm -i <input.asm> -o <output.bin>` : Compile source code to a binary boot-code file.
- `nbasm -i <input.asm> -tb <disk.xvd>` : Compile and inject code directly into an XVD drive's boot section.
- `nbasm -h` / `--help` : View the instruction set and usage guide.
- `nbasm --version` : Display the current version.

---

## 💾 NBM (New Boot Mode) - Disk Manager & Emulator
NBM is the core engine that manages virtual drives (XVD) and executes the code.


**Usage:**
- `nbm -n <file.xvd>` : Create a new Virtual Drive with a standard header and 1024 bytes of empty boot space.
- `nbm -l <file.xvd>` : Inspect the header, flags, and configuration of an existing disk.
- `nbm -l !boot_code <src.bin> <dest.xvd>` : Inject raw binary bytecode into a virtual disk.
- `nbm <file.xvd>` : Start the emulator and boot from the specified disk.
- `nbm -h` : Show help information.

---

## 🖥 NbmUI - Graphical Disk Editor
`nui` (NBMUI) is the Windows Forms version of the disk manager. 
- Create or load XVD disks visually.
- Direct hex editing of the boot code section.
- **Note:** This tool is currently in *Beta*. If you encounter lag or bugs, please report them in the Issues tab!

---

## 🎓 Learning Goals
This project implements:
1. **CPU Emulation**: 16 registers, status flags (ZF, SF), and a custom instruction set.
2. **Memory Management**: 64KB of virtual RAM and stack operations.
3. **Storage**: A custom binary disk format with metadata headers.
