Imports System.Threading
Imports System.Net
Imports System.Text
Imports System.IO
Imports System.Xml
Imports System.Xml.XPath
Imports System.CodeDom.Compiler


Module JRMCStatusListener

    Public Class StatusUpdater
        Implements ScriptingLibrary.IUpdater
        Public Sub Execute(ByVal Commande As String) Implements ScriptingLibrary.IUpdater.Execute


        End Sub
        Public Sub SendHTTP(Commande As String, Optional Args As Dictionary(Of String, String) = Nothing) Implements ScriptingLibrary.IUpdater.SendHTTP
            Try
                If Not Args Is Nothing Then
                    Dim sep As String = ""
                    Commande &= "?"
                    For Each arg In Args
                        Commande &= sep + arg.Key + "=" + System.Web.HttpUtility.UrlEncode(arg.Value)
                        sep = "&"
                    Next
                End If


                Dim request As HttpWebRequest = WebRequest.Create(Commande)
                Dim response As HttpWebResponse = request.GetResponse()
                response.Close()
            Catch ex As Exception
                Console.WriteLine("| " + ex.Message)
                Console.WriteLine("| " + Commande)
            End Try
        End Sub
        Public Function PlayerStatus() As ScriptingLibrary.PlayerStatus Implements ScriptingLibrary.IUpdater.PlayerStatus
            Return myPlayerStatus
        End Function
    End Class

    Private Property _compiledScript As ScriptingLibrary.IScript

    Function Compile(currentExec As String, scriptPath As String) As ScriptingLibrary.IScript
        Dim results As CompilerResults
        Dim reference As String

        'Find reference
        reference = System.IO.Path.GetDirectoryName(currentExec)
        If Not reference = "" And Not reference.EndsWith("\") Then reference &= "\"
        reference &= "ScriptingLibrary.dll"
        Dim line As String

        Try
            Using sr As New StreamReader(scriptPath)
                line = sr.ReadToEnd()
            End Using
        Catch e As Exception
            Console.WriteLine("The file could not be read:")
            Console.WriteLine(e.Message)
            Throw e
        End Try

        'Compile script
        results = Scripting.CompileScript(line, reference, Scripting.Languages.VB)

        If results.Errors.Count = 0 Then
            Compile = DirectCast(Scripting.FindInterface(results.CompiledAssembly, "IScript"), ScriptingLibrary.IScript)
        Else
            Dim err As CompilerError

            Console.BackgroundColor = ConsoleColor.DarkRed
            Console.WriteLine("Script loaded and compiled with ERRORS: " + scriptPath)
            'Add each error as a listview item with its line number
            For Each err In results.Errors
                Console.WriteLine("Line : " + err.Line.ToString() + "=>" + err.ErrorText)
            Next

        End If
    End Function

    Const sleepTime = 250 ' milliseconds
    '----
    Const baseUrl = "http://{0}:{1}/MCWS/v1/"
    Const playInfo = "Playback/Info?Zone=-1"
    Const fileInfo = "File/GetInfo?File="
    Const fileKeyItem = "Response/Item[@Name='FileKey']/text()"
    Const statusItem = "Response/Item[@Name='Status']/text()"
    Const volumeItem = "Response/Item[@Name='VolumeDisplay']/text()"
    Const mediaTypeField = "MPL/Item/Field[@Name='Media Type']/text()"
    Const genreField = "MPL/Item/Field[@Name='Genre']/text()"

    Dim myPlayerStatus As ScriptingLibrary.PlayerStatus
    Dim serverURL As String

    Sub Pause(message As String)
        Console.WriteLine(message)
        Console.BackgroundColor = ConsoleColor.DarkRed
        Console.WriteLine("Paused... <please press a key>")
        Console.BackgroundColor = ConsoleColor.Black
        Console.ReadKey()
    End Sub
    Sub Main()
        Dim clArgs() As String = Environment.GetCommandLineArgs()
        Dim updater As StatusUpdater = New StatusUpdater()

        Console.Title = "JRiver MC Status Notifier"
        Console.ForegroundColor = ConsoleColor.White
        Console.WindowHeight = 15
        Console.WindowWidth = Console.LargestWindowWidth / 3
        Dim scriptFile As String
        If clArgs.Length <> 2 Then
            scriptFile = My.Settings.Script
        Else
            scriptFile = clArgs(1)
        End If

        Dim script As ScriptingLibrary.IScript = Compile(clArgs(0), scriptFile)
        If script Is Nothing Then
            Pause("The script can't be compiled")
            Return
        End If

        Console.BackgroundColor = ConsoleColor.DarkBlue
        Console.Clear()
        Console.WriteLine(Console.Title + " " + My.Application.Info.Version.ToString)
        Console.WriteLine("Script loaded and compiled: " + scriptFile)
        Dim connected As Boolean = True
        serverURL = String.Format(baseUrl, My.Settings.Host, My.Settings.Port)

        Dim nav As XPathNavigator

        Do While (True)
            Try
                nav = DoGet(serverURL + playInfo)
                connected = True
                Dim fileKey As String = GetData(nav, fileKeyItem)
                Dim status As String = GetData(nav, statusItem)
                Dim volume As String = GetData(nav, volumeItem)

                Dim fileHasChanged As Boolean = myPlayerStatus.FileKey <> fileKey
                Dim statusHasChanged As Boolean = myPlayerStatus.Status <> status Or myPlayerStatus.Volume <> volume


                If fileHasChanged Or statusHasChanged Then
                    myPlayerStatus.FileKey = fileKey
                    If fileHasChanged Then
                        If fileKey <> -1 Then
                            nav = DoGet(serverURL + fileInfo + fileKey)
                            myPlayerStatus.MediaType = GetData(nav, mediaTypeField)
                            myPlayerStatus.Genre = GetData(nav, genreField)
                        Else
                            myPlayerStatus.MediaType = "Unknown"
                            myPlayerStatus.Genre = "Unknown"
                        End If
                    End If
                    myPlayerStatus.Status = status
                    myPlayerStatus.Volume = volume

                    script.Update(updater)
                End If
                Thread.Sleep(sleepTime)
            Catch ex As WebException
                If (connected) Then
                    Console.WriteLine(serverURL + ":" + ex.Message)
                End If
                connected = False
            End Try
        Loop
    End Sub

    Function DoGet(path As String) As XPathNavigator
        Dim request As HttpWebRequest = WebRequest.Create(path)
        SetBasicAuthHeader(request, My.Settings.Username, My.Settings.Password)
        Dim response As HttpWebResponse = request.GetResponse()
        Dim sr As StreamReader = New StreamReader(response.GetResponseStream())
        Dim docNav As XPathDocument = New XPathDocument(sr)
        response.Close()
        Return docNav.CreateNavigator()
    End Function

    Sub SetBasicAuthHeader(request As WebRequest, userName As String, userPassword As String)
        Dim authInfo As String = userName + ":" + userPassword
        authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo))
        request.Headers.Set("Authorization", "Basic " + authInfo)
    End Sub

    Function GetData(nav As XPathNavigator, path As String) As String
        Dim result As XPathNodeIterator = nav.Evaluate(path)
        result.MoveNext()
        Return result.Current.ToString
    End Function
End Module


