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
        Public Sub SendHTTP(ByVal Commande As String) Implements ScriptingLibrary.IUpdater.SendHTTP


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
        If Not reference.EndsWith("\") Then reference &= "\"
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
            Console.WriteLine("Script loaded and compiled: " + scriptPath)
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

    Sub Main()
        Dim clArgs() As String = Environment.GetCommandLineArgs()
        Dim updater As StatusUpdater = New StatusUpdater()

        Console.Title = "JRiver MC Status Notifier"
        Console.BackgroundColor = ConsoleColor.DarkBlue
        Console.ForegroundColor = ConsoleColor.White
        Console.WindowHeight = 15
        Console.WindowWidth = Console.LargestWindowWidth / 3
        Console.Clear()
        Dim script As ScriptingLibrary.IScript = Compile(clArgs(0), clArgs(1))
        If script Is Nothing Then
            Console.WriteLine("Paused... <please press enter>")
            Dim input = Console.ReadLine()
            Return
        End If

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

                Dim fileHasChanged As Boolean = myPlayerStatus.fileKey <> fileKey
                Dim statusHasChanged As Boolean = myPlayerStatus.status <> status Or myPlayerStatus.volume <> volume


                If fileHasChanged Or statusHasChanged Then
                    myPlayerStatus.fileKey = fileKey
                    If fileHasChanged Then
                        If fileKey <> -1 Then
                            nav = DoGet(serverURL + fileInfo + fileKey)
                            myPlayerStatus.mediaType = GetData(nav, mediaTypeField)
                            myPlayerStatus.genre = GetData(nav, genreField)
                        Else
                            myPlayerStatus.mediaType = "Unknown"
                            myPlayerStatus.genre = "Unknown"
                        End If
                    End If
                    myPlayerStatus.status = status
                    myPlayerStatus.volume = volume

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


