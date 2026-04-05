; Commodore 64 Assembly Toolkit
; copyright (c) 2025 Roberto Ceccarelli
; https://strawberryfield.altervista.org
; Released under GNU Affero GPL 3.0
;
; BASIC memory management

MoveMemory		= $A3BF ; This move can be used by your machine language routine as
				; long as the move is in an upward direction. It will also work
				; in a downward memory move as long as the source area and
				; the destination area do not overlap.
				
AllocateString		= $B4F4	; Allocate Space for String, Accumulator = length of string to be allocated.
				; Return: X-register = high-order byte of pointer, Yregister= low-order byte of pointer, 
				; and PLA to restore the length of the string in the accumulator.
AlocateStringFAC	= $B475 ; ALLOCATE AREA FOR STRING IN ACTIVE STRING LEAVE LENGTH IN 61 AND POINTER IN (62)

CopyStringFromFAC	= $B67A ; Copy String Pointed to by Descriptor to Allocated Area (35)
CopyStringFrom22	= $B68C ; Copy String Pointed at (22) to allocated area (35), Accumulator = length


; Memory locations
MemMoveStart		= $5F	; Pointer to start area to meve
MemMoveEnd		= $5A	; Pointer to end area to meve (plus 1)
MemMoveDest		= $58	; pointer to the end of the destination move area plus one.
StringDest		= $35	; Pointer to allocated area
StringSrc		= $22	; Pointer to source string
