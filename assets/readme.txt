 _    _ _                       _____ __  __   _______          _
| |  | | |               /\    / ____|  \/  | |__   __|        | |
| |  | | |__   ___ _ __ /  \  | (___ | \  / |    | | ___   ___ | |
| |  | | '_ \ / _ \ '__/ /\ \  \___ \| |\/| |    | |/ _ \ / _ \| |
| |__| | |_) |  __/ | / ____ \ ____) | |  | |    | | (_) | (_) | |
 \____/|_.__/ \___|_|/_/    \_\_____/|_|  |_|    |_|\___/ \___/|_|
         Version 2.0                By Vitor Vilela & Fernap

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

---------------------------------------------------------------------
-                         Overview                                  -
---------------------------------------------------------------------

* Specific features not present in original uberASM patch.
- relationship to UAT
- GM 14 vs level *

Intro: What is UAT? etc
history, possible issues with old resources
relegate that stuff to troubleshooting below

Table of Contents:

Part I: General Use
  - Quick Start
  - Running UberASM Tool
  - The list file
  - The library
  - Troubleshooting

Part II: Technical Information
  - Resources
  - Labels
  - The DBR
  - Extra bytes
  - NMI
  - SA1
  - The library
  - Other code files
  - Dos and Don'ts

=====================================================================
=                                                                   =
=                   Part I: General Use                             =
=                                                                   =
=====================================================================

---------------------------------------------------------------------
-                         Quick Start                               -
---------------------------------------------------------------------

1. Copy the contents of the UberASM Tool .zip file to a dedicated folder
somewhere within your project.  Right now, best practice is to keep a
separate copy of UberASM Tool for each project that you're working on
(although there are plans to add flexibility here in a future version).

2. Place the .asm files for whatever UberASM resources you're using into
the level/, gamemode/, or overworld/ folders as appropriate (most resources
will be for levels, and resources may have additional instructions as well,
so be sure to check for them).

3. Edit the list.txt file in the UberASM Tool folder and change the "rom:"
command to point to the location of your ROM file.  It can be given either
relative to the UberASM Tool executable (like "../myrom.smc") or as an
absolute path.

4. Add the resources you want to use to the list.txt file.  For example, to
use a resource named LRonoff.asm in the level/ folder for level 105, you'd
add the line "105 LRonoff.asm" under the "level:" section of list.txt.  See
below for more detail on the format of the list.txt file.

5. Run UberASMTool.exe (either double click on the icon, or run it from the
command line).  See below for command line options and more.

---------------------------------------------------------------------
-                       Running UberASM Tool                        -
---------------------------------------------------------------------

UberASM Tool has the command line syntax:

> UberASMTool [<list file> [<ROM file>]]

If no <list file> is given, UberASM Tool will use the file "list.txt".  If
no <ROM file> is given, UberASM Tool will use the ROM file given by the
"rom:" command in the list file.  For example:

     rom: MyHack.smc         ; ROM file to use.

All files may be given either as absolute paths or as paths relative to the
UberASM Tool executable file.  Alternatively, you can simply double click
the executable and it will run as if no command line arguments were given.

---------------------------------------------------------------------
-                         The list file                             -
---------------------------------------------------------------------

The list file (list.txt by default) contains configuration information for
UberASM Tool, including resources to use and some other stuff (FIX).  For
commands that take file names, you can either give absolute paths, or you
can give relative paths, in which case UberASM Tool will look relative the
directory of the UberASM Tool executable.

You may add comments to the list file.  A ";" or a "#" on a line is
considered to be a comment and the rest of the line is ignored.

