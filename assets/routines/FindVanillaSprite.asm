; FindVanillaSprite - tries to find the non-custom sprite slot occupied by the given sprite number
;                     Pixi must be installed in order to call this
;
; Input:
;     X   : sprite slot to begin searching in, plus 1 (searches downwards, safe to pass $00, which will always result in no match)
;     $00 : the sprite number to search for (custom sprites will be ignored)
;
; Output:
;     X : sprite slot containing the sprite (which might be in a dead state), or $FF is not found
;     Carry bit clear if found, carry bit set if not found

?main:

?-
    dex
    bmi ?.not_found
    lda !sprite_status,x
    beq ?-
    lda !sprite_num,x
    cmp $00
    bne ?-
    lda !extra_bits,x
    and #$08
    bne ?-

?.found:
    clc
    rtl

?.not_found:
    sec
    rtl
