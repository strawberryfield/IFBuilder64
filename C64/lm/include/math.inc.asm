; Commodore 64 Assembly Toolkit
; copyright (c) 2025 Roberto Ceccarelli
; https://strawberryfield.altervista.org
; Released under GNU Affero GPL 3.0
;
; BASIC math entry points

; Floating Point Copies
CopyFVARtoFAC1		= $BBA2 ; Copy FVAR to FAC1; accumulator = low order, Yregister= high order address of FVAR.
CopyFVARtoFAC2		= $BA8C ; Copy FVAR to FAC2; accumulator = low order, Yregister= high order address of FVAR.
CopyFAC1toInd49		= $BBD0 ; Copy FAC1 to (49); (49) 
				; typically points to the data area for a variable descriptor when called from BASIC. 
CopyFAC1toXY		= $BBD4 ; Copy FAC1 to address specified by X-register (low order) and Y-register (high order). 
CopyFAC1to5C		= $BBC7 ; Copy FAC1 to 5C-60. 
CopyFAC1to57		= $BBCA ; Copy FAC1 to 57-5B.
CopyFAC2toFAC1		= $BBFC ; Copy FAC2 to FAC1
CopyFAC1toFAC2		= $BC0C ; Copy FAC1 to FAC2.

; Floating Point Addition and Subtraction
FAC1plusFAC2		= $B86A ; FAC1 = FAC2 + FAC1.
FAC1plusACC		= $BD7E ; FAC1 = FAC1 + accumulator 
				; (the 6502 accumulator with a value that you should set from $00-$09).
FAC1plusFVAR		= $B867 ; FAC1 = FVAR + FAC1; accumulator = low order, Y-register = high order address of FVAR.
FAC2minusFAC1		= $B853 ; FAC1 = FAC2 - FAC1.
FVARminusFAC1		= $B850 ; FAC1 = FVAR - FAC1; accumulator = low order, Y-register = high order address of FVAR.
FAC1plus0dot5		= $B849 ; FAC1 = FAC1 + 0.5

; Floating Point Multiplication and Division
FAC1mulFAC2 		= $BA2B ; FAC1 = FAC1 * FAC2.
FAC1mulFVAR 		= $BA28 ; FAC1 = FAC1 * FVAR; accumulator = low order, Yregister= high order address of FVAR.
FAC1mul10 		= $BAE2 ; FAC1 = FAC1 * 10.
FAC2divFAC1 		= $BB12 ; FAC1 = FAC2 / FAC1.
FVARdivFAC1 		= $BB0F ; FAC1 = FVAR / FAC1; accumulator = low order, Yregister= high order address of FVAR.
FAC1div10 		= $BAFE ; FAC1 = FAC1 / 10.
FAC2div2PI		= $BB07 ; divide FAC2 by 2*PI and leave the result in FAC1.

; Other Math and Logic Routines
MathRound		= $BC1B ; Round FAC1.
MathExponentiation	= $BF7B ; Exponentiation: FAC1 = FAC1 raised to the power FAC2.
MathSQR			= $BF71 ; SQR: FAC1 = SQR(FACl).
MathAND			= $AFE9 ; AND: FAC1 = FAC1 AND FAC2.
MathOR			= $AFE6 ; OR: FAC1 = FAC1 OR FAC2.
MathINT			= $BCCC ; INT: FAC1 = INT(FACl).
MathTwoComp		= $B947 ; Two's Comp: FAC1 = Two's Comp of FAC1.
MathSGN			= $BC39 ; SGN: FAC1 = SGN(FACl).
MathABS			= $BC58 ; ABS: FAC1 = ABS(FACl).
MathComparison		= $BC5B ; Comparison: FAC1 = FAC1 comparison to FVAR; 
				; accumulator = low order, Y-register = high order address of FVAR.
; Integer to Floating Point Conversions
WordToDecimal		= $BC44 ; Convert two-byte integer in 62 and 63 (of FAC1) to floating point in FAC1.
ACCtoDecimal		= $BC3C ; Convert accumulator to floating point in FAC1
AXtoOutput		= $BDCD ; Convert accumulator (low order) and X-register (high order) to floating point 
				; and then convert the floating point to an ASCII string 
				; and send this ASCII string to the current output device.
Yplus05toDecimal	= $B3A2 ; Convert Y-register to floating point in FAC1 plus0dot5

; ASCII Decimal Number to Floating Point
ASCIItoDecimal		= $BCF3 ; Convert ASCII decimal number (such as -23E+07) to floating point in FAC1

; Floating Point to ASCII
DecimalToASCII		= $BDDD ; Convert FAC1 floating point to ASCII string at 0100 
				; with accumulator (low order) and Y-register (high order) pointing to 0100 at exit.
; Floating Point to Fixed Point Integer
DecimalToInt32		= $BC9B ; Convert floating point FACl to fixed point four-byte integer in FAC1
DecimalToInt16		= $B1BF ; Convert floating point FACl to fixed point integer in the range —32,768 to 32,767 and store in 64 and 65.
DecimalToWord		= $B1D2 ; Convert numeric expression from floating point to fixed point integer in 64 and 65.
DecimalToYA		= $B1AA ; Convert floating point FACl to two-byte integer with low order in Y-register and high order in accumulator.
DecimalToX		= $B7A1 ; Convert floating point FACl to integer in the range 0-255 and leave integer in X-register.
ExprToX			= $B79E ; Evaluate expression and convert FACl floating point expression value to integer in the 0-255 range left in Xregister.

; Polinomial calculated functions
MathSIN			= $E26B
MathCOS			= $E264
MathTAN			= $E2B4
MathATN			= $E30E
MathEXP 		= $BFED
MathLOG			= $B9EA

; Memory locations
FAC2			= $69	; Floating point accumulator #2

; Floating point constants
fp_pi			= $AEAB ; Pi Value 
fp_one			= $E376 ; 1
fp_half			= $bf11 ; 0.5
fp_SQR2HLF		= $B9D6 ; 1/sqr(2) = .707
fp_SQR2			= $B9DB ; sqr(2) = 1.41
fp_NEGHLF 		= $B9E0 ; -0.5
fp_Log2			= $B9E5 ; log(2) = .693147181
fp_ten			= $BAF9 ; 10
fp_pihalf		= $E2E0 ; pi/2
fp_twopi		= $E2E5 ; 2*PI

; Macros
DoubleFAC		.macro 
			inc FAC1
			.endmacro
			
HalfFAC			.macro
			dec FAC1
			.endmacro
			
fp_zero  		.macro
			.byte $00,$00,$00,$00,$00
			.endmacro



		