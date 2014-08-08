Imports System.Threading
Imports System.Net
Imports System.Text
Imports System.IO
Imports System.Xml
Imports System.Xml.XPath

Module JRMCStatusListener
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

    Structure PlayerStatus
        Property fileKey As String
        Property status As String
        Property mediaType As String
        Property genre As String
        Property volume As String
    End Structure

    Dim myPlayerStatus As PlayerStatus

    Dim serverURL As String

    Sub JRMCStatusChanged(status As PlayerStatus)
        Console.WriteLine(status.fileKey + "|" + status.status + "|" + status.mediaType + "|" + status.genre + "|" + status.volume)
    End Sub

    Sub Main()
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

                    JRMCStatusChanged(myPlayerStatus)
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
