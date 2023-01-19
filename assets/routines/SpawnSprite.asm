; Adapted from Pixi 1.4

; SpawnSprite
;
; Routine that spawns a normal or custom sprite.  Does not set speed (but defaults to 0) or position.
; Use MoveSpriteRelativePlayer if you want to set it relative to the player's position
;
; Input:  A   = number of sprite to spawn
;         C   = Carry bit clear for a normal sprite, set for a custom sprite
;
; Output: X   = index to spawned sprite (#$FF means no sprite spawned)
;         C   = Carry Set = spawn failed, Carry Clear = spawn successful.

?main:
    XBA
    LDX #!sprite_slots-1

?-
    LDA !sprite_status,x
    BEQ ?.found_slot
    DEX
    BPL ?-

    SEC
    RTL

?.found_slot:
    XBA
    STA !sprite_num,x
    JSL $07F7D2|!bank
    BCC ?.not_custom

    LDA !sprite_num,x
    STA !new_sprite_num,x

    PEI ($00)
    LDA $02 : PHA
    JSL $0187A7|!bank            ; clobbers $00-$02
    PLA : STA $02
    PLA : STA $00
    PLA : STA $01
        
    LDA #$08
    STA !extra_bits,x
?.not_custom:
    
    LDA #$01
    STA !sprite_status,x
    
    CLC
    RTL    