The following commands are available:

 - verbose: <on/off/quiet/debug>

   Sets the verbosity level for UAT.  This command is optional and defaults
   to "off" if not specified.

   quiet: Only error messages are displayed
   off:   Basic information about progress is also displayed
   on:    Extra information is displayed, such as individual
   library/resource file progress, and a more detailed breakdown of total
   insert size
   debug: Even more detailed information is displayed for debugging
   purposes.  Also prevents UAT from deleting temporary files in asm/work/

 - rom: <ROM file>

   This specifies the ROM file that UberASM Tool will use.  A ROM file may
   also be given on the command line, which will override the list file
   command (and the command may be omitted altogether in this case).

 - global:    <asm file>
 - statusbar: <asm file>
 - macrolib:  <asm file>

   These specify the .asm files for the global code file, the statusbar
   code file, and the macro library file.  These are all required, but
   there are defaults in place initially.  It's unlikely that you'll ever
   change these, but you can if you need to.  See below for more
   information about how these are used.

 - freeram: <RAM address>

   Specifies 2 bytes of free RAM used to keep track of the previous game
   mode.  It must be given as as a full address in the $7E0000-7FFFFF range
   ($400000-41FFFF is okay too if using SA-1).  This command is required,
   but the default value in place initially will likely be fine.  More may
   be required in the future, but previous versions required 38 (68 on
   SA-1) bytes, rather than the current amount.

 - level:
 - overworld:
 - gamemode:

   These commands indicate whether subseqent resources are for a level, an
   overworld, or a gamemode, respectively.  Resources must occur somewhere
   after one of these, but you can switch between them freely.

Finally, anything else is interpreted as assigning resources to
levels/gamemodes/overworlds, which has the form:

<number|*> <asm file> [: <extra byte 1> <extra byte 2> ... <extra byte n>]
[, ...]

<number> is the level number (0-1FF), game mode number (0-FF), or overworld
number (0-6, 0 = Main map; 1 = Yoshi's Island; 2 = Vanilla Dome; 3 = Forest
of Illusion; 4 = Valley of Bowser; 5 = Special World; and 6 = Star World).
Numbers here must be in hexadecimal.  You can also specify "*" instead of a
number, which is a special value that indicates that this resource is to
run on all levels (or game mdoes, or overworlds).

<asm file> is the name of the resource file to use.  For levels, UAT looks
relative to the level/ folder, for game modes, the gamemode/ folder, and
for overworlds, the overworld/ folder.  You may give paths relative to
these, or you may also give an absolute path.

If a resource uses extra bytes (see below for more information), you must
specify them after the resource file name with a ":" followed by the actual
bytes to use.  For more than one byte, separate them by spaces.  Bytes are
given in hexadecimal by default (or explicitly by prefixing with "$" or
"0x"), but you can also give them in decimal by prefixing with an "@", or
in binary by prefixing with a "%".  You may also specify negative numbers
here.  So for example, "-10", "F0", "$f0", "@-16", "@240", and "%11110000"
all refer to the same value.

You may also assign more than one resource to each level/game
mode/overworld, including "*".  Simply add them in order, separated by
commas.  You may split resources over separate lines if you wish, but the
comma must go on the same line as preceding resource.  Note that if
multiple resources are given for a single level (or game mode or
overworld), UAT executes them in the order they are given, and resources
under "*" are called first.  This usually doesn't matter, but it can
sometimes be something to keep in mind.

