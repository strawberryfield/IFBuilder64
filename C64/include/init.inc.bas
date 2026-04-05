' Adventure Player 2026
' copyright (c) 2026 Roberto Ceccarelli
' https://strawberryfield.altervista.org
' Released under GNU Affero GPL 3.0
'
' Config init
{var:ConfigAddr}={usedef:Config_Addr}
'
' Read quntities
{var:NumRooms}=peek({var:ConfigAddr}): {var:ConfigAddr}={var:ConfigAddr}+1
{var:NumObjects}=peek({var:ConfigAddr}): {var:ConfigAddr}={var:ConfigAddr}+1
{var:NumVerbs}=peek({var:ConfigAddr}): {var:ConfigAddr}={var:ConfigAddr}+1
{var:NumDoors}=peek({var:ConfigAddr}): {var:ConfigAddr}={var:ConfigAddr}+1
'
' Databases addresses
{usedef:peek_word}, {var:ConfigAddr}, {var:curroomdbaddr}: {var:ConfigAddr}={var:ConfigAddr}+2
{var:roomsdbaddr}= {var:curroomdbaddr}+3: {var:maxloaddbaddr}= {var:curroomdbaddr}+1: {var:currentloaddbaddr}= {var:curroomdbaddr}+2
{var:itemsunlockdbaddr} = {var:roomsdbaddr}+ {var:NumRooms} *6
{var:itemsoptionsdbaddr}= {var:itemsunlockdbaddr}+{var:NumObjects}
{var:itemsinitemdbaddr}= {var:itemsoptionsdbaddr}+{var:NumObjects}
{var:itemsdbaddr}= {var:itemsinitemdbaddr}+{var:NumObjects}
{var:doorsoptionsdbaddr}= {var:itemsdbaddr} + {var:NumObjects}
{var:doorsunlockdbaddr}= {var:doorsoptionsdbaddr} + {var:NumDoors} 
{var:DBtopAddress}= {var:doorsunlockdbaddr} + {var:NumDoors}: {var:DBbottomAddress}= {var:curroomdbaddr}
'
' Strings addresses
{usedef:peek_word}, {var:ConfigAddr}, {var:introaddr}: {var:ConfigAddr}={var:ConfigAddr}+2
{usedef:peek_word}, {var:ConfigAddr}, {var:messagesaddr}: {var:ConfigAddr}={var:ConfigAddr}+2
{usedef:peek_word}, {var:ConfigAddr}, {var:usrmsgaddr}: {var:ConfigAddr}={var:ConfigAddr}+2
{usedef:peek_word}, {var:ConfigAddr}, {var:verbsaddr}: {var:ConfigAddr}={var:ConfigAddr}+2: {var:verbstable}={var:verbsaddr} +{var:NumVerbs} +{var:NumVerbs} +2: {var:verbsdbaddr}={var:verbsaddr}-{var:NumVerbs} 
{usedef:peek_word}, {var:ConfigAddr}, {var:dirsaddr}: {var:ConfigAddr}={var:ConfigAddr}+2: {var:dirstable}={var:dirsaddr} +14 
' Items
{usedef:peek_word}, {var:ConfigAddr}, {var:itemsaddr}: {var:ConfigAddr}={var:ConfigAddr}+2: {var:itemstable}={var:itemsaddr} +{var:NumObjects} +{var:NumObjects} +2
{usedef:peek_word}, {var:ConfigAddr}, {var:itemsdescaddr}: {var:ConfigAddr}={var:ConfigAddr}+2
{usedef:peek_word}, {var:ConfigAddr}, {var:roomsdescaddr}: {var:ConfigAddr}={var:ConfigAddr}+2
' Doors
{usedef:peek_word}, {var:ConfigAddr}, {var:doorsaddr}: {var:ConfigAddr}={var:ConfigAddr}+2: {var:doorstable}={var:doorsaddr} +{var:NumDoors} +{var:NumDoors} +2
{usedef:peek_word}, {var:ConfigAddr}, {var:doorsdescaddr}
{var:doorsdbaddr}=  {var:doorsdescaddr} - {var:NumDoors} *6: {var:roomsDoorsdbaddr}=  {var:doorsdbaddr} - {var:NumRooms} *6: {var:doorscountdbaddr} = {var:roomsDoorsdbaddr} - {var:NumRooms}
' rooms first Item, count
{var:rooms1stitemdbaddr} = {var:doorscountdbaddr}  - {var:NumRooms}: {var:roomsitemscountdbaddr} = {var:rooms1stitemdbaddr} - {var:NumRooms}: {usedef:peek_byte}, {var:roomsitemscountdbaddr}, {var:NumPortable}
' items open desc
{var:ConfigAddr}={var:ConfigAddr}+2: {usedef:peek_word}, {var:ConfigAddr}, {var:itemsopendescaddr}
' doors deby open
{var:ConfigAddr}={var:ConfigAddr}+2: {usedef:peek_word}, {var:ConfigAddr}, {var:doorsdenyopenaddr}