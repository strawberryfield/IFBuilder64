# Adventure Player 2026
# copyright (c) 2026 Roberto Ceccarelli
# https://strawberryfield.altervista.org
# Released under GNU Affero GPL 3.0
#
# makefile

# programs
64tass = "C:\Commodore\64tass\64tass.exe"

# make all
allprg = lm.prg
.PHONY: all 
all: $(allprg)

# rules 
lm.prg: *.asm
	$(64tass) -a lm.asm -o lm.prg --list=lm.lst
	
.PHONY: lm	
lm: lm.prg	

## utils
.PHONY: clean
clean:
	rm $(allprg)
	
	