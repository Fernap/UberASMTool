incsrc "uber_defines.asm"
incsrc "!MacrolibFile"   ; global defines file

if !UberBinary
    freedata cleaned
    print "_startl ", pc
    incbin "../../library/!UberFilename"
else
    freecode cleaned
    print "_startl ", pc
    incsrc "../../library/!UberFilename"
endif

print "_endl ", pc
