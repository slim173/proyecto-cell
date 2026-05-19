; ============================================================
;  CellShop ERP — Script de instalación Inno Setup 6
;  Compilar con: Inno Setup 6 (https://jrsoftware.org/isinfo.php)
;  Antes de compilar: ejecuta 1-compilar-distribucion.ps1
; ============================================================

#define AppName      "CellShop ERP"
#define AppVersion   "1.0"
#define AppPublisher "CellShop"
#define AppURL       "http://localhost:51011"
#define AppExeName   "CellApp.exe"
#define InstDir      "{autopf}\CellShop"

[Setup]
AppId                    = {{B4A2C1D3-8E5F-4A7B-9C2D-1E3F5A7B9C2D}
AppName                  = {#AppName}
AppVersion               = {#AppVersion}
AppPublisher             = {#AppPublisher}
AppPublisherURL          = {#AppURL}
DefaultDirName           = {#InstDir}
DefaultGroupName         = {#AppName}
OutputDir                = output
OutputBaseFilename       = CellShop-Setup-v{#AppVersion}
SetupIconFile            =
Compression              = lzma2/ultra64
SolidCompression         = yes
WizardStyle              = modern
PrivilegesRequired       = admin
MinVersion               = 10.0
ArchitecturesInstallIn64BitMode = x64
UninstallDisplayName     = {#AppName}
UninstallDisplayIcon     = {app}\App\CellApp.exe
CloseApplications        = yes
RestartApplications      = no
DisableWelcomePage       = no
LicenseFile              =
WizardImageFile          = compiler:WizModernImage.bmp
WizardSmallImageFile     = compiler:WizModernSmallImage.bmp

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[CustomMessages]
spanish.PgTitle          = Configuración de PostgreSQL
spanish.PgDesc           = Introduce los datos de conexión a la base de datos
spanish.PgHost           = Servidor PostgreSQL (host):
spanish.PgPort           = Puerto:
spanish.PgPass           = Contraseña del usuario postgres:
spanish.PgTestOk         = Conexión verificada correctamente.
spanish.PgTestFail       = No se pudo conectar. Verifica que PostgreSQL esté en ejecución y la contraseña sea correcta.
spanish.PgNotFound       = PostgreSQL no está instalado. Descárgalo desde postgresql.org e instálalo antes de continuar.
spanish.DbSetup          = Configurando base de datos...
spanish.SvcSetup         = Registrando servicios de Windows...
spanish.Done             = CellShop ERP instalado. Accede en: http://localhost:51011

[Dirs]
Name: "{app}\API"
Name: "{app}\App"
Name: "{app}\Database"
Name: "{app}\scripts"

[Files]
; ── Backend API ──────────────────────────────────────────────
Source: "dist\CellApi\*"; DestDir: "{app}\API"; Flags: ignoreversion recursesubdirs createallsubdirs

; ── Frontend App ─────────────────────────────────────────────
Source: "dist\CellApp\*"; DestDir: "{app}\App"; Flags: ignoreversion recursesubdirs createallsubdirs

; ── Scripts SQL ──────────────────────────────────────────────
Source: "dist\Database\*.sql"; DestDir: "{app}\Database"; Flags: ignoreversion

; ── Scripts auxiliares (PowerShell) ─────────────────────────
Source: "scripts\setup-database.ps1";   DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "scripts\register-services.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion

[Icons]
; Menú inicio
Name: "{group}\Abrir CellShop ERP";  Filename: "{#AppURL}"; IconFilename: "{app}\App\CellApp.exe"
Name: "{group}\Iniciar servicios";   Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; Parameters: "-Command ""Start-Service CellShopAPI, CellShopApp"""; IconFilename: "{sys}\shell32.dll"; IconIndex: 238
Name: "{group}\Detener servicios";   Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; Parameters: "-Command ""Stop-Service CellShopApp, CellShopAPI"""; IconFilename: "{sys}\shell32.dll"; IconIndex: 27
Name: "{group}\Desinstalar {#AppName}"; Filename: "{uninstallexe}"

; Acceso directo escritorio
Name: "{autodesktop}\CellShop ERP";  Filename: "{#AppURL}"; IconFilename: "{app}\App\CellApp.exe"

[UninstallRun]
; Detener y eliminar servicios al desinstalar
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"
Parameters: "-ExecutionPolicy Bypass -Command ""Stop-Service CellShopApp,CellShopAPI -Force -ErrorAction SilentlyContinue; sc.exe delete CellShopApp; sc.exe delete CellShopAPI"""
Flags: runhidden waituntilterminated

[Code]
// ── Variables de la página personalizada ─────────────────────
var
  PgPage           : TWizardPage;
  PgHostEdit       : TEdit;
  PgPortEdit       : TEdit;
  PgPassEdit       : TEdit;
  PgTestLabel      : TLabel;

// ── Buscar psql.exe en rutas estándar ────────────────────────
function FindPsql: String;
var
  versiones: TArrayOfString;
  i: Integer;
  ruta: String;
begin
  SetArrayLength(versiones, 5);
  versiones[0] := '18';
  versiones[1] := '17';
  versiones[2] := '16';
  versiones[3] := '15';
  versiones[4] := '14';
  for i := 0 to GetArrayLength(versiones) - 1 do begin
    ruta := 'C:\Program Files\PostgreSQL\' + versiones[i] + '\bin\psql.exe';
    if FileExists(ruta) then begin
      Result := ruta;
      Exit;
    end;
  end;
  Result := '';
end;

// ── Verificar conexión a PostgreSQL ──────────────────────────
function TestPgConnection(Host, Port, Pass: String): Boolean;
var
  psql, tmpFile, cmd: String;
  ResultCode: Integer;
begin
  psql := FindPsql;
  if psql = '' then begin
    Result := False;
    Exit;
  end;
  tmpFile := ExpandConstant('{tmp}\pg_test.txt');
  cmd := Format('set PGPASSWORD=%s && "%s" -h %s -p %s -U postgres -c "SELECT 1;" postgres > "%s" 2>&1',
                [Pass, psql, Host, Port, tmpFile]);
  Exec(ExpandConstant('{sys}\cmd.exe'), '/C ' + cmd, '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := (ResultCode = 0);
end;

// ── Ejecutar PowerShell con política bypass ───────────────────
procedure RunPowerShell(Script, Args: String);
var
  ResultCode: Integer;
  Params: String;
begin
  Params := Format('-ExecutionPolicy Bypass -NonInteractive -File "%s" %s', [Script, Args]);
  Exec('powershell.exe', Params, '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

// ── Crear página personalizada de PostgreSQL ──────────────────
procedure CreatePgPage;
var
  lbl: TLabel;
begin
  PgPage := CreateCustomPage(
    wpSelectDir,
    ExpandConstant('{cm:PgTitle}'),
    ExpandConstant('{cm:PgDesc}')
  );

  // Host
  lbl := TLabel.Create(PgPage);
  lbl.Parent := PgPage.Surface;
  lbl.Caption := ExpandConstant('{cm:PgHost}');
  lbl.Top := 10; lbl.Left := 0; lbl.Width := 300;

  PgHostEdit := TEdit.Create(PgPage);
  PgHostEdit.Parent := PgPage.Surface;
  PgHostEdit.Top := 28; PgHostEdit.Left := 0;
  PgHostEdit.Width := 280; PgHostEdit.Text := 'localhost';

  // Puerto
  lbl := TLabel.Create(PgPage);
  lbl.Parent := PgPage.Surface;
  lbl.Caption := ExpandConstant('{cm:PgPort}');
  lbl.Top := 10; lbl.Left := 295; lbl.Width := 80;

  PgPortEdit := TEdit.Create(PgPage);
  PgPortEdit.Parent := PgPage.Surface;
  PgPortEdit.Top := 28; PgPortEdit.Left := 295;
  PgPortEdit.Width := 80; PgPortEdit.Text := '5432';

  // Contraseña
  lbl := TLabel.Create(PgPage);
  lbl.Parent := PgPage.Surface;
  lbl.Caption := ExpandConstant('{cm:PgPass}');
  lbl.Top := 68; lbl.Left := 0; lbl.Width := 375;

  PgPassEdit := TEdit.Create(PgPage);
  PgPassEdit.Parent := PgPage.Surface;
  PgPassEdit.Top := 86; PgPassEdit.Left := 0;
  PgPassEdit.Width := 375; PgPassEdit.PasswordChar := '*';

  // Etiqueta de resultado del test
  PgTestLabel := TLabel.Create(PgPage);
  PgTestLabel.Parent := PgPage.Surface;
  PgTestLabel.Top := 130; PgTestLabel.Left := 0;
  PgTestLabel.Width := 375; PgTestLabel.Caption := '';
  PgTestLabel.WordWrap := True;
end;

// ── Inicio del asistente ──────────────────────────────────────
procedure InitializeWizard;
begin
  // Verificar que PostgreSQL está instalado
  if FindPsql = '' then begin
    MsgBox(ExpandConstant('{cm:PgNotFound}'), mbError, MB_OK);
  end;
  CreatePgPage;
end;

// ── Validación al avanzar página ─────────────────────────────
function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;

  if CurPageID = PgPage.ID then begin
    // Verificar que no estén vacíos
    if (Trim(PgHostEdit.Text) = '') or (Trim(PgPortEdit.Text) = '') or
       (Trim(PgPassEdit.Text) = '') then begin
      MsgBox('Rellena todos los campos de configuración de PostgreSQL.', mbError, MB_OK);
      Result := False;
      Exit;
    end;

    // Probar conexión
    WizardForm.NextButton.Enabled := False;
    PgTestLabel.Caption := 'Comprobando conexión...';
    PgTestLabel.Font.Color := clBlue;

    if TestPgConnection(PgHostEdit.Text, PgPortEdit.Text, PgPassEdit.Text) then begin
      PgTestLabel.Caption := ExpandConstant('{cm:PgTestOk}');
      PgTestLabel.Font.Color := clGreen;
      Result := True;
    end else begin
      PgTestLabel.Caption := ExpandConstant('{cm:PgTestFail}');
      PgTestLabel.Font.Color := clRed;
      Result := False;
    end;

    WizardForm.NextButton.Enabled := True;
  end;
end;

// ── Acciones post-instalación ─────────────────────────────────
procedure CurStepChanged(CurStep: TSetupStep);
var
  scriptDb, scriptSvc: String;
  pgArgs: String;
begin
  if CurStep = ssPostInstall then begin

    // 1. Configurar base de datos
    WizardForm.StatusLabel.Caption := ExpandConstant('{cm:DbSetup}');
    scriptDb := ExpandConstant('{app}\scripts\setup-database.ps1');
    pgArgs := Format('-PgHost "%s" -PgPort "%s" -PgPassword "%s" -InstallDir "%s"',
                     [PgHostEdit.Text, PgPortEdit.Text, PgPassEdit.Text,
                      ExpandConstant('{app}')]);
    RunPowerShell(scriptDb, pgArgs);

    // 2. Registrar servicios de Windows
    WizardForm.StatusLabel.Caption := ExpandConstant('{cm:SvcSetup}');
    scriptSvc := ExpandConstant('{app}\scripts\register-services.ps1');
    RunPowerShell(scriptSvc, Format('-InstallDir "%s"', [ExpandConstant('{app}')]));
  end;
end;