An example (just the resources are given; the other commands are omitted
for clarity:

---------------------------------------------------------
| ; Example list file...this line is a comment.         |
| # so is this                                          |
|                                                       |
| level:                                                |
|    105 incio.asm                                      |
|    106 incio.asm,                 ; comment here too  |
|        waves.asm, gradient.asm                        |
|                                                       |
| overworld:                                            |
| * music.asm                                           |
|                                                       |
| level:                                                |
| 107 special.asm:@100, ../overworld/music.asm          |
---------------------------------------------------------

- "level:" says we're now giving resources for levels
- assigns the resource file incio.asm (which is in the level/ folder) to
level 105
- assigns all of incio.asm, waves.asm, gradient.asm to level 106
- switches to specifying resources for overworlds
- assigns the resource music.asm (in the overworld/ folder) to all
overworlds
- switches back to specifying resources for levels
- assigns two resources to level 107: special.asm (which takes one extra
byte) with an extra byte of 100 (decimal), and also music.asm from the
overworld/ folder.



---------------------------------------------------------------------
-                         The Library                               -
---------------------------------------------------------------------

TODO (very brief, just mention that resources may come with library files)
(more detail below)

---------------------------------------------------------------------
-                         Troubleshooting                           -
---------------------------------------------------------------------

TODO (or common issues maybe)
- adapting old code:
   - what to do with global code (global load -> level *; global main ->
   gamemode *; global nmi -> gamemode *)
   - old levelASM stuff
- incompatibilities w/ lx5 nsmb star coin patch & hb's slide kill thing
- note about running an old version of UAT on a rom with a newer version

=====================================================================
=                                                                   =
=               Part II: Technical Information                      =
=                                                                   =
=====================================================================

The following sections describe the technical aspects of UAT in more
detail.  This is mainly intended for programmers and resource creators.  If
you're only hacking, you shouldn't need anything here, although there may
still be some useful information.

---------------------------------------------------------------------
-                         Resources                                 -
---------------------------------------------------------------------

Resources are the main entities that UberASM Tool runs (bleh).  They can be
run for levels, for particular game modes, or for particular overworld
maps.  However, there's no fundamental difference between the types, and a
level resource can also be used as an overworld resource, for example.  To
make a resource for UberASM Tool, you simply need an .asm file that
contains one or more special labels that UAT calls.  These labels are
"main:", "init:", "nmi:", "load:" (for levels only), and "end:" (new to
v2.0).  See below for more specific information on each label.

As of v2.0, resources can also specify that they take extra bytes, similar
to extra bytes for custom sprites.  For example, suppose you have an
UberASM resource for a wind effect that pushes on Mario.  Previously, such
a resource might have needed to use a define to set the wind speed.  So if
someone wanted to use the same effect in a different levels with different
wind speeds, they'd have to make separate copies of the resource with a
different define value for each one.  This wastes space and makes
organization difficult for the hack creator.  Instead, now you can simply
have your resource take an extra byte that gives the wind speed, and the
same resource can be used in multiple levels with different wind speed
values.  See the section on extra bytes below for more information on how
to write resources that use this feature, and see above for how users can
specify extra bytes for resources in the list file.

---------------------------------------------------------------------
-                         Labels                                    -
---------------------------------------------------------------------

The labels "main:", "init:", "end:", and "nmi:" are available for all
resource types, and level resources additionally have the "load:" label.

For levels, the "load:" label is called once, early in the level loading
process.  The primary purpose of this is to set up Lunar Magic's
conditional direct map16 flags.  See the Lunar Magic documentation for more
information about this.  The "init:" label is also called once, after the
level is done loading and before fade-in.  The "main:" label is called once
at the beginning of each frame during normal level operation (in game mode
$14), and "end:" is likewise called at the end of each frame (but before
the OAM table information at $0420 is copied to $0400).

Overworld resource labels are similar (but for game mode $0E)

Game mode code's "init:" and "main:" labels will run at the start of the
main game loop.  On the first frame the mode is active, "init:" is run, and
on all subsequent frames "main:" is run.  The "end:" label is run after the
entire rest of the game loop, including the $0420 -> $0400 conversion
routine.

For all resource types, the "nmi:" label runs early during SMW's NMI
routine.  Level resources have their NMI label called during game modes $13
and $14, and overworld resources during game modes $0D and $0E.  In both
cases, actual game mode code under "nmi:" will run first.  See the "NMI"
section for more information.

---------------------------------------------------------------------
-                         The DBR                                   -
---------------------------------------------------------------------

By default, UAT sets the DBR to the appropriate bank when calling resource
code for labels other than nmi:.  This behavior is often not needed, and
can be turned off for a resource if its .asm file contains the exact line
(starting at column 1 even)

;>dbr off

somewhere in the source file.  Note that if you do need to set it manually,
you don't need to worry about restoring it (unless set to a bank without
RAM mirrors); UAT will do so after all resources have been called.

---------------------------------------------------------------------
-                         Extra bytes                               -
---------------------------------------------------------------------

To tell UberASM Tool that a resource takes extra bytes, it must contain a
line of the exact form (starting at column 1 even)

;>bytes <number>

somewhere in the resource's .asm file.  Only the first such line is read,
and <number> must be a decimal number in the range 0-255 that says how many
extra bytes this resource uses.

Generally UAT passes a 16-bit pointer to the extra bytes for the current
level/gamemode/overworld in $00-01.  So for example, to load the third
extra byte, you can use the code:

----------------------------------------------------------------
| main:                                                        |
|     LDY #$02              ; load the offset into Y           |
|                           ; first byte at offset $00, etc.   |
|     LDA ($00),y           ; load the accumulator with the    |
|                           ; extra byte whose offset is in Y  |
|     ; do something                                           |
|     ; ...                                                    |
|     RTL                                                      |
----------------------------------------------------------------

