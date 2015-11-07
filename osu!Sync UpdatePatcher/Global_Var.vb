Imports System.IO
Module Global_Var

    Public I__StartUpArguments() As String

    Function WriteCrashLog(ex As Exception) As String
        If Not Directory.Exists(Path.GetTempPath & "naseweis520\osu!Sync\Crashes") Then
            Directory.CreateDirectory(Path.GetTempPath & "naseweis520\osu!Sync\Crashes")
        End If
        Dim CrashFile As String = Path.GetTempPath & "naseweis520\osu!Sync\Crashes\" & Date.Now.ToString("yyyy-MM-dd HH-mm-ss") & ".txt"
        Using File As StreamWriter = My.Computer.FileSystem.OpenTextFileWriter(CrashFile, False)
            Dim Content As String = "=====   osu!Sync Crash | " & Date.Now.ToString("yyyy-MM-dd HH:mm:ss") & "   =====" & vbNewLine & vbNewLine &
                "// Information" & vbNewLine & "An exception occured in osu!Sync. If this problem persists please report it using the Feedback-window, on GitHub or on the osu!Forum." & vbNewLine & "When reporting please try to describe as detailed as possible what you've done and how the applicationen reacted." & vbNewLine & "GitHub: http://j.mp/1PDuDFp   |   osu!Forum: http://j.mp/1PDuCkK" & vbNewLine & vbNewLine &
                "// Exception" & vbNewLine & ex.ToString
            File.Write(Content)
            File.Close()
        End Using

        Return CrashFile
    End Function
End Module
