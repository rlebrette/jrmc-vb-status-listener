Imports System.Threading
Imports System.Net
Imports System.Text
Imports System.IO
Imports System.Xml
Imports System.Xml.XPath

Module JRMCStatusListener
    Const server = "localhost"
    Const port = 52199
    Const user = "rlebrette"
    Const password = "C0largol$"
    Const sleepTime = 500 ' milliseconds
    '----
    Const baseUrl = "http://{0}:{1}/MCWS/v1/"
    Const playInfo = "Playback/Info?Zone=-1"
    Const fileInfo = "File/GetInfo?File="
    Const fileKeyItem = "Response/Item[@Name='FileKey']/text()"
    Const statusItem = "Response/Item[@Name='Status']/text()"
    Const mediaTypeField = "MPL/Item/Field[@Name='Media Type']/text()"
    Const genreField = "MPL/Item/Field[@Name='Genre']/text()"

    Structure PlayerStatus
        Property fileKey As String
        Property status As String
        Property mediaType As String
        Property genre As String
    End Structure

    Dim myPlayerStatus As PlayerStatus

    Dim serverURL As String

    Sub JRMCStatusChanged(status As PlayerStatus)
        Console.WriteLine(status.fileKey + " " + status.status + " " + status.mediaType + " " + status.genre)
    End Sub

    Sub Main()
        serverURL = String.Format(baseUrl, My.Settings.Host, My.Settings.Port)

        Dim nav As XPathNavigator

        Do While (True)
            Try
                nav = DoGet(serverURL + playInfo)
                Dim fileKey As String = GetData(nav, fileKeyItem)
                Dim status As String = GetData(nav, statusItem)
                Dim fileHasChanged As Boolean = myPlayerStatus.fileKey <> fileKey
                Dim statusHasChanged As Boolean = myPlayerStatus.status <> status

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
                    JRMCStatusChanged(myPlayerStatus)
                End If
                Thread.Sleep(sleepTime)
            Catch ex As WebException
                Console.WriteLine(serverURL + ":" + ex.Message)
            End Try
        Loop
    End Sub

    Function DoGet(path As String) As XPathNavigator
        Dim request As HttpWebRequest = WebRequest.Create(path)
        SetBasicAuthHeader(request, user, password)
        Dim response As HttpWebResponse = request.GetResponse()
        Dim sr As StreamReader = New StreamReader(response.GetResponseStream())
        Dim docNav As XPathDocument = New XPathDocument(sr)
        Return docNav.CreateNavigator()
    End Function

    Sub SetBasicAuthHeader(request As WebRequest, userName As String, userPassword As String)
        Dim authInfo As String = My.Settings.Username + ":" + My.Settings.Password
        authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo))
        request.Headers.Set("Authorization", "Basic " + authInfo)
    End Sub

    Function GetData(nav As XPathNavigator, path As String) As String
        Dim result As XPathNodeIterator = nav.Evaluate(path)
        result.MoveNext()
        Return result.Current.ToString
    End Function
End Module
