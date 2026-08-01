; ShopPro Retail POS — NSIS Installer Script
; Target: Windows 10/11 x64

!define APP_NAME "ShopPro Retail POS"
!define APP_VERSION "1.0.0"
!define PUBLISHER "ShopPro Software Technologies"
!define EXE_NAME "ShopPro.UI.exe"

Name "${APP_NAME} ${APP_VERSION}"
OutFile "ShopPro_Setup_v${APP_VERSION}.exe"
InstallDir "$PROGRAMFILES64\ShopPro Retail POS"
RequestExecutionLevel admin

Page directory
Page instfiles

UninstPage uninstConfirm
UninstPage instfiles

Section "MainSection" SEC01
    SetOutPath "$INSTDIR"
    File /r "publish\*.*"

    ; Desktop Shortcut
    CreateShortCut "$DESKTOP\${APP_NAME}.lnk" "$INSTDIR\${EXE_NAME}"

    ; Start Menu Shortcut
    CreateDirectory "$SMPROGRAMS\${APP_NAME}"
    CreateShortCut "$SMPROGRAMS\${APP_NAME}\${APP_NAME}.lnk" "$INSTDIR\${EXE_NAME}"

    ; Write Uninstaller
    WriteUninstaller "$INSTDIR\uninstall.exe"
SectionEnd

Section "Uninstall"
    Delete "$DESKTOP\${APP_NAME}.lnk"
    RMDir /r "$SMPROGRAMS\${APP_NAME}"
    RMDir /r "$INSTDIR"
SectionEnd
