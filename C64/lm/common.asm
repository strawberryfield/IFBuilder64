; Adventure Player 2026
; copyright (c) 2026 Roberto Ceccarelli
; https://strawberryfield.altervista.org
; Released under GNU Affero GPL 3.0
;
; common helpers
;---------------------
; working code



Common		.block

GetNumVar	#SearchVar
check_type_num	lda #0
		cmp EXPR_TYPE		; is a num var?
		bne var_mismatch
		rts
var_mismatch	ldx Error_TYPE_MISMATCH
		jmp PrintError

GetStringVar	#SearchVar
check_type_str	lda #$ff
		cmp EXPR_TYPE		; is a string var?
		bne var_mismatch
		rts
		
GetWordToFE	#SearchWord
		lda WAC_LO
		sta $fe
		lda WAC_HI
		sta $ff
		rts

GetString	#SearchExpr
		jmp check_type_str
		
		.endblock
		

