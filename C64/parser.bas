REM Adventure Player 2026
REM copyright (c) 2026 Roberto Ceccarelli
REM https://strawberryfield.altervista.org
REM Released under GNU Affero GPL 3.0
REM
REM --- parser module ---
REM
'
{alpha:alt}
'
{uses:include/colors_poke_consts.bas}
{uses:include/sys_registers.bas}
{uses:lm/lm.def.bas}
{uses:include/common.inc.bas}
{uses:include/config.inc.bas}
{uses:custom/defs.bas}
'
{def:kbdbuf_clear=POKE 198,0}
'
' variables
{var:CurrentLocation=L}
{var:CurrentLocDBAddr=RD}
{var:CurrentVerb=V$}
{var:VerbId=V}
{var:CurrentAction=A}
{var:CurrentObject=O$}
{var:ObjectId=O}
{var:DoorId=D}
{var:Message=M}
{var:Caller=C}
{var:ListItemColor=C$}
{var:EmptyStr=E$}
{var:AvailableUndos=SA}
{var:UndoPointer=SP}
{var:UndoRequest=SR}
{var:Recording=RE}
{var:Yes=Y$}
'
' costants
{var:ColorText=CT$}
{var:ColorBack=CB$}
{var:ColorInput=CI$}
{var:ColorObject=CO$}
{var:ColorDir=CD$}
'
REM ------
REM loader
REM ------
	if ld=9 then {var:AvailableUndos}=Z: goto {:start}
	if ld=0 then {:init}
	{usedef:print_text}, {var:introaddr}: {usedef:wait_page}: goto {:start}
	
REM -------------------
REM crucial Subroutines
REM -------------------	
{:input}
	k$={var:EmptyStr}: input k$: {usedef:to_lower}, K$: return
	
{:save_undo}
	{var:UndoPointer}={var:UndoPointer}+U: if {var:UndoPointer} > 3 then {var:UndoPointer}=Z
	if {var:AvailableUndos} < 4 then {var:AvailableUndos}={var:AvailableUndos}+U
	{usedef:copy_status}, {var:DBbottomAddress}, {var:DBtopAddress}, u({var:UndoPointer}): {var:UndoRequest}=Z: return
	
{:check_visible_inside}
	' ciclo su tutti gli oggetti che ho eventualmente trovato
	if j > j1 then return
	if peek({var:itemsinitemdbaddr}+{var:ObjectId}) = o(j) then k=U: return
	if (peek({var:itemsoptionsdbaddr}+o(j)) and 2) =Z then j=j+U: goto {:check_visible_inside}
	' non era contenuto qui, ma controllo se all'interno ci sono altri contenitori ed eventualmente li aggiungo alla lista
	for j2=U to {var:NumPortable}-U
	if peek({var:itemsinitemdbaddr}+j2) = o(j) then if (peek({var:itemsoptionsdbaddr}+j2) and 2) >Z then j1=j1+U: o(j1)=j2
	next: j=j+U: goto {:check_visible_inside}

{:print_list}
	nd=nd-U: if pos({$ff}) + len(i$(nd)) >37 then print
	if pos({$ff})>Z then print " ";
	print {var:ListItemColor}; i$(nd); {var:ColorText};: if nd>U then print ",";: goto {:print_list}
	if nd=U then print " ";: {usedef:print_string}, {var:messagesaddr}, 23: goto {:print_list}
	print ".";: return

{:print_using}
	if tc<W then print "(";: {usedef:print_string}, {var:messagesaddr}, 42: print" "{var:ColorObject};: {usedef:print_string}, {var:itemsaddr}, tc: print {var:ColorText}")"
	return
	
REM ---------
REM main loop
REM ---------	
{:start}
	print {var:ColorText} "{clear}";: {var:CurrentLocation}=peek({var:curroomdbaddr}): {usedef:peek_byte}, {var:itemsaddr}, {var:NumObjects}: {usedef:peek_byte}, {var:verbsaddr}, {var:NumVerbs}
{:show_room}
	if {var:UndoRequest}>0 then gosub {:save_undo}
	poke {var:curroomdbaddr},{var:CurrentLocation}
	{uses:custom/pre_going.bas}
	{usedef:print_string}, {var:roomsdescaddr}, {var:CurrentLocation}: tl = {var:CurrentLocation}: gosub {:inventory}: if nd>Z then print: {usedef:print_string}, {var:messagesaddr}, 21: {var:ListItemColor}={var:ColorObject}: gosub {:print_list}
{:show_exits}	
	{var:CurrentLocDBAddr} = {var:roomsdbaddr}+{var:CurrentLocation}*6: nd=Z
	for j=5 to Z step -U: ta= peek({var:CurrentLocDBAddr}+j): if ta < W then {usedef:string_copy}, {var:dirsaddr}, j, k$: i$(nd)=k$: nd= nd+U
	next: print: if nd=Z then {var:Message}=54: goto {:show_message}
	{usedef:print_string}, {var:messagesaddr}, 22: {var:ListItemColor}={var:ColorDir}: gosub {:print_list}
	
