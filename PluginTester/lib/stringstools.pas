unit stringsTools;

interface
uses classes;

resourcestring
  TXT_NA = 'N/A';
  TXT_BYTES = 'bytes';
  TXT_KB = 'KB';
  TXT_MB = 'MB';
  TXT_GB = 'GB';

  function STRINGRSCAN(source:string;sep:char):string;
  function STRINGLSCAN(source:string;sep:char):string;
  function isalphanum(str:string):boolean;

  function Str_Scan(texte:string;BegSep:char;endSep:char;nieme:integer):string;
  //extrait le nieme (n=1...) champ de la string. compris entre begsep et endsep
  // ex: extract('kg h> lhlh<123>kjhkjh<456>','<','>',2) = '456'

  function comp(str:string):string;
  //supprime les blanc devant, derriere, est les espaces multiples dans une chaine

  function guillemet_html(str:string):string;
  //Recherche les guillemets et les remplace par le tag html "&quot;"

  function Str_Extract(source:string;debut:integer;fin:integer):string;
  //Renvoie l'extrait de la chaine compris entre les positions debut et fin
  //(debut et fin compris) ex:  Str_Extract('abcde g',3,5) -->'cde'

  function Str_Right(source:string;nbcar:integer):string;
  //Renvoie les nbcar caractères situés à la fin de la chaine
  // ex : str_right('abcde g',3) --> 'e g'

  function Str_Left(source:string;nbcar:integer):string;
  //Renvoie les nbcar caractères situés au début de la chaine
  // ex : str_left('abcde g',3) --> 'abc'

  function StringPos(source:string;s:string):string;
  //renvoie une string commencant à la chaine s de source.
  //exemple : stringpos('abd def ghi','ef') = 'ef ghi'

  procedure stringcopy(var dest:string;source:string);
  //Copie la string source dans la string destination

  function Inttostrf(n:integer;fmt:string):string;
  //Convertion d'un entier en string
  //Si le format posséde plus de digit que le nombre, des zeros sont ajoutés a gauche

  function DosTrunc(filename:string):string;
  //trunc le nom du fichier à 8 caractères (utilisé avec pkzip)
  //ex: kod_kde38.rom -> kod_kde3.rom

  function ExtrFileName(filename:string):string;
  function ExcludeTrailingExtension(filename:string):string; //renvoie le nom du fichier sans l'extension
  function Field(ligne:string;ind:integer):string; //renvoie le champ num 'ind' de la ligne (separateur = ¤)

  function FindString(s:String;list:TStringList):integer;

  function CompCrc(crc:string):string;

  function FixLength(str:string;len:integer):string; //complete le nom avec des espaces jusqu'a ce que le nombre de caractères soit atteint.

  function StringEllipsisLeft(str:string;len:integer):string;
  //return ...[last 'len' char]
  //if string has less than len, return str
  //else return str truncated starting with '...'

  function StringEllipsisRight(str:string;len:integer):string;
  //return [first 'len' char]...
  //if string has less than len, return str
  //else return str truncated ending with '...'

  function FileSizeToStr(OctetSize:int64):string;
  //retourne la taille exprimé en octets, KB, MB ou GB

  Function IncludeLeadingHttp(url:string):string;
  //retourne l'url avec http:// devant si nécessaire

  function TransformToDos(Str:String;var Changed:Boolean):string;
  //remplace les caractères interdit sous dos par '_' dans Str

  //retourne la position du nieme caractère 'c' dans la chaine 's'
  function CharPosn(s:string;c:char; n:integer):integer;

  //decode escape char used in xml, url or http like %eacute; &amp; %#244;
  function unescape(line: string): string;

  function ExtractFileExtEmul(filename:string):string;
  function ExcludeTrailingExtensionEmul(filename:string):string; //renvoie le nom du fichier sans l'extension

  function DoubleQuote(s:string):string;

implementation

uses SysUtils,jclstrings, Math;

//______________________________________________________________________________

  function CharPosn(s:string;c:char; n:integer):integer;
  var
    i,p :integer;

  begin
    result := -1;
    p := 0;
    for i := 1 to length(s) do begin
      if s[i] = c then begin
        p := p + 1;
        if p = n then begin
          result := i;
          exit;
        end;
      end;
    end;
  end;

//______________________________________________________________________________
function STRINGRSCAN(source:string;sep:char):string;
var
  i,j:integer;
begin
  result := '';
  source := comp(source);
  i:=length(source)-1;
  while (i>=0) and (source[i] <> sep) do
  begin
    i:=i-1;
  end;
  for j := i+1 to length(source) do
   result := result + source[j];

