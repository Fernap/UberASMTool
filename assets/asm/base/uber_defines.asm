; This file is automatically included into all other UberASM Tool files.
; If you want to add your own, use the macro library file (see the readme for more information)

; Check if SA-1 is present.
if read1($00FFD5) == $23
    if read1($00FFD7) == $0D        ; rom size > 4mb
        fullsa1rom
        !fullsa1 = 1
    else
        sa1rom
        !fullsa1 = 0
    endif

    !sa1    = 1
    !dp     = $3000
    !addr   = $6000
    !bank   = $000000
    !bank8  = $00

    !sprite_slots = 22
else
    lorom

    !sa1     = 0
    !fullsa1 = 0
    !dp      = $0000
    !addr    = $0000
    !bank    = $800000
    !bank8   = $80

    !sprite_slots = 12
endif

if read1($0FFFE0)&$01 == $01
    !Has255SpritesPerLevel = 0
else
    !Has255SpritesPerLevel = 1
endif

; set this to 1 if you want to prevent level nmi: code from running on the title screen (old behavior); see the readme for more information
!DisableTitleScreenLevelNMI = 0

!EXLEVEL = 0
if (((read1($0FF0B4)-'0')*100)+((read1($0FF0B4+2)-'0')*10)+(read1($0FF0B4+3)-'0')) > 253
	!EXLEVEL = 1
endif

; Fail if the current running version of UAT is below major.minor
macro require_uber_ver(major, minor)
    if (<major>*256)+<minor> > (!UberMajorVersion*256)+!UberMinorVersion
        error "This resource requires UberASM Tool version at least <major>.<minor>"
    endif
endmacro

macro UberRoutine(name)
    if not(defined("UberRoutine_<name>"))
        pushpc
        freecode cleaned
        print "_startl ", pc
        global #UberRoutine_<name>:
        !UberRoutine_<name> = UberRoutine_<name>
        incsrc "../../routines/<name>.asm"
        print "_endl ", pc
        pullpc
    endif

    jsl !UberRoutine_<name>
endmacro

; Protect binary file.
macro prot_file(file, label)
    pushpc
        freedata cleaned
        print "_startl ", pc
        
        <label>:
            incbin "../../<file>"
        print "_endl ", pc
    pullpc
endmacro

; Protect external source code.
macro prot_source(file, label)
    pushpc
        freecode cleaned
        print "_startl ", pc
        
        <label>:
            incsrc "../../<file>"
        print "_endl ", pc
    pullpc
endmacro

; Generic macro for moving data blocks.
; Destroys A/X/Y.
; Parameters: src = source to read, dest = destination to write, len = total bytes to copy.
macro move_block(src,dest,len)
    PHB
    REP #$30
    LDA.w #<len>-1
    LDX.w #<src>
    LDY.w #<dest>
    MVN <dest>>>16,<src>>>16
    SEP #$30
    PLB
endmacro

; Macro for calling SA-1 CPU. Label should point to a routine which ends in RTL.
; Data bank is not set, so use PHB/PHK/PLB ... PLB in your SA-1 code.
macro invoke_sa1(label)
    LDA.b #<label>
    STA $3180
    LDA.b #<label>>>8
    STA $3181
    LDA.b #<label>>>16
    STA $3182
    JSR $1E80
endmacro

; Macro for calling SNES CPU (from SA-1 CPU). Label should point to a routine which ends in RTL.
; Data bank is not set automatically.
macro invoke_snes(addr)
    LDA.b #<addr>
    STA $0183
    LDA.b #<addr>>>8
    STA $0184
    LDA.b #<addr>>>16
    STA $0185
    LDA #$D0
    STA $2209
?-
    LDA $018A
    BEQ ?-
    STZ $018A
endmacro

macro define_sprite_table(name, addr, addr_sa1)
    if !sa1
        !<name> = <addr_sa1>
    else
        !<name> = <addr>
    endif
endmacro

macro define_base2_address(name, addr)
    if !sa1
        !<name> #= <addr>|!addr
    else
        !<name> = <addr>
    endif
endmacro

; Regular sprite defines -------------------------------------------

