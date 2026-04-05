; Adventure Player 2026
; copyright (c) 2026 Roberto Ceccarelli
; https://strawberryfield.altervista.org
; Released under GNU Affero GPL 3.0
;
; routines for word search

;---------------------
; working code


Search		.block

LIST_PTR   	= $FB    	; puntatore (2 byte) alla lista
STR_PTR    	= $FD    	; puntatore (2 byte) alla stringa cercata
WORD_IDX      	= $FF    	; indice parola

Search_Word	#SearchWord		; puntatore alla tabella
		lda WAC_LO
		sta LIST_PTR
		lda WAC_HI
		sta LIST_PTR+1

		#SearchExpr		; parola da cercare
		jsr Common.check_type_str
		ldy #0
		lda (FACMO),y
		beq not_found
		sta $02			; save string length		
		iny
		lda (FACMO),y
		sta FAC2
		iny
		lda (FACMO),y
		sta FAC2+1
		lda FACMO		; clean stack
		ldy FACMO+1
		jsr StringStackClean
		lda $02 
		sec
		adc #0
		jsr AllocateString	; allocate space 
		stx STR_PTR
		sty STR_PTR+1
		ldy $02
		lda #0
		sta (STR_PTR),y
copy_loop	dey
		lda (FAC2),y		
		sta (STR_PTR),y
		cpy #0
		bne copy_loop
		
; ----------------------------------------
; FIND_WORD
; ----------------------------------------
; In:
;   LIST_PTR = puntatore lista parole
;   STR_PTR  = puntatore stringa cercata
; Out:
;   A = indice parola (0,1,2...)
;   A = $FF se non trovata
; ----------------------------------------

		#BasicOFF
FIND_WORD       LDX #$00
		STX WORD_IDX		; indice parola

NEXT_WORD       ldy #$0
		LDA (LIST_PTR),Y
		BEQ not_found     	; $00 all'inizio = fine lista
	
		JSR CMP_WORD
		BEQ FOUND

SKIP_WORD       LDA (LIST_PTR),Y	; salta parola corrente
		BEQ SKIP_END
		INY
		BNE SKIP_WORD

SKIP_END        INY              	; salta lo zero
		TYA
		CLC
		ADC LIST_PTR
		STA LIST_PTR
		BCC skip_msb
		INC LIST_PTR+1	
skip_msb	INC WORD_IDX
		JMP NEXT_WORD
		
found       	LDX WORD_IDX
		JMP exit
not_found       LDX #$FF
exit		#AllRomON
		txa
		RTS

; ----------------------------------------
; CMP_WORD
; ----------------------------------------
; Confronta parola lista con stringa
; Y = offset parola corrente
; Z = 1 se uguali
; ----------------------------------------

CMP_WORD	LDY #$00

CMP_LOOP        LDA (LIST_PTR),Y
		CMP (STR_PTR),Y
		BNE CMP_FAIL

CMP_BOTH_ZERO	LDA (LIST_PTR),Y
		BEQ CHECK_STR_END   	; fine parola lista?
		INY
		BNE CMP_LOOP

CHECK_STR_END   LDA (STR_PTR),Y
		BEQ CMP_OK          	; entrambi zero → OK
					; lista finita ma stringa no
CMP_FAIL        LDA #$01
CMP_OK		RTS
		
		.endblock
		

