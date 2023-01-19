; MoveSpriteRelativePlayer
;
; Sets the sprite's position relative to that of the player
;
; Input:
;     X  : index to sprite
;     $00: signed x offset to the player for the sprite
;     $01: signed y offset to the player for the sprite
;

?main:
    lda $00
    clc : adc $94
    sta !sprite_x_low,x
    lda #$00
    bit $00
    bpl ?+
    dec
?+
    adc $95
    sta !sprite_x_high,x

    lda $01
    clc : adc $96
    sta !sprite_y_low,x
    lda #$00
    bit $01
    bpl ?+
    dec
?+
    adc $97
    sta !sprite_y_high,x

    rtl
