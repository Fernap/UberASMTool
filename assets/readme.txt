 _    _ _                       _____ __  __   _______          _
| |  | | |               /\    / ____|  \/  | |__   __|        | |
| |  | | |__   ___ _ __ /  \  | (___ | \  / |    | | ___   ___ | |
| |  | | '_ \ / _ \ '__/ /\ \  \___ \| |\/| |    | |/ _ \ / _ \| |
| |__| | |_) |  __/ | / ____ \ ____) | |  | |    | | (_) | (_) | |
 \____/|_.__/ \___|_|/_/    \_\_____/|_|  |_|    |_|\___/ \___/|_|
         Version 2.0                By Vitor Vilela & Fernap

Thank you for downloading UberASM Tool.  We hope it helps you on your
SMW hacking journey.

---------------------------------------------------------------------
-                         Overview/FAQ                              -
---------------------------------------------------------------------

Q. What is UberASM Tool?

A. UberASM Tool is a program that lets you manage various UberASM
resources, applying them to different levels (usually levels, but sometimes
overworld maps, or game modes) as you wish.  UberASM Tool applies several
hijacks in key places in an SMW ROM which allow it to run code for UberASM
resources at specific times during the game, such as once per frame during
levels.  This approach offers a good compromise between flexibility and
ease of managing resources; resources can be added, removed, or applied to
different levels easily.  You can generally do more things with a dedicated
patch than with an UberASM resource, but removing a patch or having a patch
be active only for certain level is generally more difficult.  There's also
much less risk of two UberASM resources conflicting with each other than
there is for two patches.



Q. What's new in 2.0, and what should I be aware of when upgrading to 2.0
from 1.x?

A. Version 2.0 of UberASM Tool is a signficant update to the program.  If
you try to run it out of the box with your existing list.txt file, you'll
notice an error during list processing that the sprite: command is no
longer supported.  Simply change "sprite:" to "freeram:" in your list.txt,
and remove the "sprite-sa1:" line.

Other new features include:
- Multiple resources per level/overworld/game mode.  No more library
workaround for combining resources.  See "The List File" section for how to
do this.
- You can also specify resources that run on all levels (or overworld maps
or game modes).  This mostly does away with the need to use game mode 14
for this purpose.  See "The List File" section for more information.
- The ability for resources to use extra bytes, similar to the way they're
used for sprites.  This lets resources take parameters in the form of extra
bytes supplied in list.txt.  This lets you use the same resource in
different configurations for different levels without having to make a
copies, as you would have previously.  See "The List File" section for more
information.
- (For programmers) A new "end:" label to let resources run code at the end
of frames.  This gives resources the opportunity to achieve certain effects
that weren't possible with UberASM before.  See the "Labels" section below
for more information.
- (For programmers) Support for shared routines, similar to (but slightly
different than) those of Pixi and GPS.  See the "Shared Routines" section
for more information.
- A bunch more little tweaks and improvements.  See the changelog for a
full list.



Q. Help!  ___ doesn't work anymore!

A. That's not a question!  See the incompatibilities.txt file for known
incompatibilities and what to do about them.



Q. I've downloaded a patch/block/sprite that has some uberasm, but the
instructions don't really make sense with the current version of UberASM
Tool.  What should I do?

A. You've probably got something meant for the global code file or even the
original UberASM patch.  See the "Common Issues" section below for steps
to make them work.

------------------------------------------------------------------------------

Table of Contents:

Part I: General Use
  - Quick Start
  - Running UberASM Tool
  - The List File
  - The Library
  - Common Issues

Part II: Technical Information
  - Resources
  - Labels
  - The DBR
  - Extra Bytes
  - NMI
  - SA-1
  - The Library
  - Shared Routines
  - Other Code Files
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