end;


function StringPos(source:string;s:string):string;
var
  i,ls,lsource:integer;
begin
  ls := length(s);
  lsource :=length(source);
  for i := 0 to Lsource - 1 do begin
    if source[i] = s[1] then begin
      if Str_Extract(source,i,i+ls-1) = s then break;
    end;
  end;
  result := Str_Right(source,lsource - i);
end;


function StringLScan(source:string;sep:char):string;
var
  i:integer;
begin
  result := '';
  i:=1;
  while (i<=length(source)) and (source[i] <> sep) do
  begin
    result := result + source[i];
    i:=i+1;
  end;

end;

function Str_Scan(texte:string;BegSep:char;endSep:char;nieme:integer):string;
var
  i,j:integer;
  s:string;
begin
  result := '';
  if Length(texte) = 0 then
  begin
    result := '';
    exit;
  end;
  j:=0;
  s:='';
  for i := 1 to length(texte) do
  begin
    if texte[i] = begsep then j := j+1;
    if j = nieme then break;
  end;

  if j <> nieme then exit;
  if i >= length(texte) then exit;

  i:=i+1;
  while (i < length(texte)) and (texte[i] <> endsep) do
  begin
    result := result + texte[i]; //on commence à enregistrer
    i := i+1;
  end;
  if (texte[i] <> endsep) then result := result + texte[i];
end;

function isalphanum(str:string):boolean;
var
  l:integer;
begin
  if str='' then
  begin
    result:=true;
    exit;
  end;
  l:=1;
  while ((l<= length(str)) and
        (ord(str[l]) >= ord('0')) and (ord(str[l]) <= ord('9'))) or
        ((ord(str[l]) >= ord('A')) and (ord(str[l]) <= ord('Z'))) or
        (ord(str[l]) = ord(' ')) do l:=l+1;
  result := (l>length(str));
  end;

function comp(str:string):string;
//supprime les blanc devant, derrier, est les espaces multiples dans une chaine
var
  i:integer;
  lastchar,currchar:char;

begin
  if str='' then
  begin
    result:='';
    exit;
  end;

  result:='';
  str:=trim(str);
  lastchar:=' ';
  for i := 1 to length(str) do
  begin
    currchar:=str[i];
    if (lastchar <> ' ') or (currchar <> ' ') then
      result := result + currchar;
    lastchar:= currchar;
  end;

end;

function guillemet_html(str:string):string;
var
  i:integer;
  currchar:char;
const
  html_quote = '&quot;';
begin
  if str='' then
  begin
    result:='';
    exit;
  end;

  result:='';
  i:=1;
  while i <= length(str) do
  begin
    currchar:=str[i];
    if currchar = '"'
    then result := result + html_quote
    else result := result + currchar;
    i := i+1;
  end;
end;

function Str_Extract(source:string;debut:integer;fin:integer):string;
//Renvoie l'extrait de la chaine compris entre les positions debut et fin
//(debut et fin compris)
begin
  result := copy(source,debut,fin-debut+1);
end;

function Str_Right(source:string;nbcar:integer):string;
//Renvoie les nbcar caractères situés à la fin de la chaine
// ex : str_right('abcde g',3) --> 'e g'
begin
  result := Str_Extract(source,length(source)-nbcar+1,length(source));
end;

function Str_Left(source:string;nbcar:integer):string;
//Renvoie les nbcar caractères situés au début de la chaine
// ex : str_right('abcde g',3) --> 'abc'
begin
  result := Str_Extract(source,1,nbcar);
end;

//**************************************************************
  procedure stringcopy(var dest:string;source:string);
  begin
    dest := source;
  end;

//**************************************************************
  function Inttostrf(n:integer;fmt:string):string;
  //Convertion d'un entier en string avec conversion
  //Si le format posséde plus de digit que le nombre, des zeros
  //sont ajoutés a gauche
  var
    s:string;
    i:integer;
  begin
    s := format(fmt,[n]);
    for i := 1 to strlen(Pchar(s)) do
    begin
      if s[i] = ' ' then s[i] := '0'
    end;
    result := s;
  end;

//**************************************************************
  function DosTrunc(filename:string):string;
  var
    f:string;
  begin
    f := Str_Left(STRINGLSCAN(filename,'.'),8) + ExtractFileExt(filename);
    result := f;
  end;

