; don't really need the if/endif around %NMILevelAllJSLs(), etc anymore since that macro will be empty iff the define is 0

; this preserves/uses/restores 7E:0000-0001 (in WRAM), always
; using $00-01 (which is xx:3000-1 in IRAM) is unsafe during lag on sa-1 since both CPUs are running...using WRAM ensures that the sa-1 side can't access it

; I guess I could have a separate entry for NMI that doesn't set DBR, etc?
; think about this some more
; note there's no longer nmi: for the global code file

; various defines that UAT needs to set:
; UberUseNMI - 1 if there's any NMI code to run at all...if not, the NMI hijack is removed and this file isn't even included (this is just a logical OR of the three overall values below)
; UberGamemodeNMI - 1 if any gamemode code has nmi: labels (this is just a logical OR of the following two values)
; UberGamemodeNMIAll - 1 if there are any gamemode nmi: labels for files that always run (i.e., with *)
; UberGamemodeNMINormal - 1 if there are any gamemode nmi: labels for specific gamemodes rather than global
; ditto the above 3, but also for Level and Overworld

;---------------------------------------------

if !sa1
    !STAScratchLow  = "sta.w $0000"
    !STAScratchHigh = "sta.w $0001"
else
    !STAScratchLow  = "sta.b $00"
    !STAScratchHigh = "sta.b $01"
endif

NMIHijack:
    lda $4210    ; restore clobbered -- clears N flag, probably required
    phb

; note: saving/restoring $00 is unsafe on sa-1 during lag, but $0000-0001 is still scratch in WRAM rather than IRAM, so is safe to use
    if !sa1
        lda.w $0001 : pha
        lda.w $0000 : pha
    else
        pei ($00)
    endif

; gamemode nmi

.Gamemode:
    if !UberGamemodeNMIAll
        %GamemodeAllNMIJSLs()       ; macro defined below, added by UAT..just a bunch of JSLs
    endif

    if !UberGamemodeNMINormal
        rep #$30
        lda $0100|!addr
        asl
        tax
        lda.l NMIGamemodeResourcePointers,x  ; these point to the lists-of-jsls
        !STAScratchLow
        sep #$30
        ldx #$00
        jsr ($0000,x)
    endif


; level nmi
; run for GMs 13 & 14
.Level:
    if !UberLevelNMI
        lda $0100|!addr
        cmp #$13
        beq +
        cmp #$14
        bne .Overworld
    +
    endif

    if !UberLevelNMIAll
        %LevelAllNMIJSLs()          ; macro defined below, added by UAT..just a bunch of JSLs
    endif

    if !UberLevelNMINormal
        rep #$30
        lda !level
        asl
        tax
        lda.l NMILevelResourcePointers,x  ; these point to the lists-of-jsls
        !STAScratchLow
        sep #$30
        ldx #$00
        jsr ($0000,x)
    endif

    if !UberLevelNMI
        ; return since overworld won't run also
        pla : !STAScratchLow
        pla : !STAScratchHigh
        plb
        lda $1DFB|!addr
        jml $00817C|!bank
    endif

; overworld nmi
; GMs 0D & 0E
.Overworld:
    if !UberOverworldNMI
        if not(!UberLevelNMI)    ; otherwise A already has the current gamemode
            lda $0100|!addr
        endif
        cmp #$0D
        beq +
        cmp #$0E
        bne .Return
    +
    endif

    if !UberOverworldNMIAll
        %OverworldAllNMIJSLs()      ; macro defined below, added by UAT..just a bunch of JSLs
    endif

    if !UberOverworldNMINormal
        ldx $0DB3|!addr          ; 0 = mario, 1 = luigi
        lda $1F11|!addr,x        ; current OW map for player x
        asl
        tax
        lda.l NMIOverworldResourcePointers,x : !STAScratchLow
        lda.l NMIOverworldResourcePointers+1,x : !STAScratchHigh
        ldx #$00
        jsr ($0000,x)
    endif

.Return:
    pla : !STAScratchLow
    pla : !STAScratchHigh
    plb
    lda $1DFB|!addr
    jml $00817C|!bank
