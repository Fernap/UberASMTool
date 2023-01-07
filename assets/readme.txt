 _    _ _                       _____ __  __   _______          _ 
| |  | | |               /\    / ____|  \/  | |__   __|        | |
| |  | | |__   ___ _ __ /  \  | (___ | \  / |    | | ___   ___ | |
| |  | | '_ \ / _ \ '__/ /\ \  \___ \| |\/| |    | |/ _ \ / _ \| |
| |__| | |_) |  __/ | / ____ \ ____) | |  | |    | | (_) | (_) | |
 \____/|_.__/ \___|_|/_/    \_\_____/|_|  |_|    |_|\___/ \___/|_|
         Version 2.0?                By Vitor Vilela

Thank you for downloading my tool. I hope it helps you though your
SMW hacking journey.

UberASM Tool allows you to easily insert level ASM, overworld ASM,
game mode ASM and much more. It was inspired from GPS, Sprite Tool,
edit1754's levelASM tool, levelASM+NMI version and
33953YoShI's japanese levelASM tool.

At same time UberASM Tool allows easy insertion and distribution of
code, it has a very robust support for complex ASM projects, with
a shared library where you can put your .asm and .bin files without
worrying about freespace and bank overflows.

Features:
 - Level ASM (INIT/MAIN/NMI*/LOAD*)
 - Overworld ASM (INIT/MAIN/NMI*/LOAD*)
 - Game mode ASM (INIT/MAIN/NMI*)
 - Global code ASM (INIT*/MAIN/NMI)
 - Status bar code (MAIN)
 - Shared library with binary support
 - Macro and defines library
 - Automatic multiple banks support
 - Automatic patching and cleaning
 - Native SA-1 support
 - Friendly list with various settings
 - LM Restore System signature
 - Easily editable source code.

* Specific features not present in original uberASM patch.

---------------------------------------------------------------------
-                         Quick Start                               -
---------------------------------------------------------------------

1. Copy the contents of the UberASM Tool .zip file to a dedicated folder somewhere within your project.  Right now, best practice is to keep a separate copy of UberASM Tool for each project that you're working on (although there are plans to add flexibility here in a future version).

2. Place the .asm files for whatever UberASM resources you're using into the level/, gamemode/, or overworld/ folders as appropriate (most resources will be for levels, and resources may have additional instructions as well, so be sure to check for them).

3. Edit the list.txt file in the UberASM Tool folder and change the "rom:" command to point to the location of your ROM file.  It can be given either relative to the UberASM Tool executable (like "../myrom.smc") or as an absolute path.

4. Add the resources you want to use to the list.txt file.  For example, to use a resource named LRonoff.asm in the level/ folder for level 105, you'd add the line "105 LRonoff.asm" under the "level:" section of list.txt.  See below for more detail on the format of the list.txt file.

5. Run UberASMTool.exe (either double click on the icon, or run it from the command line).  See below for command line options and more.

---------------------------------------------------------------------
-                       Running UberASM Tool                        -
---------------------------------------------------------------------

UberASM Tool has the command line syntax:

> UberASMTool [<list file> [<ROM file>]]

If no <list file> is given, UberASM Tool will use the file "list.txt".  If no <ROM file> is given, UberASM Tool will use the ROM file given by the "rom:" command in the list file.  For example:

     rom: MyHack.smc         ; ROM file to use.

All files may be given either as absolute paths or as paths relative to the UberASM Tool executable file.  Alternatively, you can simply double click the executable and it will run as if no command line arguments were given.

---------------------------------------------------------------------
-                         The list file                             -
---------------------------------------------------------------------

The list file (list.txt by default) contains configuration information for UberASM Tool, including resources to use and some other stuff (FIX).  For commands that take file names, you can either give absolute paths, or you can give relative paths, in which case UberASM Tool will look relative the directory of the UberASM Tool executable.

