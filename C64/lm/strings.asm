; Adventure Player 2026
; copyright (c) 2026 Roberto Ceccarelli
; https://strawberryfield.altervista.org
; Released under GNU Affero GPL 3.0
;
; routines for custom string management

;---------------------
; working code

chr_cursor_down	= $11
chr_return	= $0d
chr_formfeed	= $0c
chr_clearscreen = $93

Strings		.block

WrapAt		= 40
TmpPtr		= $fa    
WordLen		= $fc
StringPtr	= $fe			; final pointer to the string

GetIndexPtr	#SearchWord		; puntatore alla tabella

GetStringPtr	#SearchByte		; identificatore della stringa
		#BasicOFF
		txa
		ldy #0
		clc
		sbc (WAC_LO),y
		bcc StringExists	; controllo se l'indice esite
		#AllRomON
		ldx #Error_BAD_SUBSCRIPT
		pla 
		pla 
		jmp PrintError
		
StringExists	inx
		txa
		asl
		tay
		lda (WAC_LO),y
		sta StringPtr
		iny
		lda (WAC_LO),y
		sta StringPtr+1
		rts

wait_page	ldx #24
		ldy #34
		jsr SetCursor
		lda #<more_str
		sta FAC2
		lda #>more_str
		sta FAC2+1
		ldy #0
wait_page_loop	lda (FAC2),y
		beq wait_char
		jsr CHROUT
		iny
		jmp wait_page_loop
wait_char	jsr GETIN
		beq wait_char
		rts

print_page	jsr wait_page
		lda #chr_clearscreen
		jmp print_and_go

PrintLine	jsr print
		lda #chr_return
		jmp CHROUT
		
; ---------------
; main print loop
Print		jsr GetIndexPtr		
Print_loop	ldy #0
		lda (StringPtr),y		
		beq print_end		; null terminated
		cmp #chr_formfeed       ; forced newpage
		beq print_page
				
print_not_ff	cmp #chr_return		; is forced newline?
		bne print_not_nl 
print_and_go	jsr CHROUT
		jmp print_advance

print_not_nl	cmp #' '
		bne pw_word
		lda CURCOL
		beq print_advance   	; Se siamo a colonna 0, scarta lo spazio 
		cmp #WrapAt-1
		bne print_space
		lda #chr_return
		jmp print_space_do
print_space	lda #' '
print_space_do	jsr CHROUT
		
print_advance	inc StringPtr		
		bne print_loop		; if no more than 255 chars
		inc StringPtr+1
		jmp print_loop
		
print_end	#AllRomON
		rts

; ---------- gestione parola ----------
pw_word		lda StringPtr		; scan lunghezza parola
		sta TmpPtr
		lda StringPtr+1
		sta TmpPtr+1
		lda #0
		sta WordLen

pw_scan		ldy #0
		lda (TmpPtr),y
		beq pw_scan_done
		cmp #' '
		beq pw_scan_done
		cmp #chr_return
		beq pw_scan_done
		cmp #chr_formfeed
		beq pw_scan_done
		inc WordLen
		inc TmpPtr
		bne pw_scan
		inc TmpPtr+1
		jmp pw_scan

pw_scan_done	lda CURCOL
		beq pw_print_word		; Se siamo già a colonna 0, non forzare l'a capo (evita riga vuota)
		clc
		adc WordLen
		cmp #WrapAt+1			; ci sta nella riga?
		bcc pw_print_word
		lda #chr_return			; no → vai a capo prima
		jsr CHROUT

pw_print_word	ldy #0
		lda (StringPtr),y
		beq print_end
		cmp #' '
		beq Print_loop
		cmp #chr_return
		beq jmp_print_loop
		cmp #chr_formfeed
		beq jmp_print_loop
		jsr CHROUT
		lda CURCOL
		beq print_advance
		cmp #WrapAt
		beq disable_link		; if kernal creates a logical line delete it
		inc StringPtr
		bne pw_print_word
		inc StringPtr+1
		jmp pw_print_word
jmp_print_loop	jmp Print_loop
		
disable_link	lda #WrapAt-1
		sta CurColLen			; reset line length
		lda #0
		sta CURCOL			; reset column
		ldx CURROW
		lda RowLinks,x
		ora #$80
		sta RowLinks,x			; reset link
		jsr SetRowPointer
		jmp print_advance

; ----------------
; Prints a single line of text
PrintSingle	jsr Common.GetWordToFE
		#BasicOFF
		jmp Print_loop

; ----------------
; Copy a single line of text
CopySingle	jsr Common.GetWordToFE
		#BasicOFF
		jsr calcLength
		jmp doCopyString
		
; ----------------
; lenght of the string (if <255)
GetLength	jsr GetIndexPtr
calcLength	ldy #0
len_cycle	lda (StringPtr),y
		beq len_scan_done
		iny
		bne len_cycle
len_scan_done	#AllRomON
		tya
len_rts		rts

; ----------------
; copy up to 255 chars to a string	
CopyString	jsr GetLength
doCopyString	cmp #0
		beq len_rts
		sta $02
		jsr Common.GetStringVar
		lda $02
		jsr AlocateStringFAC
		ldy #0				; copy descriptor to var
		lda $61
		sta (CurVarPtr),y
		iny
		lda $62
		sta (CurVarPtr),y
		iny
		lda $63
		sta (CurVarPtr),y
		#BasicOFF			; copy text to allocated area
		ldy $02
copy_loop	dey
		lda (StringPtr),y
		sta ($62),y
		cpy #0
		bne copy_loop
		ldy $02
		jmp len_scan_done
		
; -----------------
; Normalize case
Normalize_Case	jsr Common.GetStringVar	
		ldy #0
		lda (CurVarPtr),y
		beq normalize_end
		sta WordLen
		iny
		lda (CurVarPtr),y
		sta StringPtr
		iny 
		lda (CurVarPtr),y
		sta StringPtr+1
		ldy #0
normalize_loop	lda (StringPtr),y
		tax
		and #%11100000			; first area of uppercase
		cmp #$60
		bne norm_try192
		txa
		and #%11011111
		tax
		jmp norm_write
norm_try192	cmp #$c0
		bne norm_write
		txa
		and #$7f
		tax
norm_write	txa
		sta (StringPtr),y
		iny
		cpy WordLen
		bne normalize_loop
normalize_end	rts	

;-------------------------
; save string expr to $fe
CopyStrExpr	ldy #0
		lda (FACMO),y
		beq normalize_end	
		sta WordLen
		iny
		lda (FACMO),y
		sta StringPtr
		tax
		iny
		lda (FACMO),y
		sta StringPtr+1
		tay
		txa
		jmp StringStackClean
		
; ------------------------
; copy var to memory
CopyVarToMem	jsr Common.GetString
		jsr CopyStrExpr
		#SearchWord
		lda#0
		ldy WordLen
		sta (WAC_LO),y
copyvarloop	dey
		lda (StringPtr),y
		sta (WAC_LO),y
		cpy #0
		bne copyvarloop
		rts
		
; ------------------------
; Search for char
; to use with USR function

Search_Char	jsr Common.check_type_str 
		jsr CopyStrExpr
		#SearchByte
		stx $02				; temporary save
		ldy #0
search_loop	lda (StringPtr),y
		cmp $02
		beq search_found
		iny 
		cpy WordLen
		bne search_loop
		ldy #$ff
search_found	iny
		tya
		jmp ACCtoDecimal

more_str	.text "[...]"
		.byte 0
		.endblock
		

