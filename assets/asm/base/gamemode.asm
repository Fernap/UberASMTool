macro CallGamemodeResources()
    phx                      ; this call macro expects the offset in X due to needing to use A for other stuff

    pha : pha                ; 2 dummy bytes so that the offset is at $06,S instead of $04
    %GamemodeAllJSLs()       ; macro defined below, added by UAT..just a bunch of JSLs
    pla : pla

    rep #$30
    lda !previous_mode
    asl
    tax
    lda.l GamemodeResourcePointers,x  ; these point to the lists-of-jsls
    sta $00
    sep #$30
    ldx #$00
    jsr (!dp,x)
    pla
endmacro

; I'm only keeping one of these to store everything in...(the one before what the hijack at $804E jumps to)
;Editing or moving these tables breaks things. don't.
;db "uber"
;level_asm_table:
;level_init_table:
;level_nmi_table:
;level_load_table:
;db "tool"

CallGamemode:
    phb

    ldx #$02                 ; offset for main
    lda $0100|!addr
    cmp !previous_mode
    sta !previous_mode
    beq +
    ldx #$00                 ; previous and current modes not the same, so load offset for init instead
+
    %CallGamemodeResources()
    plb

; run original game mode if it's <= $29

    lda !previous_mode
    cmp #$29
    bcs .End                   ; if this frame's mode is > $29, it's a "new" mode, so just skip down to handle "end:" label

    asl                        ; $00-$29, an "old" mode, so jump to that
    tax
    lda $9329,x : sta $00      ; game mode routine ptr, low byte
    lda $9330,x : sta $01      ; game mode routine ptr, high byte
    if !bank8 != $00
        lda.b #!bank8 : sta $02
    else
        stz $02
    endif
    phk
    pea .End-1
    pea $84CF-1
    jml [!dp]                ; game mode routines end in rts, so we need this

.End:
    phb
    ldx #$04                   ; offset for end
    %CallGamemodeResources()
    plb

; return from hijack
    jml $008075|!bank

