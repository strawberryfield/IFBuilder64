; Adventure Player 2026
; copyright (c) 2026 Roberto Ceccarelli
; https://strawberryfield.altervista.org
; Released under GNU Affero GPL 3.0
;
; assembly code for Adventure Player

.include "include/parser.inc.asm"
.include "include/kernal.inc.asm"
.include "include/memory.inc.asm"
.include "include/math.inc.asm"
.include "include/cpu.inc.asm"

;----------------------
* = $c000

; API entries
setupUSR		jmp x_setusr			; set pointer for USR function

PrintStringTable	jmp Strings.Print		; Stampa una stringa da una tabella
							; prende due parametri:
							; - indirizzo della tabella
							; - numero (0-based) della stringa

PrintStringSingle	jmp Strings.PrintSingle		; Stampa una singola stringa null-terminated
							; prende come parametro l'indirizzo della stringa
							
NormalizeCase		jmp Strings.Normalize_Case	; converts alpha chars to default set					
search_text		jmp Search.Search_Word		; search a string in a table
StringLength		jmp Strings.GetLength		; return the string length (up to 255) in accumulator
							; same parameters as PrintStringTable
CopyToVar		jmp Strings.CopyString		; copy a string to a var (third parameter)
							; first two parameters as PrintStringTable
							
poscursor		jmp TextMode.PosCursor 		; posizionamento cursore modo testo
cleanRow		jmp TextMode.CleanRow		; cancella una riga dello schermo

PeekByte		jmp Peek.PeekByte		; get a byte under the Basic ROM
PeekWord		jmp Peek.PeekWord		; get a word under the Basic ROM
PokeWord		jmp Peek.PokeWord		; poke a 16 bit value

WaitPage		jmp Strings.wait_page		; wait for a key pressed

PrintStringLine		jmp Strings.PrintLine		; same as PrintStringTable with a final CR added
CopySingleToVar		jmp Strings.CopySingle		; same as CopyToVar but with absolute address
CopyVarToMem		jmp Strings.CopyVarToMem	; copy a string to a location of memory

GetBit			jmp Peek.GetBit			; same as peekbyte with an and mask
SetBit			jmp Peek.SetBit			; applies an or mask to the byte
ResetBit		jmp Peek.ResetBit		; applies an and mask to the byte

ScanDB			jmp Peek.Scan			; scan from the address for the second parameter times
							; if equal to third parameter adds the index to the list
							
CopyScreen		jmp TextMode.CopyScreen		; Copy chars and color from memory
CopyMem			jmp MemCopy.CopyRAM		; copy area from first address until second address page
							; to target address
							
;---------------------
; working code
.include "common.asm"
.include "strings.asm"
.include "lm_text.asm"
.include "search.asm"
.include "peek.asm"
.include "memcopy.asm"

; set USR vector
x_setusr	lda #<Strings.Search_Char		
		sta usr_pointer
		lda #>Strings.Search_Char
		sta usr_pointer+1
		rts
