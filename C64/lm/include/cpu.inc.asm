; Commodore 64 Assembly Toolkit
; copyright (c) 2025 Roberto Ceccarelli
; https://strawberryfield.altervista.org
; Released under GNU Affero GPL 3.0
;
; 6510 CPU PORT

CPU_DDR		= $00		; Processor port data direction register. 
				; Bit #x: 0 = Bit #x in processor port can only be read; 
				; 1 = Bit #x in processor port can be read and written.
				; Default: $2F, %00101111.
				
CPU_PORT	= $01		; Processor port. 
				; Bits #0-#2: Configuration for memory areas $A000-$BFFF, $D000-$DFFF and $E000-$FFFF. Values:
				;     %x00: RAM visible in all three areas.
				;     %x01: RAM visible at $A000-$BFFF and $E000-$FFFF.
				;     %x10: RAM visible at $A000-$BFFF; KERNAL ROM visible at $E000-$FFFF.
				;     %x11: BASIC ROM visible at $A000-$BFFF; KERNAL ROM visible at $E000-$FFFF.
				;     %0xx: Character ROM visible at $D000-$DFFF. (Except for the value %000, see above.)
				;     %1xx: I/O area visible at $D000-$DFFF. (Except for the value %100, see above.)
				; Bit #3: Datasette output signal level.
				; Bit #4: Datasette button status; 0 = One or more of PLAY, RECORD, F.FWD or REW pressed; 1 = No button is pressed.
				; Bit #5: Datasette motor control; 0 = On; 1 = Off.
				; Default: $37, %00110111.

;-----------------
; macros

AllRomON	.macro
		lda #%00110111
		sta CPU_PORT
		.endmacro
		
KernalOFF	.macro
		lda #%00110101
		sta CPU_PORT	
		.endmacro

BasicOFF	.macro
		lda #%00110110
		sta CPU_PORT	; disattiva ROM Basic
		.endmacro