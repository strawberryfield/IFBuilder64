' Adventure Player 2026
' copyright (c) 2026 Roberto Ceccarelli
' https://strawberryfield.altervista.org
' Released under GNU Affero GPL 3.0
'
' machine language entry points
{var:sys_print_text=YA}
{var:sys_print_string=YB}
{var:sys_string_legth=YC}
{var:sys_string_copy=YD}
{var:sys_to_lower=YE}
{var:sys_search_word=YF}
{var:sys_set_usr=YG}
{var:sys_set_cursor=YH}
{var:sys_clean_row=YI}
{var:sys_peek_byte=YJ}
{var:sys_peek_word=YK}
{var:sys_poke_word=YL}
{var:sys_wait_page=YM}
{var:sys_print_line=YN}
{var:sys_string_copy_single=YO}
{var:sys_copy_to_mem=YP}
{var:sys_bit_get=YQ}
{var:sys_bit_set=YR}
{var:sys_bit_reset=YS}
{var:sys_scandb=YT}
{var:sys_copy_screen=YU}
{var:sys_copy_status=YV}

' sys calls
{def:print_text=sys YA}
{def:print_string=sys YB}
{def:string_legth=sys YC}
{def:string_copy=sys YD}
{def:to_lower=sys YE}
{def:search_word=sys YF}
{def:set_usr=sys YG}
{def:set_cursor=sys YH}
{def:clean_row=sys YI}
{def:peek_byte=sys YJ}
{def:peek_word=sys YK}
{def:poke_word=sys YL}
{def:wait_page=sys YM}
{def:print_line=sys YN}
{def:string_copy_single=sys YO}
{def:copy_to_mem=sys YP}
{def:bit_get=sys YQ}
{def:bit_set=sys YR}
{def:bit_reset=sys YS}
{def:scandb=sys YT}
{def:copy_screen=sys YU}
{def:copy_status=sys YV}

' locations
{var:list_counter=XA}
{var:list_data=XB}