{:main_loop}
	print {var:ColorInput}"{down}": gosub {:input}: print {var:ColorText};: if k$={var:EmptyStr} then {:main_loop}
	if {var:Recording}>Z then print#8, k$
	{var:CurrentVerb}=k$: {var:CurrentObject}={var:EmptyStr}: p=usr(k$),32: if p>Z then {var:CurrentVerb}=left$(k$,p-U): {var:CurrentObject}=mid$(k$,p+U)
	{usedef:search_word}, {var:verbstable}, {var:CurrentVerb}: {var:VerbId}={usedef:sys_get_a}: {var:CurrentAction}=W
	if {var:VerbId}<W then {usedef:peek_byte}, {var:verbsdbaddr}+{var:VerbId}, {var:CurrentAction}

	if {var:CurrentAction} < 6 then {:go}
	if {var:CurrentAction} = 6 then {:act_go}
	' search for portable object
	{var:ObjectId}=W: {var:DoorId}=W: if {var:CurrentAction} > 14 then {:action_select}
	if {var:CurrentObject}<>{var:EmptyStr} then {:search_object}
	if {var:CurrentAction} = 7 then {:act_look}
	print {var:ColorObject};: {usedef:print_string}, {var:messagesaddr}, 51: input {var:CurrentObject}: print {var:ColorText};
	
{:search_object}	
	{usedef:search_word}, {var:itemstable}, {var:CurrentObject}: {var:ObjectId}={usedef:sys_get_a}: if {var:ObjectId}<W then {:action_select}
	' search for scenery
	{usedef:peek_byte}, {var:rooms1stitemdbaddr}+{var:CurrentLocation}, ta: if ta=W then {:try_search_door}
	{usedef:peek_word}, {var:itemsaddr} +2 +ta+ta, tb: {usedef:search_word}, tb, {var:CurrentObject}: {var:ObjectId}={usedef:sys_get_a}
	if {var:ObjectId}<W then {var:ObjectId}={var:ObjectId}+ta: goto {:action_select}
	' search for door
{:try_search_door}
	{usedef:peek_byte}, {var:doorscountdbaddr}+{var:CurrentLocation}, ta: if ta<W then gosub {:search_door}
	
{:action_select}	
	on {var:CurrentAction}-6 goto {:act_look}, {:act_take}, {:act_drop}, {:act_open}, {:act_close}, {:act_switchon}, {:act_switchoff}, {:act_insert}, {:act_inv}, {:act_xyzzy}
	on {var:CurrentAction}-16 goto {:act_help}, {:act_cmds}, {:act_save}, {:act_load}, {:recall_undo}, {:endmain}, {:about}, {:act_record}, {:show_exits}  
	{usedef:print_line}, {var:messagesaddr}, 13: {var:Message}=29: goto {:show_message}
{:endmain}
	if {var:Recording}>Z then close 8
	END
	
REM ----------------
REM Actions handling
REM ----------------
REM
REM go
REM
{:go}
	ta = peek({var:CurrentLocDBAddr}+{var:CurrentAction}): if ta=W then {usedef:print_string}, {var:messagesaddr}, 9: goto {:show_exits}
	{var:CurrentLocation} = ta: {var:UndoRequest}=U: goto {:show_room}

