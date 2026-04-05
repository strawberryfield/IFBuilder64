REM Adventure Player 2026
REM copyright (c) 2026 Roberto Ceccarelli
REM https://strawberryfield.altervista.org
REM Released under GNU Affero GPL 3.0
REM
REM --- main loader ---
'
{alpha:alt}
'
{uses:include/colors_poke_consts.bas}
{uses:include/sys_registers.bas}
{uses:lm/lm.def.bas}
{uses:common.inc.bas}
'
REM
REM loader
REM
	{uses:bin/latinorum.it.mem.bas}
	if ld=1 then ld=ld+1: gosub {:lang}
{ifdef:sd2iec}
	if ld=2 then ld=ld+1: gosub {:intro1}
	if ld=3 then ld=ld+1: load "lm",8,1
{else}
	if ld=2 then ld=ld+1: gosub {:intro1}: load "star turbo.52992",8,1
	if ld=3 then ld=ld+1: sys 52992: load "lm",8,1
{endif}
	if ld=4 then ld=ld+1: gosub {:intro2}: load {var:GameName}+".in",8,1
	if ld=5 then ld=ld+1: load {var:GameName}+".cf",8,1
	if ld=6 then ld=ld+1: load "cover",8,1
	if ld=7 then ld=ld+1: gosub {:intro3}: load {var:GameName}+".dt",8,1
REM
REM load parser
REM
	gosub {:set_colors}: if l$="i" then {:load_parser_it}
	{usedef:set_cursor},12,12: print "{red}...just a moment"
	print spc(8) "{dark gray}the game is almost ready": goto {:load_parser_end}
{:load_parser_it}
	{usedef:set_cursor},11,12: print "{red}...solo un momento" 
	print spc(8) "{dark gray}il gioco e' quasi pronto"	
{:load_parser_end}
	{usedef:set_cursor},0,18: print "{light gray}load";CHR$(34);"parser";CHR$(34) ",8";
	POKE 631,13 :POKE 632,82 :POKE 633,213 :POKE 634,13 :POKE 198,4: {usedef:set_cursor},0,15: STOP
REM
REM Init
REM	
{:intro1}
	PRINT "{clear}": POKE 781,12 :POKE 782,10 :SYS 58636: PRINT "{red}The Strawberry Field"
	print spc(16) "{dark gray}";: {var:GameName}="latinorum."
	if L$=i$ then print "presenta": {var:GameName}={var:GameName}+"it": goto {:intro1_end}
	print "presents": {var:GameName}={var:GameName}+"en"
{:intro1_end}
	{uses:lm/lm.inc.bas}
{:wait_load}	
{ifdef:sd2iec}
	m2$=" 90s)"
{else}
	m2$=" 30s)"
{endif}
	if l$=i$ then {:wait_it}
	m1$="please wait while loading": m2$="(it takes about"+m2$: goto {:wait_common}
{:wait_it}
	m1$="per favore attendi il caricamento": m2$="(impiega circa"+m2$
{:wait_common}
	print "{gray}";: POKE 781,23: poke 782,39-len(m1$): SYS 58636: print m1$
	print spc(39-len(m2$));m2$;: return	
	
{:intro2}
	print "{dark gray}";: {usedef:clean_row}, 12:  {usedef:clean_row}, 13
	if l$=i$ then {usedef:set_cursor},8,12: print "un gioco di avventura di": goto {:intro2_end}
	{usedef:set_cursor},10,12: print "an adventure game by"
{:intro2_end}
	print spc(11) "{red}Roberto Ceccarelli"
	{usedef:copy_to_mem}, {var:GameName}, {usedef:game_name}: return
	
{:intro3}
	POKE {usedef:pp_bordercolor},{usedef:pp_color_blue} :POKE {usedef:pp_backcolor},{usedef:pp_color_white}: print "{clear}{switchenable}{uppercase}{switchdisable}"
	{usedef:copy_screen}, 40960: return
	
{:lang}	
	gosub {:set_colors}
	POKE 781,11 :POKE 782,14 :SYS 58636: PRINT "{dark gray}[{red}E{dark gray}]{black} English"
	POKE 781,13 :POKE 782,14 :SYS 58636: PRINT "{dark gray}[{red}I{dark gray}]{black} Italiano"
	i$="i": e$="e"
{:lang_loop}	
	GET L$: IF L$=i$ then return
	if L$=e$ THEN return
	goto {:lang_loop}
	
{:set_colors}	
	POKE {usedef:pp_bordercolor},{usedef:pp_color_lightgrey} :POKE {usedef:pp_backcolor},{usedef:pp_color_lightgrey}: PRINT "{clear}{switchenable}{lowercase}{switchdisable}": return
	