Note that this relies on the DBR being set prior to running this code.  UAT
sets it for you by default, but that behavior can be disabled (see the DBR
section for more info), so you'd either need to manually set it, or just
not disable it in the first place.

However, there's one case where it's different -- namely in the nmi: label
on SA-1 roms.  Here, the pointer is placed on the stack, 4 bytes from the
top.  So for example again, to access the third byte, you can use the code:

----------------------------------------------------------------
| nmi:                                                         |
|     PHK : PLB             ; the DBR must be set to the       |
|                           ; current bank                     |
|     LDY #$02              ; load the offset into Y           |
|                           ; first byte at offset $00, etc.   |
|     LDA ($00),y           ; load the accumulator with the    |
|                           ; extra byte whose offset is in Y  |
|     ; do something                                           |
|     ; ...                                                    |
|     RTL                                                      |
----------------------------------------------------------------

Note that you must be careful here that if anything is pushed onto the
stack before reading the extra byte data (which can happen if this code is
in a subroutine that has been JSRed to, for example), you must take this
into account when loading extra byte data.  To write hybrid lorom/sa-1 nmi:
code as above, you would have something like:

---------------------------------------
| nmi:                                |
|     PHK : PLB                       |
|     LDY #$02                        |
|     if !sa1                         |
|         LDA ($04,S),y               |
|     else                            |
|         LDA ($00),y                 |
|     endif                           |
|     ; do something                  |
|     ; ...                           |
|    RTL                              |
---------------------------------------

Note that if you write a resource that uses extra bytes, but someone
attempts to insert it with an older (1.x) version of UberASM Tool, it will
still appear to insert correctly, but it won't

---------------------------------------------------------------------
-                         NMI                                       -
---------------------------------------------------------------------

There are some special considerations that should be taken into account for
UberASM code that runs under the nmi: label.  If a resource uses extra
bytes, the way these are accessed here is different on SA-1; see the "Extra
Bytes" section for more detail.