{:act_go}
	if {var:CurrentObject}={var:EmptyStr} then print {var:ColorDir};: {usedef:print_string}, {var:messagesaddr}, 50: gosub {:input}: {var:CurrentObject}=k$: print {var:ColorText};
	if {var:CurrentObject}={var:EmptyStr} then {var:Message}=15: goto {:show_message}
	{usedef:search_word}, {var:dirstable}, {var:CurrentObject}: {var:ObjectId}={usedef:sys_get_a}: if {var:ObjectId} < 6 then {var:CurrentAction} = {var:ObjectId}: goto {:go}
	{var:Message}=30
{:show_message}
	{usedef:print_string}, {var:messagesaddr}, {var:Message}: goto {:main_loop}
{:show_user_message}
	{usedef:print_string}, {var:usrmsgaddr}, {var:Message}: goto {:main_loop}

REM
REM look
REM	
{:act_look}
	if {var:CurrentObject}={var:EmptyStr} then {:show_room}
	{uses:custom/pre_examine.bas}
	{var:Caller}=1: goto {:check_visible}
{:act_look1}
	if {var:DoorId}<W then {usedef:print_string}, {var:doorsdescaddr}, {var:DoorId}: goto {:main_loop}
	tb= peek({var:itemsoptionsdbaddr}+{var:ObjectId}): if (tb and 2) = Z then {:look_normal}
	{usedef:string_legth}, {var:itemsopendescaddr}, {var:ObjectId}: if {usedef:sys_get_a}=Z then {:look_normal}
	{usedef:print_string}, {var:itemsopendescaddr}, {var:ObjectId}: if (tb and 64) then gosub {:print_item_content} 
	goto {:look_end}
{:look_normal}	
	{usedef:print_string}, {var:itemsdescaddr}, {var:ObjectId}
	if (tb and 66) =66 then gosub {:print_item_content}
{:look_end}
	if (tb and 32) =Z then {:main_loop}
	{var:Message}=33: if (tb and 16)=Z then {var:Message}=34
	print: goto {:show_message}

REM
REM inventory
REM		
{:act_inv}
	tl=0: gosub {:inventory}: if nd>Z then {usedef:print_string}, {var:messagesaddr}, 25: {var:ListItemColor}={var:ColorObject}: gosub {:print_list}: goto {:main_loop}
	{var:Message}=26: goto {:show_message}

REM
REM take drop 
REM	
{:act_take}
	tl={var:CurrentLocation}: goto {:check_take_drop}
{:act_drop}
	tl=0: goto {:check_take_drop}

REM
REM insert
REM	
{:act_insert}
	{var:Caller}=5: goto {:check_visible}
{:act_insert1}	
	if (peek({var:itemsoptionsdbaddr}+{var:ObjectId}) and 1) >0 then {var:Message}=4: goto {:show_message}
	{uses:custom/pre_insert.bas}
	' ottengo la lista di tutti gli oggetti aperti del player e della stanza corrente
	nd = -U: for j=U to {var:NumPortable}-U: if j = {var:ObjectId} then next
	ta = peek({var:itemsdbaddr}+j): if ta=Z or ta={var:CurrentLocation} then if (peek({var:itemsoptionsdbaddr}+j) and 66) = 66 then nd=nd+U: o(nd)=j
	next: {usedef:peek_byte}, {var:rooms1stitemdbaddr}+{var:CurrentLocation}, ta: if ta = W then {:skip_scenery}
	' search for containers in local sceneries
	{usedef:peek_byte}, {var:roomsitemscountdbaddr}+{var:CurrentLocation}, tb: for j=ta to ta+tb-U
	if (peek({var:itemsoptionsdbaddr}+j) and 66) = 66 then nd=nd+U: o(nd)=j
	next
{:skip_scenery}	
	k=Z: if nd >=Z then j=Z: gosub {:check_container_inside}
	if nd<Z then {var:Message}=38: goto {:show_message}
	if nd>Z then {:select_insert}
	j = nd
{:do_insert}	
	gosub {:save_undo}: poke {var:itemsdbaddr}+{var:ObjectId}, W: poke {var:itemsinitemdbaddr}+{var:ObjectId}, o(j): poke {var:currentloaddbaddr}, peek({var:currentloaddbaddr})-U 
	{usedef:print_string}, {var:messagesaddr}, 39: print " ";{var:ColorObject};: {usedef:print_string}, {var:itemsaddr}, o(j): print {var:ColorText};".";: goto {:main_loop}
	