%define_sprite_table("7FAB10",$7FAB10,$400040)
%define_sprite_table("7FAB1C",$7FAB1C,$400056)
%define_sprite_table("7FAB28",$7FAB28,$400057)
%define_sprite_table("7FAB34",$7FAB34,$40006D)
%define_sprite_table("7FAB9E",$7FAB9E,$400083)
%define_sprite_table("7FAB40",$7FAB40,$400099)
%define_sprite_table("7FAB4C",$7FAB4C,$4000AF)
%define_sprite_table("7FAB58",$7FAB58,$4000C5)
%define_sprite_table("extra_bits",$7FAB10,$400040)
%define_sprite_table("new_code_flag",$7FAB1C,$400056)
%define_sprite_table("extra_prop_1",$7FAB28,$400057)
%define_sprite_table("extra_prop_2",$7FAB34,$40006D)
%define_sprite_table("new_sprite_num",$7FAB9E,$400083)
%define_sprite_table("extra_byte_1",$7FAB40,$400099)
%define_sprite_table("extra_byte_2",$7FAB4C,$4000AF)
%define_sprite_table("extra_byte_3",$7FAB58,$4000C5)
%define_sprite_table("extra_byte_4",$7FAB64,$4000DB)
%define_sprite_table(sprite_num, $9E, $3200)
%define_sprite_table(sprite_speed_y, $AA, $9E)
%define_sprite_table(sprite_speed_x, $B6, $B6)
%define_sprite_table(sprite_misc_c2, $C2, $D8)
%define_sprite_table(sprite_y_low, $D8, $3216)
%define_sprite_table(sprite_x_low, $E4, $322C)
%define_sprite_table(sprite_status, $14C8, $3242)
%define_sprite_table(sprite_y_high, $14D4, $3258)
%define_sprite_table(sprite_x_high, $14E0, $326E)
%define_sprite_table(sprite_speed_y_frac, $14EC, $74C8)
%define_sprite_table(sprite_speed_x_frac, $14F8, $74DE)
%define_sprite_table(sprite_misc_1504, $1504, $74F4)
%define_sprite_table(sprite_misc_1510, $1510, $750A)
%define_sprite_table(sprite_misc_151c, $151C, $3284)
%define_sprite_table(sprite_misc_1528, $1528, $329A)
%define_sprite_table(sprite_misc_1534, $1534, $32B0)
%define_sprite_table(sprite_misc_1540, $1540, $32C6)
%define_sprite_table(sprite_misc_154c, $154C, $32DC)
%define_sprite_table(sprite_misc_1558, $1558, $32F2)
%define_sprite_table(sprite_misc_1564, $1564, $3308)
%define_sprite_table(sprite_misc_1570, $1570, $331E)
%define_sprite_table(sprite_misc_157c, $157C, $3334)
%define_sprite_table(sprite_blocked_status, $1588, $334A)
%define_sprite_table(sprite_misc_1594, $1594, $3360)
%define_sprite_table(sprite_off_screen_horz, $15A0, $3376)
%define_sprite_table(sprite_misc_15ac, $15AC, $338C)
%define_sprite_table(sprite_slope, $15B8, $7520)
%define_sprite_table(sprite_off_screen, $15C4, $7536)
%define_sprite_table(sprite_being_eaten, $15D0, $754C)
%define_sprite_table(sprite_obj_interact, $15DC, $7562)
%define_sprite_table(sprite_oam_index, $15EA, $33A2)
%define_sprite_table(sprite_oam_properties, $15F6, $33B8)
%define_sprite_table(sprite_misc_1602, $1602, $33CE)
%define_sprite_table(sprite_misc_160e, $160E, $33E4)
%define_sprite_table(sprite_index_in_level, $161A, $7578)
%define_sprite_table(sprite_misc_1626, $1626, $758E)
%define_sprite_table(sprite_behind_scenery, $1632, $75A4)
%define_sprite_table(sprite_misc_163e, $163E, $33FA)
%define_sprite_table(sprite_in_water, $164A, $75BA)
%define_sprite_table(sprite_tweaker_1656, $1656, $75D0)
%define_sprite_table(sprite_tweaker_1662, $1662, $75EA)
%define_sprite_table(sprite_tweaker_166e, $166E, $7600)
%define_sprite_table(sprite_tweaker_167a, $167A, $7616)
%define_sprite_table(sprite_tweaker_1686, $1686, $762C)
%define_sprite_table(sprite_off_screen_vert, $186C, $7642)
%define_sprite_table(sprite_misc_187b, $187B, $3410)
%define_sprite_table(sprite_tweaker_190f, $190F, $7658)
%define_sprite_table(sprite_misc_1fd6, $1FD6, $766E)
%define_sprite_table(sprite_cape_disable_time, $1FE2, $7FD6)
%define_sprite_table("9E", $9E, $3200)
%define_sprite_table("AA", $AA, $9E)
%define_sprite_table("B6", $B6, $B6)
%define_sprite_table("C2", $C2, $D8)
%define_sprite_table("D8", $D8, $3216)
%define_sprite_table("E4", $E4, $322C)
%define_sprite_table("14C8", $14C8, $3242)
%define_sprite_table("14D4", $14D4, $3258)
%define_sprite_table("14E0", $14E0, $326E)
%define_sprite_table("14EC", $14EC, $74C8)
%define_sprite_table("14F8", $14F8, $74DE)
%define_sprite_table("1504", $1504, $74F4)
%define_sprite_table("1510", $1510, $750A)
%define_sprite_table("151C", $151C, $3284)
%define_sprite_table("1528", $1528, $329A)
%define_sprite_table("1534", $1534, $32B0)
%define_sprite_table("1540", $1540, $32C6)
%define_sprite_table("154C", $154C, $32DC)
%define_sprite_table("1558", $1558, $32F2)
%define_sprite_table("1564", $1564, $3308)
%define_sprite_table("1570", $1570, $331E)
%define_sprite_table("157C", $157C, $3334)
%define_sprite_table("1588", $1588, $334A)
%define_sprite_table("1594", $1594, $3360)
%define_sprite_table("15A0", $15A0, $3376)
%define_sprite_table("15AC", $15AC, $338C)
%define_sprite_table("15B8", $15B8, $7520)
%define_sprite_table("15C4", $15C4, $7536)
%define_sprite_table("15D0", $15D0, $754C)
%define_sprite_table("15DC", $15DC, $7562)
%define_sprite_table("15EA", $15EA, $33A2)
%define_sprite_table("15F6", $15F6, $33B8)
%define_sprite_table("1602", $1602, $33CE)
%define_sprite_table("160E", $160E, $33E4)
%define_sprite_table("161A", $161A, $7578)
%define_sprite_table("1626", $1626, $758E)
%define_sprite_table("1632", $1632, $75A4)
%define_sprite_table("163E", $163E, $33FA)
%define_sprite_table("164A", $164A, $75BA)
%define_sprite_table("1656", $1656, $75D0)
%define_sprite_table("1662", $1662, $75EA)
%define_sprite_table("166E", $166E, $7600)
%define_sprite_table("167A", $167A, $7616)
%define_sprite_table("1686", $1686, $762C)
%define_sprite_table("186C", $186C, $7642)
%define_sprite_table("187B", $187B, $3410)
%define_sprite_table("190F", $190F, $7658)
%define_sprite_table("1FD6", $1FD6, $766E)
%define_sprite_table("1FE2", $1FE2, $7FD6)

