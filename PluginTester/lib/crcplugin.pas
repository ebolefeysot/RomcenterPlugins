unit CrcPlugIn;

interface
uses sysutils, classes, windows;

resourcestring
  TXT_NOTARCDLL = 'Not a romcenter crc dll';
  TXT_PLUGINPATHNOTFOUND = 'Plug in path [%s] not found.';
  TXTs_PLUGINNOTFOUND = 'Plug in %s not found.';
const
  C_DLL_TYPE = 'romcenter signature calculator';

type
  EDllNotFound = class(exception);
  EFunctionNotFound = class(exception);
  ENotACrcDll = class(exception);

  TGetPlugInName = function: PansiChar; stdcall;
  TGetAuthor = function: PansiChar; stdcall;
  TGetVersion = function: PansiChar; stdcall;
  TGetWebPage = function: PansiChar; stdcall;
  TGetEmail = function: PansiChar; stdcall;
  TGetDescription = function: PansiChar; stdcall;

  TGetDllType = function: PansiChar; stdcall;
  TGetDllInterfaceVersion = function: PansiChar; stdcall;

  TGetSignature = function(filename: PansiChar; ZipCrc: PansiChar; var format:PansiChar; var size:int64; var Comment: PansiChar; var ErrorMessage: PansiChar): PansiChar; stdcall;

  TCrcPlugIn = class(TComponent)
  private
    Handle: THandle; //0 if not available

    GetPlugInName: TGetPlugInName;
    GetAuthor: TGetAuthor;
    GetVersion: TGetVersion;
    GetDescription: TGetDescription;

    GetDllInterfaceVersion: TGetDllInterfaceVersion;

    GetWebPage: TGetWebPage;
    GetEmail: TGetEmail;
    FFileName: string;
    FPath: string;

    function IsDllCorrect: boolean;

    procedure InitValues;
    //Fill item properties with dll infos

    procedure LinkInterface;
    procedure SetFileName(const Value: string);
    procedure SetPath(const Value: string);
    //Link Item functions to dll

  public

    DllInterfaceVersion: string;

    PlugInName: string;
    Author: string;
    Version: string;
    Description: string;

    WebPage: string;
    Email: string;

    GetSignature: TGetSignature;

    procedure LoadDll;
    //when dll is loaded, handle > 0

    procedure UnloadDll;

    constructor Create(AOwner: TComponent);override;
  published
    Property FileName: string read FFileName write SetFileName;
    Property Path: string read FPath write SetPath;
  end;

implementation
uses FileCtrl,Dialogs;

{ TCrcPlugIn }

function TCrcPlugIn.IsDllCorrect: boolean;
var
  GetDllType: TGetDllType;
  FPointer: TFarProc;
begin
  result := false;

  //link to GetDllType
  FPointer := GetProcAddress(Handle, pAnsichar('rc_GetDllType'));
  if FPointer <> nil then begin
    GetDllType := TGetDllType(FPointer);
    if GetDllType = C_DLL_TYPE then result := true;
  end;

end;

constructor TCrcPlugIn.Create(AOwner: TComponent);
begin
  inherited;
  Handle := 0;
  Path := '';
end;

procedure TCrcPlugIn.InitValues;
begin

  PlugInName := string (GetPlugInName);
  Author := string (GetAuthor);
  Version := string (GetVersion);
  Description := string (GetDescription);
  WebPage := string (GetWebPage);
  Email := string (GetEmail);

end;

procedure TCrcPlugIn.LinkInterface;
var
  FPointer: TFarProc;
