        ; Test program for LC3 simulator
        loc #0 ; not needed but why not
        ; Load 1 in CC Z
        ADD R0 R0 R0 ; R0 = 0
        ; Jump to program (i guessed the index though)
        BRZ #14
        
        loc #16
        ADD R0 R0 #10
        ADD R1 R1 #$1F ; R1 = 0 - 1 = 65535
        ADD R2 R2 #$1
        ADD R3 R1 R2   ; but, this should give 0 but mem dump says 2
        JSRR R0
        TRAP
        
        loc #10
        AND R4 R0 R1
        RET