{:select_insert}
	{usedef:print_string}, {var:messagesaddr}, 40
	for j=Z to nd: {usedef:string_copy}, {var:itemsaddr}, o(j), k$: i$(j)=k$: next
	tc=nd: nd = nd+U: {var:ListItemColor}={var:ColorObject}: gosub {:print_list}
	print {var:ColorObject}"{down}": gosub {:input}: print {var:ColorText};: if k$={var:EmptyStr} then {:main_loop}
	j=-U: for j1=Z to tc: if i$(j1)=k$ then j=j1
	next: if j=-U then {var:Message}=12: goto {:show_message}
	goto {:do_insert}
	
{:check_container_inside}
	' ciclo su tutti gli oggetti che ho eventualmente trovato
	if j > nd then return
	' controllo se all'interno ci sono altri contenitori ed eventualmente li aggiungo alla lista
	for j2=U to {var:NumPortable}-U
	if peek({var:itemsinitemdbaddr}+j2) = o(j) then if (peek({var:itemsoptionsdbaddr}+j2) and 66) = 66 then nd=nd+U: o(nd)=j2
	next: j=j+U: goto {:check_container_inside}
	
REM
REM open close
REM	
{:act_open}
	tl=4: goto {:check_open_close}
{:act_close}
	tl=8: goto {:check_open_close}

REM
REM switch
REM
{:act_switchon}
	tl=Z: goto {:check_switch}
{:act_switchoff}
	tl=U: goto {:check_switch}

REM
REM help and magic
REM	
{:act_xyzzy}
	{var:Message}=18: goto {:show_message}
{:act_help}
	{usedef:print_line}, {var:messagesaddr}, 19: {usedef:print_line}, {var:messagesaddr}, 28: {var:Message}=29: goto {:show_message}	
{:act_cmds}
	{usedef:print_string}, {var:messagesaddr}, 20: print: ta=Z
	for j=Z to {var:NumVerbs}-U: print tab(ta*12+2);: {usedef:print_string}, {var:verbsaddr}, j: ta=ta+U: if ta=3 then ta=Z: print
	next: print: goto {:main_loop}

REM
REM load save
REM	
{:act_save}
	gosub {:ask_file}: if {var:CurrentObject}={var:EmptyStr} then {:main_loop}
	SYS 57812 ("@0:"+{var:CurrentObject}),8,1: {usedef:poke_word}, 193, {var:DBbottomAddress}: {usedef:poke_word}, 174, {var:DBtopAddress}: SYS 62957	
	{var:Message}=24: goto {:show_message}
{:act_load}
	gosub {:ask_file}: if {var:CurrentObject}={var:EmptyStr} then {:main_loop}
	open 1,8,15: open 2,8,2,{var:CurrentObject}+",p,r": INPUT#1, EN, EM$, ET, ES: CLOSE 1: close 2
	if en<>Z then print em$: goto {:main_loop}
	ld=9: load {var:CurrentObject},8,1
{:act_record}
	gosub {:ask_file}: if {var:CurrentObject}={var:EmptyStr} then {:main_loop}
	{var:Message}=49: open 8,8,8,"@0:"+{var:CurrentObject}+",s,w": if st=Z then {var:Recording}=U: {var:Message}=24
	goto {:show_message}
{:ask_file}
	if {var:CurrentObject}={var:EmptyStr} then {usedef:print_string}, {var:messagesaddr}, 17: input {var:CurrentObject}
	return

REM ---------------
REM End of the Game
REM ---------------

{:end_loose}
	{var:Message}=44: goto {:end_common}
{:end_win}
	{var:Message}=43
{:end_common}
	close 8: print {var:ColorText}: {usedef:print_string}, {var:messagesaddr}, {var:Message}: {usedef:print_string}, {var:messagesaddr}, 45: {usedef:kbdbuf_clear}: print "{down}"
{:end_loop}	
	GET K$: IF K$<"0" OR K$>"4" THEN {:end_loop}
	ON VAL(K$) GOTO {:recall_undo}, {:restart}, {:act_load}: end
	
REM -----------
REM Subroutines
REM -----------

REM
REM Search door
REM
{:search_door}
	ta= {var:roomsDoorsdbaddr}+{var:CurrentLocation}*6: for j=0 to 5: {usedef:peek_byte}, ta+j, {var:DoorId}: if {var:DoorId}=W then {:door_loop}
	{usedef:string_copy}, {var:doorsaddr}, {var:DoorId}, k$: if k$={var:CurrentObject} then return
{:door_loop}
	next: return
	
