// Plug in for RomCenter
//
// (C) Copyright 2012 Eric Bole-Feysot
// All Rights Reserved.
//
// The dll produced by this code is a signature plug in for RomCenter application
// www.romcenter.com

// The main function (GetSignature) calculates a signature (crc...) of the file
// given in parameters and send it back to romcenter.

library sha1;
{$R *.res}

uses
  classes,
  sysutils,
  IdHashSHA,
  idHash;

// Datas definitions
// Calculate the crc of the full file.
// Send back the zipcrc if available
// No format calculated (format = '')
// _________________

// These datas defines infos about this plug in. They are loaded by romcenter.
// Use of this plug in depends of these datas;
const
  C_PlugInName = 'Arcade sha1 calculator'; // full name of plug in
  C_Author = 'Eric Bole-Feysot'; // your name
  C_Version = '1.0.0'; // version of plug in
  C_WebPage = 'www.romcenter.com'; // home page of plug in
  C_Email = 'help@romcenter.com'; // Email of plug in support
  C_Description = 'Sha1 calculator.';

  // The main function to define is 'GetSignature' located at the end of this code;

  // functions definitions
  // _____________________

function rc_GetDllInterfaceVersion: pansichar; stdcall;
begin
  // Version of the interface. Do not change
  Result := '2.62';
end;

function rc_GetDllType: pansichar; stdcall;
begin
  // PlugIn check. Do not change
  Result := 'romcenter signature calculator';
end;

function rc_GetPlugInName: pansichar; stdcall;
begin
  Result := C_PlugInName;
end;

function rc_GetAuthor: pansichar; stdcall;
begin
  Result := C_Author;
end;

function rc_GetVersion: pansichar; stdcall;
begin
  Result := C_Version;
end;

function rc_GetWebPage: pansichar; stdcall;
begin
  Result := C_WebPage;
end;

function rc_GetEmail: pansichar; stdcall;
begin
  Result := C_Email;
end;

function rc_GetDescription: pansichar; stdcall;
begin
  Result := C_Description;
end;

// ______________________________________________________________________________
// main function definition
// ______________________________________________________________________________
function rc_GetSignature(filename: pansichar; ZipCrc: pansichar; var format: pansichar; var size: int64; var comment, ErrorMsg: pansichar)
  : pansichar; stdcall;
var
  sfilename:string;
  idSha1 : TIdHashSHA1;
  fs : TFileStream;
begin
  sfilename := string(filename);

  Result := '';
  try

    // return the sha-1 of the filename
    // this signature will be compared to the signature stored in the datafile

    // result:
    // errormsg: This text will appear in the error field of romcenter view. If an
    // errormsg is returned, the file will be considered as corrupted and not available.

    idSha1 := TIdHashSHA1.Create;
    fs := TFileStream.Create(sfileName, fmOpenRead OR fmShareDenyWrite) ;
    size := fs.Size;
    try
      result := pansichar(ansistring(idSha1.HashStreamAsHex(fs)));
    finally
      fs.Free;
      idSha1.Free;
    end;

  except
    on e: Exception do
    begin
      ErrorMsg := pansichar(ansistring(e.Message));
    end;
  end;
end;

exports
  rc_GetAuthor, rc_GetDescription, rc_GetDllInterfaceVersion,
  rc_GetSignature, rc_GetDllType, rc_GetEmail, rc_GetPlugInName,
  rc_GetVersion, rc_GetWebPage;

end.
