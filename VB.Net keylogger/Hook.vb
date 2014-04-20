﻿Imports System.Runtime.InteropServices
Public Class Hook
    Private Delegate Function CallBack(ByVal nCode As Integer, ByVal wParam As IntPtr, ByVal lParam As IntPtr) As Integer
    Enum HookType As Integer
        WH_JOURNALRECORD = 0
        WH_JOURNALPLAYBACK = 1
        WH_KEYBOARD = 2
        WH_GETMESSAGE = 3
        WH_CALLWNDPROC = 4
        WH_CBT = 5
        WH_SYSMSGFILTER = 6
        WH_MOUSE = 7
        WH_HARDWARE = 8
        WH_DEBUG = 9
        WH_SHELL = 10
        WH_FOREGROUNDIDLE = 11
        WH_CALLWNDPROCRET = 12
        WH_KEYBOARD_LL = 13
        WH_MOUSE_LL = 14
    End Enum
#Region "WindowsHook functions"
    'Import for the SetWindowsHookEx function.
    <DllImport("User32.dll", CharSet:=CharSet.Auto, CallingConvention:=CallingConvention.StdCall)> _
    Private Overloads Shared Function SetWindowsHookEx _
          (ByVal HookType As HookType, ByVal HookProc As CallBack, _
           ByVal hInstance As IntPtr, ByVal wParam As Integer) As Integer
    End Function
    'Import for the CallNextHookEx function.
    <DllImport("User32.dll", CharSet:=CharSet.Auto, CallingConvention:=CallingConvention.StdCall)> _
    Private Overloads Shared Function CallNextHookEx _
          (ByVal idHook As Integer, ByVal nCode As Integer, _
           ByVal wParam As IntPtr, ByVal lParam As IntPtr) As Integer
    End Function
    'Import for the UnhookWindowsHookEx function.
    <DllImport("User32.dll", CharSet:=CharSet.Auto, CallingConvention:=CallingConvention.StdCall)> _
    Private Overloads Shared Function UnhookWindowsHookEx _
              (ByVal idHook As Integer) As Boolean
    End Function
#End Region
    Private hHooks As New Dictionary(Of HookType, Integer)
    Private Keyboardhookpro As CallBack = AddressOf LowLevelKeyboardProc
    Private Mousehookproc As CallBack = AddressOf LowLevelMouseProc
    Public Sub Hook(hookType As HookType)
        If hHooks.ContainsKey(hookType) Then Return
        Select Case hookType
            Case hookType.WH_KEYBOARD_LL
                hHooks.Add(hookType, SetWindowsHookEx(hookType.WH_KEYBOARD_LL, Keyboardhookpro, IntPtr.Zero, 0))
            Case hookType.WH_MOUSE_LL
                hHooks.Add(hookType, SetWindowsHookEx(hookType.WH_MOUSE_LL, Mousehookproc, IntPtr.Zero, 0))
            Case Else
                Throw New NotImplementedException()
        End Select
        If hHooks(hookType) = 0 Then
            Debug.Print("SetWindowsHookEx {0}", hookType.ToString)
            Return
        Else
            Debug.Print("SetWindowsHookEx {0}", hookType.ToString)
        End If
    End Sub
    Public Sub UnHook(hookType As HookType)
        If hHooks.ContainsKey(hookType) Then
            If UnhookWindowsHookEx(hHooks(hookType)).Equals(False) Then
                Debug.Print("UnhookWindowsHookEx {0}", hookType.ToString)
                Return
            Else
                Debug.Print("UnhookWindowsHookEx {0}", hookType.ToString)
                hHooks.Remove(hookType)
            End If
        End If
    End Sub

