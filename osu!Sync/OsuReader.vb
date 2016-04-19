Imports System.IO

Public Class OsuReader
    Inherits BinaryReader

    Sub New(s As Stream)
        MyBase.New(s)
    End Sub

    Public Overrides Function ReadString() As String
        Dim tag As Byte = ReadByte()
        If tag = 0 Then Return Nothing
        If tag = &HB Then Return MyBase.ReadString()
        Throw New IOException("Invalid string tag")
    End Function

    Public Function ReadDate() As Date
        Return New DateTime(ReadInt64())
    End Function
End Class
