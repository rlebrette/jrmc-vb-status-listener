Imports System.CodeDom.Compiler

Public Class Scripting

    Public Enum Languages
        VB
        CSharp
    End Enum

    Public Shared Function CompileScript(ByVal Source As String, ByVal Reference As String, ByVal Language As Languages) As CompilerResults
        Dim provider As CodeDomProvider = Nothing

        Select Case Language
            Case Languages.VB
                provider = New Microsoft.VisualBasic.VBCodeProvider()
            Case Languages.CSharp
                provider = New Microsoft.CSharp.CSharpCodeProvider()
        End Select

        Return CompileScript(Source, Reference, provider)
    End Function

    Public Shared Function CompileScript(ByVal Source As String, ByVal Reference As String, ByVal Provider As CodeDomProvider) As CompilerResults
        Dim compiler As ICodeCompiler = Provider.CreateCompiler()
        Dim params As New CompilerParameters()
        Dim results As CompilerResults

        'Configure parameters
        With params
            .GenerateExecutable = False
            .GenerateInMemory = True
            .IncludeDebugInformation = True
            If Not Reference Is Nothing AndAlso Reference.Length <> 0 Then .ReferencedAssemblies.Add(Reference)
            .ReferencedAssemblies.Add("System.dll")
        End With

        'Compile
        results = compiler.CompileAssemblyFromSource(params, Source)

        Return results
    End Function

    Public Shared Function FindInterface(ByVal DLL As Reflection.Assembly, ByVal InterfaceName As String) As Object
        Dim t As Type

        'Loop through types looking for one that implements the given interface
        For Each t In DLL.GetTypes()
            If Not (t.GetInterface(InterfaceName, True) Is Nothing) Then
                Return DLL.CreateInstance(t.FullName)
            End If
        Next

        Return Nothing
    End Function

End Class