REM
REM Check take drop
REM
{:check_take_drop}
	{uses:custom/pre_takedrop.bas}
	{var:Caller}=3: goto {:check_visible}
{:check_take_drop1}
	if {var:DoorId}=W then {:check_take_drop2}
	{var:Message}=4: if tl=Z then {var:Message}=2
	goto {:show_message}
{:check_take_drop2}
	if tl>Z then {:check_take}
	if ta>0 then {var:Message}=2: goto {:show_message}
	goto {:do_take_drop}
{:check_take}	
	if ta=Z then {var:Message}=1: goto {:show_message}
	tb= (peek({var:itemsoptionsdbaddr}+{var:ObjectId}) and 1): if tb= 0 then {:do_take_drop}
	if ta=tl then {var:Message}=4: goto {:show_message}
	{var:Message}=0: goto {:show_message}
{:do_take_drop}
	gosub {:save_undo}: {var:Message}=24: tc =peek({var:currentloaddbaddr}) 
	if tl=Z then poke {var:itemsdbaddr}+{var:ObjectId},{var:CurrentLocation}: poke {var:itemsinitemdbaddr}+{var:ObjectId}, W: poke {var:currentloaddbaddr}, tc-U: goto {:show_message}
	if tc=peek({var:maxloaddbaddr}) then {var:Message}=47: goto {:show_message}
	poke {var:currentloaddbaddr}, tc+U: poke {var:itemsinitemdbaddr}+{var:ObjectId}, W: poke {var:itemsdbaddr}+{var:ObjectId}, Z: goto {:show_message}

REM
REM Check open close
REM
{:check_open_close}
	{uses:custom/pre_openclose.bas}
	{var:Caller}=2: goto {:check_visible}
{:check_open_close1}
	if {var:DoorId}<W then {:door_open_close}
	tb = peek({var:itemsoptionsdbaddr}+{var:ObjectId}): if (tb and tl) >Z then {:check_open_status}
	{var:Message}=8: if tl=4 then {var:Message}=7
	goto {:show_message}
{:check_open_status}
	if tl=8 then {:check_close_status}
	if (tb and 2) <>Z then {var:Message}=10: goto {:show_message}
	' controllo se necessita di un oggetto per lo sblocco
	tc = peek({var:itemsunlockdbaddr}+{var:ObjectId}): if tc=W then {:do_open}
	if peek({var:itemsdbaddr}+tc) > Z then {var:Message}=27: goto {:show_message}
{:do_open}	
	gosub {:save_undo}: poke {var:itemsoptionsdbaddr}+{var:ObjectId}, tb or 2: gosub {:print_using}
	{uses:custom/post_open.bas}
	{usedef:string_legth}, {var:itemsopendescaddr}, {var:ObjectId}: if {usedef:sys_get_a}=Z then {usedef:print_string}, {var:messagesaddr}, 24: goto {:open_end}
	{usedef:print_string}, {var:itemsopendescaddr}, {var:ObjectId}
{:open_end}
	if (tb and 64) > Z then gosub {:print_item_content}
	goto {:main_loop}
{:check_close_status}
	if (tb and 2) =Z then {var:Message}=11: goto {:show_message}
	' sanity check
	tc = peek({var:itemsunlockdbaddr}+{var:ObjectId}): if tc<W then {var:Caller}=1: goto {:confirm_close}
{:do_close}
	gosub {:save_undo}: poke {var:itemsoptionsdbaddr}+{var:ObjectId}, tb and 253: {var:Message}=24: goto {:show_message}

{:confirm_close}
	if peek({var:itemsdbaddr}+tc) = Z then {:confirm_close_ok}
	{usedef:print_string}, {var:messagesaddr}, 52: gosub {:input}: j=len(k$): if j=Z then {:main_loop}
	if left$({var:Yes},j)<>k$ then {:main_loop}
{:confirm_close_ok}
	on {var:Caller} goto {:do_close}, {:do_close_door}
	
