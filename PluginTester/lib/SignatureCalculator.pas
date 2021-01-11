//Copyright (C) 2001 Eric Bole-Feysot
//www.romcenter.com
//plug in support
//zip handler

unit SignatureCalculator;

interface

uses
  Windows, // DWORD for D3/D4 compatibility
  classes,
  sysutils,
  //DiskAccess,
  crcplugin, archivemanager;

resourcestring
  TXTs_FILENOTFOUND = 'File %s not found.';

type
  //exceptions
  ECrcFileNotFound = class(exception);
  EZipCorrupted = class(exception);
  EPlugInError = class(exception);
  ENoPlugInAvailable = class(exception);

  //resultats d'un fichier
  TCrcItem = class
    Name: string; //nom du fichier concerné
    Format: string; //format détecté du fichier (ex: .smd)
    Signature: string; //signature du fichier
    Size: int64; //taille réelle de la rom (sans header... (= 2^n))
    Comment:string; //commentaire sur la rom elle même.
    ErrorMessage: string; //eventuel message d'erreur retourné par le plug in
    Date:TDateTime;
  end;

  TSignatureCalculator = class(TComponent)
  private
    FTempDir: string;
    FScanInsideZip: boolean;
    FScanExtensions: string;
    CrcItems: Tlist;
    FCrcPlugIn: TCrcPlugIn;
    FSkipExtensions: string;
    FUseZipCrcForArcade: boolean;
    function Get(index: integer): TCrcItem;
    procedure SetTempDir(const Value: string);
    function GetCount: integer;
    procedure Add(FileName, Crc: string; Format: string; Size: int64; comment,ErrorMsg: string;date:TDateTime);
    procedure SetScanExtensions(const Value: string);
    procedure SetSkipExtensions(const Value: string);
    procedure SetUseZipCrcForArcade(const Value: boolean);
  public
    Arch: TArchiveManager;
    constructor Create(AOwner: TComponent); override;
    destructor Destroy; override;
    property count: integer read GetCount; //Number of result
    property CrcItem[index: integer]: TCrcItem read Get; default;
    function CalcCrc(FileName: string):boolean; //return false if corrupted

  published
    property CrcPlugIn: TCrcPlugIn read FCrcPlugIn write FCrcPlugIn;
    property TempDir: string read FTempDir write SetTempDir;
    property ScanInsideZip: boolean read FScanInsideZip write FScanInsideZip;
    property ScanExtensions: string read FScanExtensions write SetScanExtensions;
    property SkipExtensions: string read FSkipExtensions write SetSkipExtensions;
    property UseZipCrcForArcade: boolean read FUseZipCrcForArcade write SetUseZipCrcForArcade;
  end;

implementation

uses
  Dialogs, // ShowMessage
  stringstools, filestools, filectrl, archive,jclFileUtils,rcstrings;

{ TSignatureCalculator }

constructor TSignatureCalculator.Create(AOwner: TComponent);
begin
  inherited;
  CrcItems := TList.Create;
  ScanInsideZip := true;
  UseZipCrcForArcade := true;
  ScanExtensions := '*';
end;

destructor TSignatureCalculator.destroy;
var
  i:integer;
begin
  for i := 0 to CrcItems.Count - 1 do begin
    TCrcItem(CrcItems[i]).Free;
  end;
  CrcItems.Free;
  inherited destroy;
end;

