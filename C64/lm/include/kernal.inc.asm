; Commodore 64 Assembly Toolkit
; copyright (c) 2025 Roberto Ceccarelli
; https://strawberryfield.altervista.org
; Released under GNU Affero GPL 3.0
;
; kernal entry points
SETMSG  	= $FF90         ; set kernel message control flag
SECOND  	= $FF93         ; set secondary address after LISTEN
TKSA    	= $FF96         ; send secondary address after TALK
LISTEN  	= $FFB1         ; command serial bus device to LISTEN
TALK    	= $FFB4         ; command serial bus device to TALK
SETLFS  	= $FFBA         ; set logical file parameters
SETNAM  	= $FFBD         ; set filename
ACPTR   	= $FFA5         ; input byte from serial bus
CIOUT   	= $FFA8         ; output byte to serial bus
UNTLK   	= $FFAB         ; command serial bus device to UNTALK
UNLSN   	= $FFAE         ; command serial bus device to UNLISTEN
CHKIN   	= $FFC6         ; define input channel
CLRCHN  	= $FFCC         ; restore default devices
INPUT   	= $FFCF         ; input a character (official name CHRIN)
CHROUT  	= $FFD2         ; output a character
LOAD    	= $FFD5         ; load from device
SAVE    	= $FFD8         ; save to device
STOP    	= $FFE1         ; check the STOP key
GETIN   	= $FFE4         ; get a character

; screen utilities
SetCursor	= $e50c		; X register = row ,Y register = column
GetSetCursor	= $e50a		; X register = row ,Y register = column; carry 0 = read, carry 1 = write
CleanTextRow	= $e9ff		; Clear screen line. Input: X = Row number.
ClearScreen	= $E544		
HomeCursor	= $E566 
ScrollUp	= $E8EA		; Scroll complete screen upwards.
ScrollDown	= $E965		; Insert line before current line and scroll lower part of screen downwards.
SetRowPointer	= $E56C		; Set pointer at memory addresses $00D1-$00D2 to current line in screen memory

; locations
CURCOL  	= $D3		; Current screen column
CurColLen	= $D5		; Current logical line length
CURROW		= $D6		; Current screen row
CurRowPtr	= $D1		; Pointer to current screen row
RowLinks	= $D9		; links for logical lines