The following commands are available:

 - verbose: <on/off>

   If turned "on", extra information will be displayed as UberASM Tool runs, such as resource/library insert location and overall progress; this can be useful for debugging.  This command is optional, and the default is "off".

 - rom: <ROM file>

   This specifies the ROM file that UberASM Tool will use.  A ROM file may also be given on the command line, which will override the list file command (and the command may be omitted altogether in this case).

 - global: <asm path>
 - statusbar: <asm path>
 - macrolib: <asm path>

   These specify the .asm files for the global code file, the statusbar code file, and the macro library file.  These are all required, but there are defaults in place initially.  It's unlikely that you'll ever change these, but you can if you need to.  See below for more information about how these are used.

 - freeram: <RAM address>

   Specifies 2 bytes of free RAM used to keep track of the previous game mode.  The Usually you don't need to worry about this RAM address; the default value should work normally.  More may be required in the future, but previous versions required 38 (68 on SA-1) bytes, rather than the current amount.


 - "level:"

   This defines that we're now inserting LevelASM code. So any number below this label will be considerated as LevelASM code. Example:

level:
105 yoshi_island_1.asm
106 yoshi_island_2.asm

The input is hexadecimal and valid range is 000-1FF. All files must
be on level folder. You can use it subfolders if you want, for example:

level:
105 world 1/level 1.asm
115 world 3/castle.asm

You can also use the same .asm file for multiple levels, allowing you
save more space.

 - "overworld:"
   This defines that we're now inserting OW code. It has the same
properties as "level:" label, except it applies to OW ASM code, uses
overworld subfolder and valid numbers are: 0 = Main map; 1 = Yoshi's
Island; 2 = Vanilla Dome; 3 = Forest of Illusion; 4 = Valley of
Bowser; 5 = Special World; and 6 = Star World.

 - "gamemode:"
   This defines that we're not inserting Game mode code. It has the
same properties as "level:" label, however it uses gamemode subfolder
and valid range is 00-FF.

";" means comment. They won't be processed by UberASM tool. Useful
for putting comments, notes, etc.


---------------------------------------------------------------------
-                         Getting Started                           -
---------------------------------------------------------------------
Since UberASM Tool relies on Asar and pretty much uses same hijacks,
you can safely apply it on your ROM even if you used uberASM or
levelASM patch previously. Be sure to make a backup of your ROM
before just to you be sure.

Just like a Block or Sprite tool, each level/OW/etc. now have their
own .asm file, making easier to manage each level ASM code. Together
with that, each .asm file (except global and status code) are now
separated from the other files, so things like labels are not shared
anymore with them. Plus, each one now have a separated RATS, so you
don't have to worry at all with bank limitations, since each .asm
file can have their own ROM bank. Due of that, of course, each .asm
file now must end with RTL, instead of RTS.

IMPORTANT: in case of crash while porting, double check your .asm
file in case you missed any RTS, since the code must end with RTL
now.

The INIT and MAIN labels changed too. Since it is a tool, now you
must simply point them as "init:" and "main:". For example, in
your level_init_code.asm, there is this:

levelinit105:
	LDA #$01
	STA $19
	RTS

And in your level_code.asm, there is this:

level105:
	INC $19
	RTS

Merging both codes, changing label and changing RTS to RTL, it will
look like this:

init:
	LDA #$01
	STA $19
	RTL

main:
	INC $19
	RTL

Done! Simply save this file, place on level folder and reference it
on your list file, for example:

level:
105		your file.asm

IMPORTANT: when referencing your file in your list.txt, make sure to
put your file below "level:" line for levels, below "overworld:"
line for OW code and etc.

In case you don't have either init code or main code, you can simply
delete the unused label. For example:

init:
	LDA #$01
	STA $19
	RTL

or

main:
	INC $19
	RTL

Of course at least one label (init, main or nmi) is required to
work.

---------------------------------------------------------------------
-                        Multiple Bank Support                      -
---------------------------------------------------------------------

UberASM Tool was intentionally designed to every single level code,
global code, overworld code, game mode code, libraries, external
resources, etc., to have their own freespace. This means each code
will be on a different bank, allowing you to you put complex codes
without worrying about bank size limitations. That is, for every
library, code, etc., you can have a insert size up to 0x7FF8 bytes of
space to put whatever you want. As a downside, each struture will
generate a new RATS tag, which is 8 bytes big, slight reducing the
overall free space from the ROM. But that should not be a big issue.

Another thing you have to keep in mind that every code must return
with RTL and not RTS. Data bank is set up automatically, *except*
for NMI code, global code and library calls, since they're called
directly from your code.

---------------------------------------------------------------------
-                         NMI Code Support                          -
---------------------------------------------------------------------

To allow more flexible OW and level ASM advanced design, UberASM Tool
allows coders to run a different NMI code per level, overworld, game
mode and/or global code. To save v-blank time, UberASM Tool checks if
some level, ow, gm or global code actually has nmi label present, and
if so it automatically activates NMI feature support depending on the
demand.

