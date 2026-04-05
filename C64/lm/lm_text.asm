; Tensione nei circuiti elettrici in corrente alternata
; copyright (c) 1984,2025 Roberto Ceccarelli
; https://strawberryfield.altervista.org
; Released under GNU Affero GPL 3.0
;
; routines per la gestione schermo modo testo

;---------------------
; working code
TextMode	.block	

ColorSrc	= $fa    
CharDest	= $fc
ColorDest	= $fe			
	
;----------------------
; posizionamento cursore modo testo
PosCursor	#SearchByte		; parametro per colonna
		txa
		pha			; memorizza nello stack
		#SearchByte		; parametro per riga
		pla
		tay			; recupera colonna
		jmp SetCursor		; posiziona cursore

;----------------------
; cancellazione riga	
CleanRow	#SearchByte		; parametro per riga
		jmp CleanTextRow

;----------------------
; copy text screen and colors
CopyScreen	#SearchWord
 		lda WAC_LO
		sta ColorSrc
		clc
		lda WAC_HI
		adc #$4
		sta ColorSrc+1
		lda #$4
		sta CharDest+1
		lda #$d8
		sta ColorDest+1
		lda #0
		sta CharDest
		sta ColorDest
		#BasicOFF
		ldx #4
		ldy #0
CopyLoop	lda (WAC_LO),y
		sta (CharDest),y
		lda (ColorSrc),y
		sta (ColorDest),y
		iny
		bne CopyLoop
		dex
		beq CopyEnd
		inc WAC_HI
		inc CharDest+1
		inc ColorSrc+1
		inc ColorDest+1
		jmp CopyLoop
CopyEnd		#AllRomON
		rts		
		.endblock

