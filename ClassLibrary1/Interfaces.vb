Public Interface IScript
    Sub Update(ByVal Updater As IUpdater)
End Interface

Public Interface IUpdater
    Function PlayerStatus() As PlayerStatus
    Sub Execute(ByVal Commande As String)
    Sub SendHTTP(ByVal Commande As String)
End Interface

Public Structure PlayerStatus
    Property FileKey As String
    Property Status As String
    Property MediaType As String
    Property Genre As String
    Property Volume As String

    Public Function ToJSON() As String
        ToJSON = "{" +
            """key"":""" + FileKey + """," +
            """status:""" + Status + """," +
            """mediaType"":""" + MediaType + """," +
            """genre"":""" + Genre + """," +
            """volume"":""" + Volume + """}"
    End Function
End Structure