function TSignatureCalculator.CalcCrc(FileName: string):boolean; //return fals eif corrupted
var
  ext: string;
  CRC,fileext, errormsg,comment: string;
  zipCrcAnsi,fileextAnsi, errormsgAnsi,commentAnsi: PAnsiChar;
  ZipFullName, ZipName, ZipCrc: string;
  ZipSize: Integer;
  ZipDate:TDateTime;
  Size: int64;
  i: integer;
  OldDir: string;
  TempFile: string;
  TempFileAnsi:PAnsiChar;
  it: TArchiveItems;
  changed:boolean;
  begin
  //efface la liste de resultats
  for i := 0 to CrcItems.Count - 1 do begin
    TCrcItem(CrcItems[i]).Free;
  end;

  CrcItems.clear;
  result := true;
  //file exists?

  if not jclfileutils.fileexists(filename) then raise ECrcFileNotFound.Create(format(TXTs_FILENOTFOUND,[filename]));

  if arch.IsAnArchive(filename) and ScanInsideZip then begin //lecture du zip

    it := TArchiveItems.Create;
    ZipDate := 0;
    try //finally
      try //except
        arch.GetContent(FileName, it);
        comment := arch.GetComment(FileName);

        //for each file of the zip
        for i := 0 to it.Count - 1 do begin
          zipFullName := it[i].FullName; //name of rom with folders in zip
          zipname := it[i].Name;
          zipcrc := it[i].crc;
          zipsize := it[i].UncompSize;
          ZipDate := it[i].Date;
          fileext := '';//pchar(ExtractFileExt(it[i].Name));
          //filter files
          if (ScanExtensions <> '*') and (pos(lowercase(ExtractFileExt(zipname)), ScanExtensions) = 0) then continue;
          if (pos(lowercase(ExtractFileExt(zipname)), SkipExtensions) <> 0) then continue;

          //n'affiche pas les répertoires (comme dans winzip)
          if (zipname = '') and (zipsize = 0) then continue;

          //ajoute les infos dans la liste
          //If chd is zipped, unzip and calculate sha-1
          if (CrcPlugIn = nil)
          or ( UseZipCrcForArcade and
               (lowercase(ExtractFileName(CrcPlugIn.FileName)) = 'arcade.dll')
               and not(sametext(ExtractFileExt(zipname),'.chd'))
              ) then begin
            //utilise le crc du zip
            add(zipFullName, zipcrc, fileext,  ZipSize, comment,'',ZipDate);
            continue;
          end
          else begin
            //unzip file
            OldDir := GetCurrentDir;
            SetCurrentDir(TempDir);

            Arch.TempDir := TempDir;
            try
              //unzip
              Arch.ExtractFile(FileName, ZipFullName, tempdir, false);
              TempFile := tempdir + extractFileName(ZipName);

              //calcul le crc du fichier
              fileextansi := '';
              errormsgansi := '';
              commentansi := '';
              zipCrcAnsi :=PAnsiChar(Ansistring(zipCrc));
              TempFileAnsi := PAnsiChar(Ansistring(TempFile));
              crc := string(CrcPlugIn.GetSignature(TempFileAnsi, zipcrcAnsi, fileextAnsi, size, commentansi,errormsgansi));
              fileext := string(FileextAnsi);
              errormsg := string(errormsgAnsi);
              //comment := string(commentansi); zip comment <> plugin comment
              //if plugin return a size of 0, use the file size instead
              if size <= 0 then size := zipsize;
              fileext := TransformToDos(fileext,changed);
              if changed then begin
                fileext := '';
              end;

            finally
              //supprime les fichiers
              UnprotectFile(ExtrFileName(TempFile));
              DeleteFile(ExtrFileName(TempFile));
              SetCurrentDir(OldDir);
            end;

            //ajoute les infos dans la liste
            add(zipName, lowercase(crc), fileext, size,comment, errormsg,ZipDate);
          end;
        end; //fichier suivant
      except
        on e:exception do begin
          add(zipName, '00000000', fileext, size,'', e.Message,ZipDate );
          result := false;
        end;
      end;
    finally
      it.Free;
    end;

  end
  else begin
    //not an archive
    ext := ExtractFileExt(FileName);
    if (pos('*', ScanExtensions) = 0) and (trim(ScanExtensions) <> '') and (pos(ext, ScanExtensions) = 0) then exit;

    //calculate the crc
    if CrcPlugIn = nil then raise ENoPlugInAvailable.Create('No plug in specified');
    fileextansi := '';
    errormsg := '';
    comment := '';
    TempFileAnsi := PAnsiChar(Ansistring(FileName));
    crc := String(CrcPlugIn.GetSignature(TempFileAnsi, nil, fileextansi, size, commentansi,errormsgansi));
    //if plugin return a size of 0, use the file size instead
    if size <= 0 then size := SizeOfFile(FileName); //size of rom is <> size of file if rom has header.
    fileext := TransformToDos(string(FileextAnsi),changed);
    errormsg := string(errormsgAnsi);
    comment := string(commentansi);
    zipdate := DateOfFile(filename);

    add(ExtractFileName(FileName), crc, fileext, size,comment, errormsg,zipdate);

  end;
end;

procedure TSignatureCalculator.Add(FileName, Crc: string; Format: string; Size: int64; comment,ErrorMsg: string;date:TDateTime);
var
  item: TCrcItem;
begin
  item := TCrcItem.Create;
  item.Name := FileName;
  item.Format := Format;
  item.Signature := Crc;
  item.Size := Size;
  item.Comment := Comment;
  item.ErrorMessage := ErrorMsg;
  item.Date := date;

  CrcItems.Add(item);
end;

function TSignatureCalculator.Get(index: integer): TCrcItem;
begin
  result := TCrcItem(CrcItems.items[index]);
end;

procedure TSignatureCalculator.SetTempDir(const Value: string);
begin
  if (trim(value) = '') or (not DirectoryExists(Value)) then
    FTempDir := WindowsTempDir
  else
    FTempDir := IncludeTrailingPathDelimiter(Value);
end;

function TSignatureCalculator.GetCount: integer;
begin
  result := CrcItems.Count;
end;

procedure TSignatureCalculator.SetScanExtensions(const Value: string);
begin
  FScanExtensions := Trim(lowercase(Value));
  if (pos('*', FScanExtensions) <> 0) or (FScanExtensions = '') then FScanExtensions := '*';
end;

procedure TSignatureCalculator.SetSkipExtensions(const Value: string);
begin
  FSkipExtensions := trim(lowercase(Value));
end;

procedure TSignatureCalculator.SetUseZipCrcForArcade(const Value: boolean);
begin
  FUseZipCrcForArcade := Value;
end;

end.