3.5. If you're upgrading from 1.x and using an existing list.txt file,
remove the line with "sprite-sa1:" (if it's there), and on the line with
"sprite: ...", change "sprite" to "freeram".

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
-                         The List File                             -
---------------------------------------------------------------------

The list file (list.txt by default) contains configuration information for
UberASM Tool, including what resources to use for what level/game
mode/overworld map, along with some other information.  For commands that
take file names, you can either give absolute paths, or you can give
relative paths, in which case UberASM Tool will look relative the directory
of the UberASM Tool executable.

You may add comments to the list file.  A ";" or a "#" on a line is
considered to begin a comment, and the rest of the line is ignored.

The following commands are available:

 - verbose: <on/off/quiet/debug>

   Sets the verbosity level for UAT.  This command is optional and defaults
   to "off" if not specified.

   quiet: Only error messages are displayed
   off:   Basic information about progress is also displayed
   on:    Extra information is displayed, such as individual library/
          resource file progress, and a more detailed breakdown of total
          insert size
   debug: Even more detailed information is displayed for debugging
          purposes.  Also prevents UAT from deleting temporary files
          in asm/work/

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

<number|*> <asm file> [: <extra byte 1> <extra byte 2> ...] [, ...]

<number> is the level number (0-1FF), game mode number (0-FF), or overworld
number (0-6, 0 = Main map; 1 = Yoshi's Island; 2 = Vanilla Dome; 3 = Forest
of Illusion; 4 = Valley of Bowser; 5 = Special World; and 6 = Star World).
Numbers here must be in hexadecimal.  You can also specify "*" instead of a
number, which is a special value that indicates that this resource is to
run on all levels (or game modes, or overworlds).

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
- level 105 is assigned the resource incio.asm (which is in the level/
  folder)
- level 106 is assigned the three resources incio.asm, waves.asm, and
  gradient.asm
- "overworld:" says we're now giving resources for overworld maps
- the resource music.asm (in the overworld/ folder) is assigned to all
  overworlds
- "level:" switches back to specifying resources for levels
- level 107 is assigned the resource special.asm (which takes one extra
  byte) with an extra byte of 100 (decimal), and then the resource
  music.asm from the overworld/ folder.



---------------------------------------------------------------------
-                         The Library                               -
---------------------------------------------------------------------

Some resources may come with external library files.  These should be
placed in UberASM Tool's library/ folder.  Any files in here are
automatically included in your ROM and made available to UberASM resources.
See below for more technical detail on UberASM's library system.




---------------------------------------------------------------------
-                         Common Issues                             -
---------------------------------------------------------------------

As of the 2.0 release of UberASM Tool, there are a couple incompatible
patches: lx5's NSMB star coins patch, and HammerBrother's SMA2 Slide Kill
Chain patch.  Assuming they haven't been updated by the time you read this,
see the "incompatibilities.txt" file for instructions on making them work
with the new version.

---

You may run into resources (patches, uberasm, etc) that want you to copy
and paste some code into UberASM Tool's global code file.  If that code was
destined for the "init:" label, it should work the same, but previous
versions (1.x) also had support for "main:", "load:", and "nmi:" labels,
which are no longer used.  For code that goes under "load:", make a new
file and place the code under a "load:" label, but ending with RTL instead
of RTS as the instructions might suggest.  So,

load:
    <code to paste>
    RTL

Then put the new file in UAT's level/ folder and add it as a resource for
level "*" in your list.txt file.  Similarly, for code that goes under
"main:", make a new file and place the code under both "main:" and "init:".
So,

init:
main:
    <code to paste>
    RTL

Then put the new file in UAT's gamemode/ folder and add it as a resource
for gamemode "*" in your list.txt file.  For global code for the "nmi:"
label, do the same, but just place it under "nmi:" in a new file.

---

Some even older resources may talk about pasting code under "level###:" or
something similar.  If you run into this situation, you'll want to make a
new .asm file (for level, overworld, or game mode, as appropriate) and
paste the given code under a label called "main:", but ending with RTL
instead of RTS.  Similarly, code for "level###init:" should go under a
label called "init:", also ending with RTL instead of RTS.  Then add the
new file to your list.txt wherever you want to use it.

---

As of version 2.0, UberASM Tool will refuse to run on a ROM that has had a
newer version of UberASM Tool used on it.  It's recommended that you simply
upgrade to the current version in this case.  If for some reason you can't
do that, you can force it to run by editing the asm/base/clean.asm file,
and changing the "!OldVersionOverride" define to 1.  But do so at your own
risk; future versions may introduce breaking changes that will prevent
older versions from working correctly.  Also note that versions 1.x make no
such check and will attempt to run anyway.  As of 2.0, it should still work
properly, but again, this may change in the future.



-----------------------------------------------------------------------------

=====================================================================
=                                                                   =
=               Part II: Technical Information                      =
=                                                                   =
=====================================================================

The following sections describe the technical aspects of UAT in more
detail.  This is mainly intended for programmers and resource creators.  If
you're making a hack without getting into the details of ASM programming,
you can probably skip this part, although there may still be some useful
information.



---------------------------------------------------------------------
-                         Resources                                 -
---------------------------------------------------------------------

Resources are the main entities that UberASM Tool uses to achieve affects
in the game.  They can be used for levels, for particular game modes, or
for particular overworld maps.  However, there's no fundamental difference
between the types, and a level resource can also be used as an overworld
resource, for example.  To make a resource for UberASM Tool, you simply
need an .asm file that contains one or more special labels that UAT calls.
These labels are "main:", "init:", "nmi:", "load:" (for levels only), and
"end:" (new to v2.0).  See below for more specific information on each
label.

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

For example, here's a level resource that drains one coin from Mario every
4 frames:

-----------------------------------------------------------
| main:                                                   |
|     LDA $9D                                             |
|     BEQ .Return   ; don't run if sprites are locked     |
|     LDA $0DBF|!addr                                     |
|     BEQ .Return      ; don't run if already at 0 coins  |
|     LDA $14                                             |
|     AND #$03                                            |
|     BNE .Return      ; only run every 4th frame         |
|     DEC $0DBF|!addr  ; decrement coin count             |
| .Return:                                                |
|     RTL                                                 |
-----------------------------------------------------------




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
routine.  You may have noticed that UberASM Tool supports game modes
00-FF, even though SMW only uses game modes 00-29.  Any game mode 2A
or higher is a custom mode, and thus there will be no base SMW routine to
run (except for the standard NMI routine).

For all resource types, the "nmi:" label runs early during SMW's NMI
routine.  Level resources have their NMI label called during game modes $13
and $14, and overworld resources during game modes $0D and $0E.  In both
cases, actual game mode code under "nmi:" will run first.  See the "NMI"
section for more information.



---------------------------------------------------------------------
-                         The DBR                                   -
---------------------------------------------------------------------

By default, UAT sets the DBR (data bank register) to the appropriate bank
when calling resource code for labels other than nmi:.  This behavior is
often not needed, and can be turned off for a resource if its .asm file
contains the exact line (starting at column 1 even)

;>dbr off

somewhere in the source file.  Note that if you do need to set it manually,
you don't need to worry about restoring it (unless set to a bank without
RAM mirrors); UAT will do so after all resources have been called.