#Region "WH_KEYBOARD_LL"
    Public Enum WM_KEYBOARD_MSG
        WM_KEYDOWN = &H100
        WM_KEYUP = &H101
        WM_SYSKEYDOWN = &H104
        WM_SYSKEYUP = &H105
    End Enum
    <Flags()> Public Enum KBDLLHOOKSTRUCTFlags As UInt32
        LLKHF_EXTENDED = &H1
        LLKHF_INJECTED = &H10
        LLKHF_ALTDOWN = &H20
        LLKHF_UP = &H80
    End Enum
    ' KeyboardStruct structure declaration.
    <StructLayout(LayoutKind.Sequential)> Public Structure KBDLLHOOKSTRUCT
        Public vkCode As Keys
        Public scanCode As UInt32
        Public flags As KBDLLHOOKSTRUCTFlags
        Public time As UInt32
        Public dwExtraInfo As UIntPtr
    End Structure
    Public Event KeyboardChange(nCode As Integer, wParam As WM_KEYBOARD_MSG, ByRef lParam As KBDLLHOOKSTRUCT, ByRef cancel As Boolean)
    Private Function LowLevelKeyboardProc(ByVal nCode As Integer, ByVal wParam As IntPtr, ByVal lParam As IntPtr) As Integer
        If wParam = WM_KEYBOARD_MSG.WM_KEYDOWN Or wParam = WM_KEYBOARD_MSG.WM_SYSKEYDOWN Then
            My.Settings.TotalKeyboardClick += 1
            My.Settings.Save()
        End If

        Dim myKeyboardHookStruct As New KBDLLHOOKSTRUCT()
        myKeyboardHookStruct = CType(Marshal.PtrToStructure(lParam, myKeyboardHookStruct.GetType()), KBDLLHOOKSTRUCT)

        Dim cancel As Boolean = False
        RaiseEvent KeyboardChange(nCode, wParam, myKeyboardHookStruct, cancel)
        If cancel = True Then
            Return 1
        Else
            Return CallNextHookEx(hHooks(HookType.WH_KEYBOARD_LL), nCode, wParam, lParam)
        End If
    End Function
#End Region

#Region "WH_MOUSE_LL"
    Public Enum WM_MOUSE_MSG As UInt32
        WM_MOUSEMOVE = &H200
        WM_LBUTTONDOWN = &H201
        WM_LBUTTONUP = &H202
        WM_MOUSEWHEEL = &H20A
        WM_MOUSEHWHEEL = &H20E
        WM_RBUTTONDOWN = &H204
        WM_RBUTTONUP = &H205
        WM_MOUSEWHEELUP = 519
        WM_MOUSEWHEELDOWN = 520
    End Enum
    'MouseHookStruct structure declaration.
    <StructLayout(LayoutKind.Sequential)> Public Structure MSLLHOOKSTRUCT
        Public pt As Drawing.Point
        Public mouseData As UInt32
        Public flags As UInt32
        Public time As UInt32
        Public dwExtraInfo As UInt64
    End Structure
    Public Event MouseChange(nCode As Integer, wParam As WM_MOUSE_MSG, lParam As MSLLHOOKSTRUCT, ByRef cancel As Boolean)
    Private Function LowLevelMouseProc(ByVal nCode As Integer, ByVal wParam As Integer, ByVal lParam As IntPtr) As Integer
        If wParam = WM_MOUSE_MSG.WM_MOUSEMOVE Then My.Settings.TotalMouseMoves += 1
        If wParam = WM_MOUSE_MSG.WM_LBUTTONDOWN Or wParam = WM_MOUSE_MSG.WM_RBUTTONDOWN Then My.Settings.TotalMouseClick += 1
        My.Settings.Save()

        Dim myMouseHookStruct As New MSLLHOOKSTRUCT()
        myMouseHookStruct = CType(Marshal.PtrToStructure(lParam, myMouseHookStruct.GetType()), MSLLHOOKSTRUCT)

        Dim cancel As Boolean = False
        RaiseEvent MouseChange(nCode, wParam, myMouseHookStruct, cancel)
        If cancel = True Then
            Return 1
        Else
            Return CallNextHookEx(hHooks(HookType.WH_MOUSE_LL), nCode, wParam, lParam)
        End If
    End Function
#End Region

End Class
