; Teleport - teleports the player to a given level, adapted from GPS
;            must be called with A in 16-bit mode, and returns with A in 8-bit mode
;
; Input:
;     A  : The (16-bit) level to teleport to
;
; Clobbers A,X,Y

    PHA
    STZ $88

    SEP #$30

    if !EXLEVEL
        JSL $03BCDC|!bank
    else
        LDX $95
        PHA
        LDA $5B
        LSR
        PLA
        BCC ?+
        LDX $97
    ?+
    endif
    PLA
    STA $19B8|!addr,x
    PLA
    ORA #$04
    STA $19D8|!addr,x

    LDA #$06
    STA $71

    RTL