REM
REM Door open close
REM
{:door_open_close}
	tb = peek({var:doorsoptionsdbaddr}+{var:DoorId}): if tl=8 then {:door_close_status}
	if tb >Z then {var:Message}=36: goto {:show_message}
	{uses:custom/pre_open_door.bas}
	' controllo se necessita di un oggetto per lo sblocco
	tc = peek({var:doorsunlockdbaddr}+{var:DoorId}): if tc=W then {:do_open_door}
	if peek({var:itemsdbaddr}+tc) = Z then {:do_open_door}
	{usedef:string_legth}, {var:doorsdenyopenaddr}, {var:DoorId}: if {usedef:sys_get_a}=Z then {var:Message}=27: goto {:show_message}
	{usedef:print_string}, {var:doorsdenyopenaddr}, {var:DoorId}: goto {:main_loop}
{:do_open_door}
	gosub {:print_using}
	gosub {:door_exits_loop}: gosub {:save_undo}: poke {var:roomsdbaddr} +{var:CurrentLocation}*6 +j1, tc: poke {var:roomsdbaddr} +tc*6 +j2, {var:CurrentLocation}
	{var:UndoRequest}=U: poke {var:doorsoptionsdbaddr}+{var:DoorId},1: goto {:show_exits}
{:door_close_status}
	if tb =Z then {var:Message}=37: goto {:show_message}
	' sanity check
	tc = peek({var:doorsunlockdbaddr}+{var:DoorId}): if tc<W then {var:Caller}=2: goto {:confirm_close}
{:do_close_door}
	gosub {:door_exits_loop}: gosub {:save_undo}: poke {var:roomsdbaddr} +{var:CurrentLocation}*6 +j1, W: poke {var:roomsdbaddr} +tc*6 +j2, W
	poke {var:doorsoptionsdbaddr}+{var:DoorId},0: goto {:show_exits}
	
{:door_exits_loop}
	{usedef:print_string}, {var:messagesaddr}, 24: ta = {var:doorsdbaddr}+{var:DoorId}*6: for j=Z to 5: {usedef:peek_byte}, ta+j, tb: if tb=W then {:end_door_loop}
	if tb = {var:CurrentLocation} then j1=j: goto {:end_door_loop}
	j2=j: tc=tb
{:end_door_loop}
	next: return
	
REM
REM Check switch
REM
{:check_switch}
	{var:Caller}=4: goto {:check_visible}
{:check_switch1}
	if {var:DoorId}<W then {:check_switch2}
	{uses:custom/pre_switch.bas}
	tb = peek({var:itemsoptionsdbaddr}+{var:ObjectId}): if (tb and 32) >Z then {:check_switch_status}
{:check_switch2}
	{var:Message}=31: if tl=U then {var:Message}=32
	goto {:show_message}
{:check_switch_status}
	if tl=U then {:check_switchoff_status}
	if (tb and 16) >Z then {var:Message}=33: goto {:show_message}
	' controllo se necessita di un oggetto per l'accensione
	tc = peek({var:itemsunlockdbaddr}+{var:ObjectId}): if tc=W then {:do_switchon}
	if peek({var:itemsdbaddr}+tc) > Z then {var:Message}=35: goto {:show_message}
{:do_switchon}	
	gosub {:save_undo}: {usedef:bit_set}, {var:itemsoptionsdbaddr}+{var:ObjectId}, 16 : {var:Message}=33: goto {:show_message}
{:check_switchoff_status}
	if (tb and 16) >Z then gosub {:save_undo}: {usedef:bit_reset}, {var:itemsoptionsdbaddr}+{var:ObjectId}, 16: {var:UndoRequest}=U 
	{var:Message}=34: goto {:show_message}
	
REM
REM Check visible
REM
{:check_visible}
	if {var:CurrentObject}={var:EmptyStr} then {var:Message}=3: goto {:show_message}
	if {var:DoorId}<W then {:check_visible_return} 
	if {var:ObjectId}=W then {usedef:print_line}, {var:messagesaddr}, 0: {var:Message}=28: goto {:show_message}
	ta = peek({var:itemsdbaddr}+{var:ObjectId}): if ta=Z or ta={var:CurrentLocation} then {:check_visible_return}
	' ottengo la lista di tutti gli oggetti aperti del player e della stanza corrente
	j1 = -U: for j=U to {var:NumPortable}-U: ta = peek({var:itemsdbaddr}+j) 
	if ta=Z or ta={var:CurrentLocation} then if (peek({var:itemsoptionsdbaddr}+j) and 2) >Z then j1=j1+U: o(j1)=j
	next: {usedef:peek_byte}, {var:rooms1stitemdbaddr}+{var:CurrentLocation}, ta: if ta = W then {:skip_scenery_check}
	' search for containers in local sceneries
	{usedef:peek_byte}, {var:roomsitemscountdbaddr}+{var:CurrentLocation}, tb: for j=ta to ta+tb-U
	if (peek({var:itemsoptionsdbaddr}+j) and 2) > Z then j1=j1+U: o(j1)=j
	next 
{:skip_scenery_check}
	k=Z: if j1 >=Z then j=Z: gosub {:check_visible_inside}
	if k=Z then {var:Message}=0: goto {:show_message}
{:check_visible_return}	
	on {var:Caller} goto {:act_look1}, {:check_open_close1}, {:check_take_drop1}, {:check_switch1}, {:act_insert1}