---------------------------------------------------------------------
-                         Extra Bytes                               -
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
|     RTL                             |
---------------------------------------

Note that if you write a resource that uses extra bytes, but someone
attempts to insert it with an older (1.x) version of UberASM Tool, it will
still appear to insert correctly, but it won't.  You can use the
%require_uber_ver() macro to cause it to fail to insert in older
versions; see the "Other Code Files" section for more information.



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
-                         The Library                               -
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
more generally see labels of) other library files.  However, as of version
2.0, UberASM Tool has support for a shared routine system similar to that
of Pixi and GPS.  See below for more information.



---------------------------------------------------------------------
-                         Shared Routines                           -
---------------------------------------------------------------------

UberASM Tool 2.0 adds support for shared routines.  These are similar to
those of Pixi and GPS, but are implemented a little differently.  Shared
routines are simply .asm files that have common code that different
resources can call (one routine per file).  To call a shared routine called
"Name", add the line

    %UberRoutine(Name)

to your resource code.  (This differs from Pixi and GPS, where you'd simply
call %Name() instead).  Routine files must be located in the routines/
directory; see there for what's already included and available to use.
Note that unlike libraries, routines are added to the ROM on-demand.  If
nothing uses a routine, it won't take up any space in the ROM.  Also unlike
libraries, shared routines may call other shared routines freely.  And note
that libraries may call shared routines.  On the other hand, whether or not
a shared routine can call a library will depend on where the routine is
first used; if it's first used from a resource, it will work, but if it's
first used from a library, it won't (because libraries can't call other
libraries).  It's probably best to just avoid this case altogether.