If the game's NMI routine (which includes UberASM Tool's calling of
resources' nmi: labels) takes too long, there can be noticeable graphical
glitches, generally black bars at the top of the screen.  To help avoid
this, NMI code should be optimized for speed.  To this end, UAT does not
set the DBR for resources when calling the nmi: label.  You're free to do
so if you need, and if so, it does not need to be restored before returning
(as long as it's set to a bank with RAM mirrors).  See "The DBR" section
for more detail.

Finally, the main game code may not have finished running by the time that
NMI code is run (i.e., the game is lagging, or experiencing slowdown).  If
this is the case, the value in memory location $10 will be nonzero, and you
may not want to run your NMI code at all.  If you do, special care needs to
be taken on SA-1 (including hybrid resources that might run on either)
since the SA-1 CPU does not get halted during NMI, but continues to run
alongside the regular SNES CPU.  In this case, normal scratch memory
($00-0F) is not safe to use.  Instead, you can use absolute addressing,
$0000-000F, which is scratch WRAM that the SA-1 CPU has no access to.  Any
scratch memory that you use during lag must be preserved, with the
exception of $00-01 ($0000-0001 on SA-1); UberASM Tool uses this itself and
restores it after all resources have been called.

---------------------------------------------------------------------
-                         The library                               -
---------------------------------------------------------------------

UberASM Tool supports a system for shared library code that different
resources can access.  Files for the shared library should be placed in the
library/ directory.  You can have source (.asm) files, or binary (anything
else) files.

Binary files simply get included (via an incbin) verbatim, with a single
label exposed with name the same as the file's name without the extension.
For example, if the file PlayerGfx.bin is in library/, then all resources
will have the label "PlayerGfx:" available at the start of the file's
contents.  Each binary file will have its own freedata area in the ROM.

Likewise, source files get included into their own freecode region.  Any
labels in the file will be available to resources with the file name
prefixed.  So for example, if the file library/math.asm contains the labels
sqrt: and atan2:, then you can call these as routines with "JSL math_sqrt"
and "JSL math_atan2", respectively.  Be aware that the DBR is not set
automatically.

Since the filenames are used as label prefixes, library file names must be
valid Asar labels.  They must start with a letter and contain only letters,
numbers, and underscores.  However, they can also contain spaces, which
UberASM Tool will convert to underscores.  Furthermore, you may place
library files into subdirectories.  Label prefixes for files in
subdirectories will have slashes converted to underscores.  For example,
the file "library/stuff/My routine.asm" will have its labels prefixed with
stuff_My_routine_.

The main disadvantages of the library system are that library files are
included in the ROM whether or not they're actually used.  So you may wind
up with wasted ROM space.  Also, library files cannot call routines in (or
more generally see labels of) other library files.  There are plans to
include a shared routine system in a future version, similar to that of
Pixi and GPS that should offer more flexibility.

---------------------------------------------------------------------
-                         Other code files                          -
---------------------------------------------------------------------

The macro library file (given by macrolib: in the list file) contains
common defines and macros you may want to use.  You can add defines and
macros here for yourself if you wish.  Some pre-existing macros of note:

- %require_uber_ver(major, minor) -- Fail if the current version of UberASM
Tool is less than major.minor.  This will also cause a resource to fail on
1.x versions as well since this macro is new to 2.0.  This is particularly
useful for files with extra bytes since they'll appear to insert with older
versions, but not work correctly due to the extra bytes not being passed.

- %invoke_sa1(label) -- useful if you want to run some code on the SA-1
CPU; see the "SA-1" section for more detail

- %prot_file(file, label) / %prot_source(file, label) -- these include
binary and source files respectively at the given label, but puts them in
their own freedata/freecode blocks, especially useful for large binary
files

The global code file (given by global: in the list file) has its "init:"
label called by UberASM Tool fairly early in SMW's overall initailization.
Code here should end with RTS instead of RTL.  Note that older versions of
UberASM Tool also had labels for "main:", "nmi:", and "load:", but these
are no longer supported.  See above for how to use code intended for these
labels.  Note, though, that there's one small difference here.  Previously,
global "main:" code would run before the global frame counter (at $13) was
incremented, while code converted to run for UberASM Tool 2.0+ runs after
the frame counter is incremented.  This is unlikely to matter, but it's
worth noting.

The status bar code file (given by statusbar: in the list file) has only
one label used, "main:", which is run during levels, a little later than
normal main: code, right before the status bar is updated.

---------------------------------------------------------------------
-                         SA-1                                      -
---------------------------------------------------------------------

If using UberASM Tool on an SA-1 ROM, note that all resources still run on
the SNES CPU side by default.  If you want to take advantage of the speed
boost that the SA-1 CPU offers, the macro library file includes the
%invoke_sa1(label) macro to run the routine at "label:" on the SA-1 CPU.
The called routine should end in a JSL, and the DBR is not set
automatically, so you will have to do so if needed.  To use this in a
hybrid resource, then, some example code might look like:

--------------------------------------------------------------
| ; ... main resource code                                   |
| if !sa1                                                    |
|     %invoke_sa1(MyRoutine)                                 |
| else                                                       |
|     JSL MyRoutine                                          |
| endif                                                      |
| ; ... main code continues                                  |
|                                                            |
| ...                                                        |
|                                                            |
| MyRoutine:                                                 |
|     if !sa1                                                |
|         PHB : PHK : PLB    ; save/set the DBR (if needed)  |
|     endif                                                  |
|                                                            |
|     ; ... routine body                                     |
|                                                            |
|     if !sa1                                                |
|         PLB     ; restore the DBR (if it was set above)    |
|     endif                                                  |
|                                                            |
|     RTL                                                    |
-------------------------------------------------------------|

There are some extra things to consider when running NMI code on an SA-1
ROM.  See the "NMI" section for more detail, and the "Extra Bytes" section
if making a resource that needs to access extra bytes during NMI.

---------------------------------------------------------------------
-                         Dos and Don'ts                            -
---------------------------------------------------------------------

- (put at the end)
- UAT turns on global namespaces for resources and libraries; don't turn
them off
- If you do set a namespace in your code somewhere, make sure to exit it as
well
- Don't add your own hijacks (is technically possible, but should be
avoided)
- Do %require_uber_ver(2,0) if your resource uses extra bytes

--

other stuff that needs to be mentioned
- %prot macros (including that filename is relative to parent of macrolib
dir)
- resources/libraries get their own freecode areas, but global init and
statusbar don't
- statusbar & global code
- custom game modes
- installed version at 01CD1E

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