---------------------------------------------------------------------
-                   "Load" Level Code Support                       -
---------------------------------------------------------------------

UberASM Tool also features the "load:" label for levelASM and global
code. That label is trigged before "init:", when the level has not
loaded yet.

It allows you to set up Lunar Magic's Conditional Direct Map16
feature and initialize other flags (ExAnimation triggers, etc.). As
a bonus, it allows you to write map16 data to the level table,
$7E/$7F:C800-FFFF (or $40/$41:C800-FFFF for SA-1 ROMs), making it
possible to add your own level loading blocks.

---------------------------------------------------------------------
-                 External Bank Resource Include                    -
---------------------------------------------------------------------

Sometimes when you're working with big chunks of data, like tilemaps
or graphics, you may want to use it in another bank. UberASM Tool
allows you to easily add external binary code using the macro
"prot_file". You can do the same for .asm files, using "prot_source".

%prot_file(<file to include>, <label name>)
%prot_source(<file to include>, <label name>)

The freespace is automatically set and UberASM Tool cleans your files
automatically, so you won't have to worry about freespace cleaning.

See the macro library to learn more how they work.

---------------------------------------------------------------------
-                   Shared Library Code Support                     -
---------------------------------------------------------------------

UberASM folder has a special folder called "library". You can insert
whatever you want, .asm file or whatever other extension. UberASM
Tool will insert or assemble all files inside that folder to your ROM
and will clean automatically too.

Non-ASM files will have its pointer saved to a label named with its
file name. In other words, if you put "tilemap.bin" on library folder,
you can access it in other level/gamemode/OW/etc. with the "tilemap"
label.

ASM files will have all labels exposed to the other ASM files however
prefixed with the filename. For example, if you put "math.asm" on the
library folder and there's a "sqrt" label inside the ASM file, you
will be able to access the function in other level/gamemode/OW/etc.
with the "math_sqrt" label.

With that, you can save lot of space on your ROM by putting generic
codes and data on the library folder and call them from your level,
OW or game mode code. For example, HDMA codes.

However there is two major problems with using the shared library
currently:

The first one is the included file will be inserted on ROM
regardless if it was used or not. So if you insert tons of libraries
and you never use it, you will be simply wasnting ROM space because
UberASM can't know if the label was actually used or not.

The second one is that you can't call other libraries codes from a
library file. For example, if you have a windowing HDMA code and you
need to call a sqrt routine, which is located on the math library, you
can't do that, because UberASM Tool can't guess what labels each file
will generate nor what labels each library .asm file will depend from
each other. So unfortunately the library files are pretty much
isolated from each one.

---------------------------------------------------------------------
-                         Other Information                         -
---------------------------------------------------------------------

You can use the following labels for LevelASM code:

load:
init:
main:
nmi:

If you don't use some of them, UberASM Tool will by default point
them to a "RTL", making your code slight cleaner.

Data bank is set up automatically for load, init and main. nmi is not
set up automatically to save v-blank time and usually you don't need
it anyway.

For OverworldASM and Game Mode code, the following labels are
available:

init:
main:
nmi:

Data bank is set up automatically for init and main labels.

For Global Code, the following labels are available:

load:
init:
main:
nmi:

However they should return with RTS and not RTL. Data bank is not set
up automatically.

For Status Code, the only label available is "main:". Data bank is
not set up automatically.

When UberASM Tool is executed, a .extmod file is automatically
generated. This file is used by Lunar Magic to know what external
program modified the ROM and is registered on LM Restored System.

UberASM Tool does support unheadered ROMs.

---------------------------------------------------------------------
-                             Credits                               -
---------------------------------------------------------------------

I'd like to thank:
 - edit1754 for the original LevelASM Tool idea;
 - p4plus2 for the original uberASM patch;
 - Alcaro/byuu/Raidenthequick for Asar; and
 - 33953YoShI/Mirann/Wakana for testing.
 - 33953YoShI again for giving me the LOAD label base hijack and idea.

---------------------------------------------------------------------
-                             Contact                               -
---------------------------------------------------------------------

You can contact me though the following links:

* My Github profile: https://github.com/VitorVilela7
* My Twitter profile: https://twitter.com/HackerVilela
* My personal blog: https://vilela.sneslab.net/

