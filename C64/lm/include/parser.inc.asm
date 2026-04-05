; Commodore 64 Assembly Toolkit
; copyright (c) 2025 Roberto Ceccarelli
; https://strawberryfield.altervista.org
; Released under GNU Affero GPL 3.0
;
; BASIC parser entry points
SearchComma		= $AEFD ; check for comma
GetByte  		= $B79E ; get 8-bit parameter into X
GetFloat    		= $AD8A ; get float into FAC1
FloatToWord  		= $B7F7 ; convert to word in $14-$15 if needed
GetExpression		= $AD9E ; Get any expression.
				; It accepts any expression and sets the flag in location $0D 
				; to show whether it's a string ($0D=$FF) or a number ($0D = 0). 
PrintError		= $A437 ; print erroe message with number in X		
StringStackClean	= $B6DB ; CLEAN-UP TEMPORARY STRING DESCRIPTOR STACK		
VarFindCreate		= $B08B ; Find a variable and create it if needed
				; Exit Conditions: OD = $FF if string or $00 if numeric.
				; If OD = $00 then 0E = $80 if integer or $00 if floating point.
EndStatement		= $A8F8 ; MOVE TEXT POINTER TO END-OF-LINE OR END-OF-STATEMENT
				
; Memory locations
WAC_LO			= $14	; Word from FAC1
WAC_HI			= $15
FAC1			= $61	; Floating point accumulator #1
FACMO			= $64   ; String print Index
EXPR_TYPE		= $0d	; 0 = numeric, $ff = string
CurVarPtr		= $47	; pointer to current variable data
CurVarName		= $45	; two bytes with current variable name
				; $0045 bits #0-#6: First character of variable name.
				; $0046 bits #0-#6: Second character of variable name; $00 = Variable name consists of only one character.
				; $0045 bit #7 and $0046 bit #7:
				;    %00: Floating-point variable.
				;    %01: String variable.
				;    %10: FN function, created with DEF FN.
				;    %11: Integer variable.

usr_pointer		= $0311 ; pointer to USR function

; Error messages
Error_BAD_SUBSCRIPT		= $12
Error_BREAK			= $1E
Error_CANT_CONTINUE		= $1A
Error_DEVICE_NOT_PRESENT	= $05
Error_DIVISION_BY_ZERO		= $14
Error_FILE_DATA			= $18
Error_FILE_NOT_FOUND		= $04
Error_FILE_NOT_OPEN		= $03
Error_FILE_OPEN			= $02
Error_FORMULA_TOO_COMPLEX	= $19
Error_ILLEGAL_DEVICE_NUMBER	= $09
Error_ILLEGAL_DIRECT		= $15
Error_ILLEGAL_QUANTITY		= $0E
Error_LOAD			= $1D
Error_MISSING_FILENAME		= $08
Error_NEXT_WITHOUT_FOR		= $0A
Error_NOT_INPUT_FILE		= $06
Error_NOT_OUTPUT_FILE		= $07
Error_OUT_OF_DATA		= $0D
Error_OUT_OF_MEMORY		= $10
Error_OVERFLOW			= $0F
Error_REDIMD_ARRAY		= $13
Error_RETURN_WITHOUT_GOSUB	= $0C
Error_STRING_TOO_LONG		= $17
Error_SYNTAX			= $0B
Error_TOO_MANY_FILES		= $01
Error_TYPE_MISMATCH		= $16
Error_UNDEFD_FUNCTION		= $1B
Error_UNDEFD_STATEMENT		= $11
Error_VERIFY			= $1C


; Macros
SearchByte	.macro
		jsr SearchComma
		jsr GetByte
		.endmacro
		
SearchFloat	.macro
		jsr SearchComma
		jsr GetFloat
		.endmacro
		
SearchWord	.macro
		#SearchFloat
		jsr FloatToWord
		.endmacro
		
SearchExpr	.macro
		jsr SearchComma
		jsr GetExpression
		.endmacro
		
CleanStrStack	.macro
		lda FACMO
		ldy FACMO+1
		jsr StringStackClean
		.endmacro
		
SearchVar	.macro
		jsr SearchComma
		jsr VarFindCreate
		.endmacro
		