REM
REM inventory lists
REM	
{:print_item_content}
	tl = {var:ObjectId}: gosub {:item_inventory}: print: if nd>Z then {usedef:print_string}, {var:messagesaddr}, 5: {var:ListItemColor}={var:ColorObject}: goto {:print_list}
	{usedef:print_string}, {var:messagesaddr}, 14: return 
	
{:inventory}
	{usedef:scandb}, {var:itemsdbaddr}+U, {var:NumObjects}-U, tl: tb = peek({var:list_counter}): nd=Z: if tb=Z then return
	for j=Z to tb-U: ta=peek({var:list_data}+j)+U
	if (peek({var:itemsoptionsdbaddr}+ta) and U) = Z then {usedef:string_copy}, {var:itemsaddr}, ta, k$: i$(nd)=k$: nd= nd+U
	next: return

{:item_inventory}
	{usedef:scandb}, {var:itemsinitemdbaddr}+U, {var:NumObjects}-U, tl: nd = peek({var:list_counter})
	for j=Z to nd-U: ta=peek({var:list_data}+j)+U: {usedef:string_copy}, {var:itemsaddr}, ta, k$: i$(j)=k$
	next: return

REM ---------------
REM UNDO management
REM ---------------

{:recall_undo}
	if {var:AvailableUndos}=Z then {var:Message}=46: goto {:show_message}
	{usedef:copy_status}, u({var:UndoPointer}), u({var:UndoPointer}+U), {var:DBbottomAddress}: {var:AvailableUndos}={var:AvailableUndos}-U
	{var:UndoPointer}={var:UndoPointer}-U: if {var:UndoPointer} < Z then {var:UndoPointer}=3
	goto {:start}

REM -----
REM about
REM -----
{:about}
	open 2,8,2,{var:GameName}+".ab,s,r": if st <> Z then {:file_error}
	input#2,i$(0),i$(1),i$(2),i$(3),i$(4): if st <> Z then {:file_error}
	close 2: print "{down}";{var:ColorObject};i$(0): print {var:ColorDir};i$(1);{var:ColorText}
	{usedef:print_string}, {var:messagesaddr}, 48: print " ";i$(2)
	print {var:ColorDir};"{down}ID: ";i$(3); "SER ";i$(4);{var:ColorText}
	print "{down}Casasoft Adventure Player 2026"
	print {var:ColorDir};"Build: ";{usedef:__BuildDate};{var:ColorText}: goto {:main_loop}
{:file_error}
	close 2: {var:Message}=49: goto {:show_message}
REM ----
REM init
REM ----
{:init}
	Z=.: U=1: W=255: nd=Z: j=Z: ta=Z: tb=Z: tc=Z: tl=Z: {var:EmptyStr}="": k$={var:EmptyStr}
	{var:UndoPointer}=Z: {var:AvailableUndos}=Z: {var:UndoRequest}=Z: {var:Recording}=Z
	{uses:lm/lm.inc.bas}
	{uses:include/init.inc.bas}
	{usedef:string_copy_single}, {usedef:game_name}, {var:GameName}
	{usedef:string_copy}, {var:messagesaddr}, 53, {var:Yes}
	{var:ColorText}="{black}": {var:ColorBack}="{light gray}": {var:ColorInput}="{blue}": {var:ColorObject}="{red}": {var:ColorDir}="{dark gray}"
	{uses:custom/init.bas}
	{usedef:set_usr}: dim i$(20), o(20), u(5)
	' undo init
	ta = int(({var:DBtopAddress}-{var:DBbottomAddress})/256+U)*256: u(Z)=57344
	for j=U to 4: u(j)=u(j-U)+ta: next
{:restart}
	print {var:ColorText} "{clear}{switchenable}{lowercase}{switchdisable}";
	ld=1: load {var:GameName}+".db",8,1