//**************************************************************
  function ExtrFileName(filename:string):string;
  var
    i,j:integer;
    c:char;
  begin
    result := '';
    i:=length(filename);
    while (i > 0) and (filename[i] <> '\') and (filename[i] <> '/') do
    begin
      i:=i-1;
    end;
    for j := i+1 to length(filename) do begin
      c := filename[j];
      result := result + c;
    end;
  end;

//______________________________________________________________________________
  function ExcludeTrailingExtension(filename:string):string;
  //renvoie le nom du fichier sans l'extension
  var
    Ext:string;
  begin
    Ext := ExtractFileExt(FileName);
    if Ext = '' then result := filename
    else result := Str_Extract(filename,1,Pos(Ext,filename)-1);
  end;
//______________________________________________________________________________
function Field(ligne:string;ind:integer):string; //renvoie le champ num 'ind' de la ligne (separateur = ¤)
begin
  result := Str_Scan(ligne,'¤','¤',ind);
end;

//______________________________________________________________________________
function FindString(s:String;list:TStringList):integer;
//rend l'indice de la premiere ligne de list commencant par s
var
  i:integer;
  ligne:string;
begin
  for i := 0 to list.count -1 do begin
    ligne := list[i];
    if Str_Left(ligne,length(s)) = s then begin
      result := i;
      exit;
    end;
  end;
  result := -1;

end;

//______________________________________________________________________________
  function CompCrc(crc:string):string;
  //renvoie le crc complémenté
  var
    ccrc:integer;
  begin
    if (crc = '') or (crc = '        ') or (uppercase(crc) = TXT_NA) then Result := ''
    else begin
      if TryStrToInt('0x'+crc,ccrc) then
        result := lowercase(inttohex( StrToInt('0xFFFFFFFF') - ccrc,8))
      else result := crc;  //cas ou la signature contient des lettres.
    end;
  end;

//______________________________________________________________________________
  function FixLength(str:string;len:integer):string; //complete le nom avec des espaces jusqu'a ce que le nombre de caractères soit atteint.
  var
    diff,i:integer;
  begin
    diff:= len - length(str);
    for i := 1 to diff do str := str + ' ';
    result := str;
  end;

//______________________________________________________________________________
  function FileSizeToStr(OctetSize:int64):string;
  begin
    if OctetSize = -1 then result := TXT_NA
    else If OctetSize >= 1024 * 1024 * 1024 Then // > 1024 M0, on affiche des Go
      Result := FloatToStrF(OctetSize / (1024 * 1024 * 1024), ffnumber, 10, 1) + ' ' + TXT_GB
    Else If OctetSize >= 1024 * 1024 Then // > 1024 Ko, on affiche des Mo
      Result := FloatToStrF(OctetSize / 1048576, ffnumber, 10, 1) + ' ' + TXT_MB
    Else If OctetSize >= 1024 Then // > 1024 oct, on affiche des Ko
      Result := FloatToStrF(OctetSize / 1024, ffnumber, 10, 1) + ' ' + TXT_KB
    Else // < 10 Ko on affiche des octets
      Result := IntToStr(OctetSize) + ' ' + TXT_BYTES;
  end;

//______________________________________________________________________________
  Function IncludeLeadingHttp(url:string):string;
  //retourne l'url avec http:// devant si nécessaire
  begin
    if pos('://', URL) = 0 then result := 'http://' + URL
    else result := url;
  end;

  //______________________________________________________________________________
  function TransformToDos(Str:String;var Changed:Boolean):string;
  //remplace les caractères interdit sous dos par '_' dans Str
  begin
    Result := str;
    CharReplace(result,'/','\');
    //CharReplace(result,'\','_');
    Result := ExtractFileName(Result); //some dat have folders in romname ! Remove them.
    CharReplace(result,':','_');
    CharReplace(result,'*','_');
    CharReplace(result,'?','_');
    CharReplace(result,'"','_');
    CharReplace(result,'<','_');
    CharReplace(result,'>','_');
    CharReplace(result,'|','_');
    CharReplace(result,'獥','_');
    CharReplace(result,'甮','_');

    changed := (str <> result);
  end;

  //______________________________________________________________________________
  function StringEllipsisLeft(str:string;len:integer):string;
  begin
    if Length(str) <= len then result := str
    else begin
      result := '...' + StrRight(str,len-3);
    end;
  end;

  function StringEllipsisRight(str:string;len:integer):string;
  begin
    if Length(str) <= len then result := str
    else begin
      result := StrLeft(str,len-3) + '...';
    end;
  end;

function unescape(line: string): string;
{ decode escape char like %eacute; &amp; %#244;
  complete list (not all supported)
    160     : r:='nbsp';
    161     : r:='excl';
    162     : r:='cent';
    163     : r:='ound';
    164     : r:='curren';
    165     : r:='yen';
    166     : r:='brvbar';
    167     : r:='sect';
    168     : r:='uml';
    169     : r:='copy';
    170     : r:='ordf';
    171     : r:='laquo';
    172     : r:='not';
    173     : r:='shy';
    174     : r:='reg';
    175     : r:='macr';
    176     : r:='deg';
    177     : r:='plusmn';
    178     : r:='sup2';
    179     : r:='sup3';
    180     : r:='acute';
    181     : r:='micro';
    182     : r:='para';
    183     : r:='middot';
    184     : r:='cedil';
    185     : r:='sup1';
    186     : r:='ordm';
    187     : r:='raquo';
    188     : r:='frac14';
    189     : r:='frac12';
    190     : r:='frac34';
    191     : r:='iquest';
    192     : r:='Agrave';
    193     : r:='Aacute';
    194     : r:='Acirc';
    195     : r:='Atilde';
    196     : r:='Auml';
    197     : r:='Aring';
    198     : r:='AElig';
    199     : r:='Ccedil';
    200     : r:='Egrave';
    201     : r:='Eacute';
    202     : r:='Ecirc';
    203     : r:='Euml';
    204     : r:='Igrave';
    205     : r:='Iacute';
    206     : r:='Icirc';
    207     : r:='Iuml';
    208     : r:='ETH';
    209     : r:='Ntilde';
    210     : r:='Ograve';
    211     : r:='Oacute';
    212     : r:='Ocirc';
    213     : r:='Otilde';
    214     : r:='Ouml';
    215     : r:='times';
    216     : r:='Oslash';
    217     : r:='Ugrave';
    218     : r:='Uacute';
    219     : r:='Ucirc';
    220     : r:='Uuml';
    221     : r:='Yacute';
    222     : r:='THORN';
    223     : r:='szlig';
    224     : r:='agrave';
    225     : r:='aacute';
    226     : r:='acirc';
    227     : r:='atilde';
    228     : r:='auml';
    229     : r:='aring';
    230     : r:='aelig';
    231     : r:='ccedil';
    232     : r:='egrave';
    233     : r:='eacute';
    234     : r:='ecirc';
    235     : r:='euml';
    236     : r:='igrave';
    237     : r:='iacute';
    238     : r:='icirc';
    239     : r:='iuml';
    240     : r:='eth';
    241     : r:='ntilde';
    242     : r:='ograve';
    243     : r:='oacute';
    244     : r:='ocirc';
    245     : r:='otilde';
    246     : r:='ouml';
    247     : r:='divide';
    248     : r:='oslash';
    249     : r:='ugrave';
    250     : r:='uacute';
    251     : r:='ucirc';
    252     : r:='uuml';
    253     : r:='yacute';
    254     : r:='thorn';
    255     : r:='yuml';
}
var
  p1:integer;
  p2:integer;
  s1,s2,e1,e2:string;
begin
  result := line;
  if line = '' then exit;

  p1 := StrFind('&',line,1);
  if p1 <> 0 then begin
    p2 := StrFind(';',line,p1);
    if p2 <> 0 then begin
      e1 := StrMid(line,p1+1,p2-p1-1);
      s1 := StrLeft(line,p2);
      s2 := StrRestOf(line,p2+1);
      //decode e1
      if e1[1] = '#' then begin
        e2 := chr(StrToInt(strrestof(e1,2)));
      end
      else begin
        if e1 = 'amp' then e2 := '&'
        else if e1 = 'lt' then e2 := '<'
        else if e1 = 'gt' then e2 := '>'
        else if e1 = 'egrave' then e2 := 'è'
        else if e1 = 'eacute' then e2 := 'é'
        else if e1 = 'quot' then e2 := '"'
        else e2 := '?';
      end;

      //result
      result:= StringReplace(s1,'&'+e1+';',e2,[]) + unescape(s2);
    end;
  end;

end;

function DoubleQuote(s:string):string;
begin
  result := StringReplace(s,'''','''''',[rfReplaceAll]);
end;

//extract the emulation extension (.ext)
//.gb or 3 letters
function ExtractFileExtEmul(filename:string):string;
begin
  result := '';
  if sametext(extractfileext(filename),'.zip') then result := '.zip'
end;

function ExcludeTrailingExtensionEmul(filename:string):string; //renvoie le nom du fichier sans l'extension
  //renvoie le nom du fichier sans l'extension
  var
    Ext:string;
  begin
    Ext := ExtractFileExtEmul(FileName);
    if Ext = '' then result := filename
    else result := Str_Left(filename,length(filename)-length(ext));
  end;
end.
