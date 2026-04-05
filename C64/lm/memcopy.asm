; Adventure Player 2026
; copyright (c) 2026 Roberto Ceccarelli
; https://strawberryfield.altervista.org
; Released under GNU Affero GPL 3.0
;
; routines for copying memory
;---------------------
; working code


MemCopy		.block

OrigStart	= $fb
OrigEnd		= $fd

CopyRAM		#SearchWord
		lda WAC_LO
		sta OrigStart
		lda WAC_HI
		sta OrigStart+1
		#SearchWord
		lda WAC_LO
		sta OrigEnd
		lda WAC_HI
		sta OrigEnd+1
		#SearchWord
		ldy OrigStart
		ldx OrigStart+1
		lda #0
		sta OrigStart
		sta WAC_LO
		sei
		#kernalOFF
copy_loop	lda (OrigStart),y
		sta (WAC_LO),y
		iny	
		bne copy_loop
		inx
		cpx OrigEnd+1
		beq end_loop
		stx OrigStart+1
		inc WAC_HI
		jmp copy_loop
end_loop	#AllRomON
		cli
		rts
		
		.endblock
		

