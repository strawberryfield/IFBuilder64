; Adventure Player 2026
; copyright (c) 2026 Roberto Ceccarelli
; https://strawberryfield.altervista.org
; Released under GNU Affero GPL 3.0
;
; routines for peeking under ROM
;---------------------
; working code

ListCounter 	= $0334
ListData	= $0335

Peek		.block

GetIndexPtr	#SearchWord		; address
		jsr Common.GetNumVar	; destination var
		#BasicOFF
		ldy #0
		lda (WAC_LO),y
		sta $02
		rts
		
PeekByte	jsr GetIndexPtr		; return byte content
		#AllRomON
		lda $02
SaveByte	sta FAC1+1
		lda #0
		sta FAC1+2
		ldx #$88
Save		sec
		jsr $bc49
SaveToVar	ldx CurVarPtr
		ldy CurVarPtr+1
		jsr CopyFAC1toXY
		jmp EndStatement
				
PeekWord	jsr GetIndexPtr		; return word content
		iny
		lda (WAC_LO),y
		tax
		#AllRomON
		stx FAC1+1
		lda $02
		sta FAC1+2
		ldx #$90
		jmp Save

PokeWord	jsr Common.GetWordToFE	; save word to a couple of bytes
		#SearchWord
		ldy #0
		lda WAC_LO
		sta ($fe),y
		iny
		lda WAC_HI
		sta ($fe),y
		rts

GetBit		jsr GetIndexPtr
		#AllRomON
		#SearchByte
		txa
		and $02
		jmp SaveByte
		
BitMask		#SearchWord
		#SearchByte
		txa
		ldy #0
		rts
		
SetBit		jsr BitMask		
		ora (WAC_LO),y
		sta (WAC_LO),y
		rts

ResetBit	jsr BitMask
		eor #$ff
		and (WAC_LO),y
		sta (WAC_LO),y
		rts
		
Scan		#SearchWord
		#SearchByte
		stx $02
		#SearchByte
		lda #<ListData
		sta $fe
		lda #>ListData
		sta $ff
		lda #0
		sta ListCounter
		txa
		ldy $02
scan_loop	dey
		cmp (WAC_LO),y
		bne check_loop
scan_add	pha
		tya
		pha
		ldy ListCounter
		sta ($fe),y
		inc ListCounter
		pla
		tay
		pla
check_loop	cpy #0
		bne scan_loop
		rts
		
		.endblock
		

