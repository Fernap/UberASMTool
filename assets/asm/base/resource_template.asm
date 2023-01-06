;This file is included by a working copy in asm/work, once for each resource
;
;From the asar doc:
;   (Caution: when used inside macros, paths are relative to the macro definition rather than to the macro call).
;so I guess this means that since the macro is in asm/base, I have to work relative to that, regardless of where I include the file with the macro from
;
;Note this sets the DBR (if specified), but doesn't restore it...should be fine as long as the DBR never gets set a bank without low ram mirrors,
;   which would only happen by a misbehaving bit of code in a high bank or something.
;It will be explicitly restored after all resources are called
;
;---------------------------------------

incsrc "!MacrolibFile"
incsrc "../work/library_labels.asm"

namespace nested on

macro UberResource(filename, setdbr)
    freecode cleaned

    print "_startl ", pc

    init:
    main:
    end:
    load:
        rtl

    namespace Inner

    ResourceEntry:
        if <setdbr>
            phk
            plb
        endif
        lda $06,S
        tax
        jmp (ResourceLabels,x)

    ResourceLabels:
        dw init
        dw main
        dw end
        dw load

    incsrc "<filename>"

    ExtraBytes:
    incsrc "../work/extra_bytes.asm"

    print "_endl ", pc
    namespace off
endmacro