begin
  DllInterfaceVersion := '';
  PlugInName := '';
  Author := '';
  Version := '';
  Description := '';
  WebPage := '';
  Email := '';

  if Handle <= 0 then LoadDll;

  //find the interface version
  //link GetDllInterfaceVersion function
  FPointer := GetProcAddress(Handle, pAnsichar('GetDllInterfaceVersion'));
  if FPointer <> nil then DllInterfaceVersion := '2.50'
  else begin
    FPointer := GetProcAddress(Handle, pAnsichar('rc_GetDllInterfaceVersion'));
    if FPointer <> nil then DllInterfaceVersion := '2.62'
  end;

  //link GetSignature function
  if DllInterfaceVersion = '2.50' then begin
    FPointer := GetProcAddress(Handle, pAnsichar('GetSignature'));
    if FPointer <> nil then begin
      GetSignature := TGetSignature(FPointer);
    end
    else
      raise EFunctionNotFound.create('Function GetSignature not found in ' + filename);

    //link GetPlugInName function
    FPointer := GetProcAddress(Handle, pAnsichar('GetPlugInName'));
    if FPointer <> nil then begin
      GetPlugInName := TGetPlugInName(FPointer);
    end
    else
      raise EFunctionNotFound.create('Function GetPlugInName not found in ' + filename);

    //link GetAuthor function
    FPointer := GetProcAddress(Handle, pAnsichar('GetAuthor'));
    if FPointer <> nil then begin
      GetAuthor := TGetAuthor(FPointer);
    end
    else
      raise EFunctionNotFound.create('Function GetAuthor not found in ' + filename);

    //link GetVersion function
    FPointer := GetProcAddress(Handle, pAnsichar('GetVersion'));
    if FPointer <> nil then begin
      GetVersion := TGetVersion(FPointer);
    end
    else
      raise EFunctionNotFound.create('Function GetVersion not found in ' + filename);

    //link GetDescription function
    FPointer := GetProcAddress(Handle, pAnsichar('GetDescription'));
    if FPointer <> nil then begin
      GetDescription := TGetDescription(FPointer);
    end
    else
      raise EFunctionNotFound.create('Function GetDescription not found in ' + filename);

    //link GetWebPage function
    FPointer := GetProcAddress(Handle, pAnsichar('GetWebPage'));
    if FPointer <> nil then begin
      GetWebPage := TGetWebPage(FPointer);
    end
    else
      raise EFunctionNotFound.create('Function GetWebPage not found in ' + filename);

    //link GetEmail function
    FPointer := GetProcAddress(Handle, pAnsichar('GetEmail'));
    if FPointer <> nil then begin
      GetEmail := TGetEmail(FPointer);
    end
    else
      raise EFunctionNotFound.create('Function GetEmail not found in ' + filename);

    FPointer := GetProcAddress(Handle, pAnsichar('GetDllInterfaceVersion'));
    if FPointer <> nil then begin
      GetDllInterfaceVersion := TGetDllInterfaceVersion(FPointer);
    end
    else
      raise EFunctionNotFound.create('Function GetDllInterfaceVersion not found in ' + filename);
  end
  else begin

    FPointer := GetProcAddress(Handle, pAnsichar('rc_GetSignature'));
    if FPointer <> nil then begin
      GetSignature := TGetSignature(FPointer);
    end
    else
      raise EFunctionNotFound.create('Function rc_GetSignature not found in ' + filename);

    //link GetPlugInName function
    FPointer := GetProcAddress(Handle, pAnsichar('rc_GetPlugInName'));
    if FPointer <> nil then begin
      GetPlugInName := TGetPlugInName(FPointer);
    end
    else
      raise EFunctionNotFound.create('Function rc_GetPlugInName not found in ' + filename);

    //link GetAuthor function
    FPointer := GetProcAddress(Handle, pAnsichar('rc_GetAuthor'));
    if FPointer <> nil then begin
      GetAuthor := TGetAuthor(FPointer);
    end
    else
      raise EFunctionNotFound.create('Function rc_GetAuthor not found in ' + filename);

    //link GetVersion function
    FPointer := GetProcAddress(Handle, pAnsichar('rc_GetVersion'));
    if FPointer <> nil then begin
      GetVersion := TGetVersion(FPointer);
    end
    else
      raise EFunctionNotFound.create('Function rc_GetVersion not found in ' + filename);

    //link GetDescription function
    FPointer := GetProcAddress(Handle, pAnsichar('rc_GetDescription'));
    if FPointer <> nil then begin
      GetDescription := TGetDescription(FPointer);
    end
    else
      raise EFunctionNotFound.create('Function rc_GetDescription not found in ' + filename);

    //link GetWebPage function
    FPointer := GetProcAddress(Handle, pAnsichar('rc_GetWebPage'));
    if FPointer <> nil then begin
      GetWebPage := TGetWebPage(FPointer);
    end
    else
      raise EFunctionNotFound.create('Function rc_GetWebPage not found in ' + filename);

    //link GetEmail function
    FPointer := GetProcAddress(Handle, pAnsichar('rc_GetEmail'));
    if FPointer <> nil then begin
      GetEmail := TGetEmail(FPointer);
    end
    else
      raise EFunctionNotFound.create('Function rc_GetEmail not found in ' + filename);

    //link GetDllInterfaceVersion function
    FPointer := GetProcAddress(Handle, pAnsichar('rc_GetDllInterfaceVersion'));
    if FPointer <> nil then begin
      GetDllInterfaceVersion := TGetDllInterfaceVersion(FPointer);
    end
    else
      raise EFunctionNotFound.create('Function rc_GetDllInterfaceVersion not found in ' + filename);
  end;
end;

procedure TCrcPlugIn.LoadDll;
begin
  Handle := LoadLibrary(pchar(Path + filename));
  if Handle <= 0 then begin
    Handle := 0;
    raise EDllNotFound.Create(format(TXTs_PLUGINNOTFOUND,[filename]));
  end;

  if not IsDllCorrect then begin
    raise ENotACrcDll.create(TXT_NOTARCDLL);
  end;
  LinkInterface;
  InitValues;
end;

procedure TCrcPlugIn.UnloadDll;
begin
  FreeLibrary(Handle);
  Handle := 0;
end;

procedure TCrcPlugIn.SetFileName(const Value: string);
begin
  FFileName := ExtractFileName(Value);
  LoadDll;
end;

procedure TCrcPlugIn.SetPath(const Value: string);
begin
  if value = '' then begin
    FPath := '';
    exit;
  end;
  if not DirectoryExists(value) then
    MessageDlg(format(TXT_PLUGINPATHNOTFOUND,[Value]), mtError, [mbOK], 0)
  else FPath := IncludeTrailingPathDelimiter(Value);

end;

end.