To make your own shared routine, simply create a new asm file with the name
of the routine in the routines/ directory (it must be directly in
routines/, not in a subdirectory).  Execution starts at the beginning of
the file; no label is needed there.  The routine is JSLed to, so the DBR is
not set automatically, and your routine should end with an RTL.

IMPORTANT: Due to the way shared routines are implemented, any labels in
the routine MUST be macro-local; that is, they must start with a question
mark.  So, for example, a shared routine that returns the first open sprite
slot in X (or $FF if there are none) might look like the following:

routines/FindFreeSpriteSlot.asm
------------------------------------
|     LDX #!sprite_slots-1         |
| ?-                               |
|     LDA !sprite_status,x         |
|     BEQ ?Return                  |
|     DEX                          |
|     BPL ?-                       |
| ?Return:                         |
|     RTL                          |
------------------------------------

You can then call this from any library, resource, or other shared routine
file by using %UberResource(FindFreeSpriteSlot).

---------------------------------------------------------------------
-                         Other Code Files                          -
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
normal main: code, right before the status bar is updated.  Any code
here should also end in RTS instead of RTL

Note that unlike regular resources, code for the status bar and global
code files is included with the main UberASM patch, rather than
getting their own freecode blocks.  Due to how these are built, you
also can't use the %prot macros in these.

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

As of version 2.0, you can now also specify that UberASM Tool should run
resource labels on the SA-1 CPU if it's available (i.e., it's safe to use
this option on all ROMs -- it simply does nothing on non-SA-1 ROMs).  This
works for any resource labels except "nmi:" -- it will always be run on the
SNES side.  To do so, have a line in the resource .asm file exactly of the
form:

;>sa1 <comma separated list of labels>

with no extra spaces.  For example, if a resource file contains the line

;>sa1 main,end

then SA-1 ROMs will have "main:" and "end:" run on SA-1, while "init:" and
"load:" (if present) will run on the SNES side.  Since older versions of
UberASM Tool don't support this feature, consider using
%require_uber_version(2, 0) here, especially if you have code that won't
work properly without the SA-1 CPU being invoked as expected (such as when
using the multiplication and division registers).  Also keep in mind that
there's some overhead in invoking the SA-1 CPU, so it may not be worth it
for very short routines.  The DBR will still be set by default, unless
the resource file also contains ";>dbr off".  See "The DBR" section for
more information.

---------------------------------------------------------------------
-                         Dos and Don'ts                            -
---------------------------------------------------------------------

Just a few random reminders for good practice when making resources:

- UAT turns on nested namespaces for resources and libraries; DON'T turn
them off.  And if you do set a namespace in your code somewhere, DO make
sure to exit it as well

- DON'T add your own hijacks.  It's possible to do, but it runs the risk
of confusing the patching process.  Just leave patches in a separate
file to be applied with Asar directly.

- DO %require_uber_ver(2,0) if your resource uses extra bytes.  Failing
to do so will let someone add it with version 1.x appear to add it, but
it won't work correctly due to lack of extra byte support.  Consider
including this even for features that will explicitly fail on older
versions.  This helps the user know quickly that they need to upgrade.

=============================================================================


---------------------------------------------------------------------
-                             Credits                               -
---------------------------------------------------------------------

UberASM Tool was inspired by GPS, Sprite Tool, edit1754's levelASM tool,
levelASM+NMI version and 33953YoShI's japanese levelASM tool.

Vitor would like to thank:
 - edit1754 for the original LevelASM Tool idea;
 - p4plus2 for the original uberASM patch;
 - Alcaro/byuu/Raidenthequick for Asar; and
 - 33953YoShI/Mirann/Wakana for testing.
 - 33953YoShI again for giving me the LOAD label base hijack and idea.

Fernap would also like to thank:
 - Atari2.0 and TheBiob for help with Visual Studio

---------------------------------------------------------------------
-                             Contact                               -
---------------------------------------------------------------------

You can contact Vitor though the following links:

* My Github profile: https://github.com/VitorVilela7
* My Twitter profile: https://twitter.com/HackerVilela
* My personal blog: https://vilela.sneslab.net/

You can contact Fernap at

* My SMWC profile: https://www.smwcentral.net/?p=pm&do=compose&user=48050
* Or as Fernap#8699 on the SMWC Discord: https://discord.gg/smwc