if !Has255SpritesPerLevel
    %define_sprite_table("1938", $7FAF00, $418A00)
    %define_sprite_table("7FAF00", $7FAF00, $418A00)
    %define_sprite_table(sprite_load_table, $7FAF00, $418A00)
else
    %define_sprite_table("1938", $1938, $418A00)
    %define_sprite_table(sprite_load_table, $1938, $418A00)
endif

; this macro can be called in the macrolib file to force the defines, if needed
macro Force255SpritesPerLevel()
    !Has255SpritesPerLevel = 1
    %define_sprite_table("1938", $7FAF00, $418A00)
    %define_sprite_table("7FAF00", $7FAF00, $418A00)
    %define_sprite_table(sprite_load_table, $7FAF00, $418A00)
endmacro

; Extended sprite defines -----------------------------------------

!ExtendedOffset = $13

%define_base2_address(extended_num,$170B)
%define_base2_address(extended_y_low,$1715)
%define_base2_address(extended_y_high,$1729)
%define_base2_address(extended_x_low,$171F)
%define_base2_address(extended_x_high,$1733)
%define_base2_address(extended_x_speed,$1747)
%define_base2_address(extended_y_speed,$173D)
%define_base2_address(extended_x_fraction,$175B)
%define_base2_address(extended_y_fraction,$1751)
%define_base2_address(extended_table,$1765)
%define_base2_address(extended_timer,$176F)
%define_base2_address(extended_behind,$1779)

%define_base2_address(extended_table_1,$1765)
%define_base2_address(extended_table_2,$198C)
%define_base2_address(extended_table_3,$1996)
%define_base2_address(extended_table_4,$19A0)
%define_base2_address(extended_table_5,$19AA)

; Private defines; do not use or change -----------------------------

!UberOffsetInit = $00
!UberOffsetMain = $02
!UberOffsetEnd  = $04
!UberOffsetLoad = $06
