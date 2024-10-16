Imports System.Reflection
Imports System.Text
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports RestSharp

Public Class ApiInterface
    Public Token As String, Message As String

    Public Function ExecuteApi(ByVal ApiRoute As String) As DataOutput
        Dim DataOutput As DataOutput = New DataOutput()
        DataOutput = ApiPostData(ApiRoute)
        Return DataOutput
    End Function

    ''' <summary>
    ''' Fill a model with the result of an Api invocation.
    ''' </summary>
    ''' <param name="ApiRoute">Api path and name.</param>
    ''' <param name="HashT">Parameters to be sent to the Api.</param>
    ''' <returns></returns>
    Public Function ExecuteApi(ByVal ApiRoute As String, ByVal HashT As Hashtable) As DataOutput
        Dim DataOutput As DataOutput = New DataOutput()
        DataOutput = ApiPostData(ApiRoute, HashT)
        Return DataOutput
    End Function

    ''' <summary>
    ''' Fill a model with the result of an Api invocation.
    ''' </summary>
    ''' <param name="ApiRoute">Api path and name.</param>
    ''' <param name="Model">Parameters to be sent to the Api.</param>
    ''' <returns></returns>
    Public Function ExecuteApi(ByVal ApiRoute As String, ByVal Model As Object) As DataOutput
        Dim DataOutput As DataOutput = New DataOutput()
        DataOutput = ApiPostData(ApiRoute, Model)
        Return DataOutput
    End Function

    Public Function ApiPostData(ByVal ApiRoute As String) As DataOutput
        GetTokenApi()

        Dim dataOutput As DataOutput = New DataOutput()
        Dim client As New RestSharp.RestClient(ApiRoute)
        Dim request As New RestRequest(Method.POST)
        request.AddHeader("UserName", System.Configuration.ConfigurationManager.AppSettings("UserName"))
        request.AddHeader("Token", Token)
        request.Timeout = CInt(60) * 60000

        Dim response As New RestResponse
        response = client.Execute(request)
        Dim json As String = response.Content

        Dim resp As Object = JsonConvert.DeserializeObject(Of DataOutput)(json)
        dataOutput.ResponseCode = resp.ResponseCode
        dataOutput.ResponseMessage = resp.ResponseMessage
        dataOutput.ResponseData = resp.ResponseData

        Return dataOutput
    End Function

    Public Function ApiPostData(ByVal ApiRoute As String, ByVal HashT As Hashtable) As DataOutput
        GetTokenApi()

        Dim dataOutput As DataOutput = New DataOutput()
        Dim client As New RestSharp.RestClient(ApiRoute)
        Dim request As New RestRequest(Method.POST)
        request.AddHeader("UserName", System.Configuration.ConfigurationManager.AppSettings("UserName"))
        request.AddHeader("Token", Token)
        request.Timeout = CInt(60) * 60000

        If HashT.Count() > 0 Then
            For Each item As DictionaryEntry In HashT
                request.AddParameter(item.Key, item.Value)
            Next
        End If

        Dim response As New RestResponse
        response = client.Execute(request)
        Dim json As String = response.Content

        Dim resp As Object = JsonConvert.DeserializeObject(Of DataOutput)(json)
        dataOutput.ResponseCode = resp.ResponseCode
        dataOutput.ResponseMessage = resp.ResponseMessage
        dataOutput.ResponseData = resp.ResponseData

        Return dataOutput
    End Function

    Public Function ApiPostData(ByVal ApiRoute As String, ByVal Body As Object) As DataOutput
        GetTokenApi()

        Dim dataOutput As DataOutput = New DataOutput()
        Dim client As New RestSharp.RestClient(ApiRoute)
        Dim request As New RestRequest(Method.POST)
        request.AddHeader("UserName", System.Configuration.ConfigurationManager.AppSettings("UserName"))
        request.AddHeader("Token", Token)
        request.AddJsonBody(Body)
        request.Timeout = CInt(60) * 60000

        Dim response As New RestResponse
        response = client.Execute(request)
        Dim json As String = response.Content

        Dim resp As Object = JsonConvert.DeserializeObject(Of DataOutput)(json)
        dataOutput.ResponseCode = resp.ResponseCode
        dataOutput.ResponseMessage = resp.ResponseMessage
        dataOutput.ResponseData = resp.ResponseData

        Return dataOutput
    End Function


    Protected Sub GetTokenApi()
        Try
            Dim requestToken As System.Net.WebRequest = System.Net.WebRequest.Create(System.Configuration.ConfigurationManager.AppSettings("ApiToken"))
            Dim dataToken As String = "UserName=" + System.Configuration.ConfigurationManager.AppSettings("UserName") +
                "&UserSecret=" + System.Configuration.ConfigurationManager.AppSettings("UserSecret")
            requestToken.Method = "POST"
            requestToken.ContentType = "application/x-www-form-urlencoded"
            requestToken.ContentLength = dataToken.Length
            requestToken.Credentials = System.Net.CredentialCache.DefaultCredentials
            requestToken.Timeout = CInt(60) * 60000

            Dim encodingToken As New UTF8Encoding()
            Using dsAuxToken = requestToken.GetRequestStream()
                dsAuxToken.Write(encodingToken.GetBytes(dataToken), 0, dataToken.Length)
            End Using

            Dim responseToken As System.Net.WebResponse = requestToken.GetResponse()
            Dim dsToken As System.IO.Stream = responseToken.GetResponseStream()
            Dim readerToken As New System.IO.StreamReader(dsToken)
            Dim responseServerToken As String = readerToken.ReadToEnd()
            readerToken.Close()
            responseToken.Close()

            Try
                Dim joToken As JObject = JObject.Parse(responseServerToken)
                Token = joToken.SelectToken("ResponseData[0].Token").ToString
            Catch ex As Exception
                Token = Nothing
                Message = "Error: Invalid Token."
            End Try
        Catch hex As System.Net.WebException
            Message = "Error: " + hex.Message()
        Catch ex As Exception
            Message = "Error: " + ex.Message()
        End Try
    End Sub

    Public Function ApiGetDataSet(ByVal ApiRoute As String) As DataSet
        GetTokenApi()

        Dim ds As DataSet = New DataSet()
        Dim dT As DataTable = New DataTable()
        Dim client As New RestSharp.RestClient(ApiRoute)
        Dim request As New RestRequest(Method.POST)
        request.AddHeader("UserName", System.Configuration.ConfigurationManager.AppSettings("UserName"))
        request.AddHeader("Token", Token)
        request.Timeout = CInt(60) * 60000

        Dim response As New RestResponse
        response = client.Execute(request)
        Dim json As String = response.Content

        Dim resp As Object = JsonConvert.DeserializeObject(Of DataOutput)(json)
        Dim code As String = resp.ResponseCode

        If code = 200 Then
            Dim data As List(Of Object) = resp.ResponseData
            Dim jsonData As Object = JsonConvert.DeserializeObject(Of Object)(data.First.ToString())

            For Each columnName As JProperty In jsonData
                dT.Columns.Add(columnName.Value, Type.GetType("System.String"))
            Next

            For i As Integer = 1 To data.Count() - 1
                Dim v As Integer = 0
                Dim jsonDataValue As Object = JsonConvert.DeserializeObject(Of Object)(data(i).ToString())
                Dim dr As DataRow = dT.NewRow

                For Each columnData As JProperty In jsonDataValue
                    dr(dT.Columns(v).ToString()) = columnData.Value.ToString()

                    v = v + 1
                Next

                dT.Rows.Add(dr)
            Next
        End If

        ds.Tables.Add(dT)

        Return ds
    End Function

    Public Function ApiGetDataSet(ByVal ApiRoute As String, ByVal HashT As Hashtable) As DataSet
        GetTokenApi()

        Dim ds As DataSet = New DataSet()
        Dim dT As DataTable = New DataTable()
        Dim client As New RestSharp.RestClient(ApiRoute)
        Dim request As New RestRequest(Method.POST)
        request.AddHeader("UserName", System.Configuration.ConfigurationManager.AppSettings("UserName"))
        request.AddHeader("Token", Token)
        request.Timeout = CInt(60) * 60000

        If HashT.Count() > 0 Then
            For Each item As DictionaryEntry In HashT
                request.AddParameter(item.Key, item.Value)
            Next
        End If

        Dim response As New RestResponse
        response = client.Execute(request)
        Dim json As String = response.Content

        Dim resp As Object = JsonConvert.DeserializeObject(Of DataOutput)(json)
        Dim code As String = resp.ResponseCode

        If code = 200 Then
            Dim data As List(Of Object) = resp.ResponseData
            Dim jsonData As Object = JsonConvert.DeserializeObject(Of Object)(data.First.ToString())

            For Each columnName As JProperty In jsonData
                dT.Columns.Add(columnName.Value, Type.GetType("System.String"))
            Next

            For i As Integer = 1 To data.Count() - 1
                Dim v As Integer = 0
                Dim jsonDataValue As Object = JsonConvert.DeserializeObject(Of Object)(data(i).ToString())
                Dim dr As DataRow = dT.NewRow

                For Each columnData As JProperty In jsonDataValue
                    dr(dT.Columns(v).ToString()) = columnData.Value.ToString()

                    v = v + 1
                Next

                dT.Rows.Add(dr)
            Next
        End If

        ds.Tables.Add(dT)

        Return ds
    End Function

    Public Function ApiGetDataSet(ByVal ApiRoute As String, ByVal Body As Object) As DataSet
        GetTokenApi()

        Dim ds As DataSet = New DataSet()
        Dim dT As DataTable = New DataTable()
        Dim client As New RestSharp.RestClient(ApiRoute)
        Dim request As New RestRequest(Method.POST)
        request.AddHeader("UserName", System.Configuration.ConfigurationManager.AppSettings("UserName"))
        request.AddHeader("Token", Token)
        request.AddJsonBody(Body)
        request.Timeout = CInt(60) * 60000

        Dim response As New RestResponse
        response = client.Execute(request)
        Dim json As String = response.Content

        Dim resp As Object = JsonConvert.DeserializeObject(Of DataOutput)(json)
        Dim code As String = resp.ResponseCode

        If code = 200 Then
            Dim data As List(Of Object) = resp.ResponseData
            Dim jsonData As Object = JsonConvert.DeserializeObject(Of Object)(data.First.ToString())

            For Each columnName As JProperty In jsonData
                dT.Columns.Add(columnName.Value, Type.GetType("System.String"))
            Next

            For i As Integer = 1 To data.Count() - 1
                Dim v As Integer = 0
                Dim jsonDataValue As Object = JsonConvert.DeserializeObject(Of Object)(data(i).ToString())
                Dim dr As DataRow = dT.NewRow

                For Each columnData As JProperty In jsonDataValue
                    dr(dT.Columns(v).ToString()) = columnData.Value.ToString()

                    v = v + 1
                Next

                dT.Rows.Add(dr)
            Next
        End If

        ds.Tables.Add(dT)

        Return ds
    End Function

    Public Function API_Get_DataTable(ByVal APIRoute As String) As DataTable
        GetTokenApi()

        Dim dt As DataTable = New DataTable()
        Dim client As New RestSharp.RestClient(APIRoute)
        Dim request As New RestRequest(Method.GET)
        request.AddHeader("UserName", System.Configuration.ConfigurationManager.AppSettings("UserName"))
        request.AddHeader("Token", Token)
        request.Timeout = CInt(60) * 60000

        Dim response As New RestResponse
        response = client.Execute(request)
        Dim json As String = response.Content

        Dim resp As Object = JsonConvert.DeserializeObject(Of DataOutput)(json)
        Dim code As String = resp.ResponseCode

        If code = 200 Then
            Dim data As List(Of Object) = resp.ResponseData
            If data.Count > 0 Then
                ' Usar el primer elemento para crear las columnas automáticamente
                Dim jsonData As Object = JsonConvert.DeserializeObject(Of Object)(data.First.ToString())

                ' Crear las columnas basadas en las propiedades del primer elemento
                For Each columnName As JProperty In jsonData
                    dt.Columns.Add(columnName.Name, Type.GetType("System.String"))
                Next

                ' Iterar sobre los demás elementos para llenar las filas
                For i As Integer = 0 To data.Count() - 1
                    Dim jsonDataValue As Object = JsonConvert.DeserializeObject(Of Object)(data(i).ToString())
                    Dim dr As DataRow = dt.NewRow

                    Dim v As Integer = 0
                    For Each columnData As JProperty In jsonDataValue
                        dr(v) = columnData.Value.ToString()
                        v += 1
                    Next

                    dt.Rows.Add(dr)
                Next
            End If
        End If

        Return dt

    End Function

    Public Function API_Get_DataTable(ByVal APIRoute As String, ByVal Body As Object) As DataTable
        GetTokenApi()

        Dim dt As DataTable = New DataTable()
        Dim client As New RestSharp.RestClient(APIRoute)
        Dim request As New RestRequest(Method.POST)
        request.AddHeader("UserName", System.Configuration.ConfigurationManager.AppSettings("UserName"))
        request.AddHeader("Token", Token)
        request.AddHeader("Content-Type", "application/json")
        request.AddJsonBody(Body)
        request.Timeout = CInt(60) * 60000

        Dim response As New RestResponse
        response = client.Execute(request)
        Dim json As String = response.Content

        Dim resp As Object = JsonConvert.DeserializeObject(Of DataOutput)(json)
        Dim code As String = resp.ResponseCode

        If code = 200 Then
            Dim data As List(Of Object) = resp.ResponseData
            If data.Count > 0 Then
                ' Usar el primer elemento para crear las columnas automáticamente
                Dim jsonData As Object = JsonConvert.DeserializeObject(Of Object)(data.First.ToString())

                ' Crear las columnas basadas en las propiedades del primer elemento
                For Each columnName As JProperty In jsonData
                    dt.Columns.Add(columnName.Name, Type.GetType("System.String"))
                Next

                ' Iterar sobre los demás elementos para llenar las filas
                For i As Integer = 0 To data.Count() - 1
                    Dim jsonDataValue As Object = JsonConvert.DeserializeObject(Of Object)(data(i).ToString())
                    Dim dr As DataRow = dt.NewRow

                    Dim v As Integer = 0
                    For Each columnData As JProperty In jsonDataValue
                        dr(v) = columnData.Value.ToString()
                        v += 1
                    Next

                    dt.Rows.Add(dr)
                Next
            End If
        End If

        Return dt
    End Function

End Class