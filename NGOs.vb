Imports System.ServiceProcess
Imports System.Configuration
Imports System.Data.SqlClient
Imports System.IO
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Text
Imports Microsoft.SqlServer.Server
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports RestSharp
Imports System.Collections.Specialized.BitVector32
Imports System.Threading
Public Class NGOs

    Private WithEvents Timer As System.Timers.Timer

    'Configuration Settings
    Private Version As String = "Version 1.0.0 (08/10/2024)"
    Private Interval As String = "3"
    Private StartTime As String = "00:00"
    Private EndTime As String = "23:59"

    Private LogPath As String = "C:\tmp"
    Private DefaultIdUser As String = "1"
    Private Airline As String = "IBERIA"


    'Change to false when service mode
    Private DebugMode As Boolean = False


    Private EmailAlerts = "No"
    Private SMTPHost As String = "correo.com.mx"
    Private SMTPPort As String = "587"
    Private SMTPSsl As String = "1"
    Private SMTPUsr As String = "correo@cooreo.com.mx"
    Private SMTPPwd As String = "Alertas01@"
    Private SMTPCc As String = ""

    Private ApiToken As String = "http://localhost:6061/APIAirlineService/Token"

    Private SelNgosApi As String = "http://localhost:6061/APIAirlineService/SelectAllNgos"
    Private IBAirshoppingApi As String = "http://localhost:6061/APIAirlineService/IBAirShopping"
    Private InsFlightOfferApi As String = "http://localhost:6061/APIAirlineService/InsertFlightOffers"

    Private SelNgosApiTimeOut As String = "2000"
    Private IBAirshoppingApiTimeOut As String = "2000"
    Private InsFlightOfferApiTimeOut As String = "2000"

    'Global variables
    Private Processing As Boolean = False
    Private Ticking As Boolean = False
    Private Session As String

    Protected Overrides Sub OnStart(ByVal args() As String)
        InitializeComponent()

        'Load Interval Configuration
        Interval = System.Configuration.ConfigurationManager.AppSettings("Interval")

        'Override Interval Configuration setting  minimum = 30 maximum = 480. Debug sets interval = 1 minute
        If Interval.ToUpper = "DEBUG" Then
            Interval = "1"
        Else
            If CInt(Interval) <= 3 Then
                Interval = "3"
            End If

            If CInt(Interval) > 480 Then
                Interval = "480"
            End If
        End If

        'Add any setting after the InitializeComponent() call
        If Timer IsNot Nothing Then
            Timer.Dispose()
        End If

        Timer = New System.Timers.Timer(CInt(Interval) * 60000)
        AddHandler Timer.Elapsed, New System.Timers.ElapsedEventHandler(AddressOf Me.timer_elapsed)
        Timer.AutoReset = False
        Me.Timer.Enabled = True

        CreateDirectories()

        'Log service start 
        Log("NGOs Service")
        Log(Version)
        Log("NGOs Service was started.")

        LoadGeneralConfiguration()

        'Uncomment next line when Debugging
        'Tick()

    End Sub

    Protected Sub CreateDirectories()
        'If not exists logfolder it will be created
        LogPath = System.Configuration.ConfigurationManager.AppSettings("LogPath")

        'Remove last slash
        If Strings.Right(LogPath, 1) = "\" Then
            LogPath = Strings.Left(LogPath, LogPath.Length - 1)
        End If

        If (Not System.IO.Directory.Exists(LogPath)) Then
            System.IO.Directory.CreateDirectory(LogPath)
        End If

    End Sub

    Protected Sub LoadGeneralConfiguration()
        'Load Interval Configuration
        Interval = System.Configuration.ConfigurationManager.AppSettings("Interval")

        'Override Interval Configuration setting  minimum = 30 maximum = 480. Debug sets interval = 1 minute
        If Interval.ToUpper() = "DEBUG" Then
            DebugMode = True

            Log("Debug mode interval active.")
            Interval = "1"
        Else
            Log("Debug mode interval not active.")

            If CInt(Interval) <= 3 Then
                Interval = "3"
            End If

            If CInt(Interval) > 480 Then
                Interval = "480"
            End If
        End If

        'Add any setting after the InitializeComponent() call
        If Timer IsNot Nothing Then
            Timer.Dispose()
        End If

        Timer = New System.Timers.Timer(CInt(Interval) * 60000)
        AddHandler Timer.Elapsed, New System.Timers.ElapsedEventHandler(AddressOf Me.timer_elapsed)
        'Timer.AutoReset = False
        Me.Timer.Enabled = True

        Log("Loading configuration.")

        Interval = System.Configuration.ConfigurationManager.AppSettings("Interval")
        StartTime = System.Configuration.ConfigurationManager.AppSettings("StartTime")
        EndTime = System.Configuration.ConfigurationManager.AppSettings("EndTime")

        Airline = System.Configuration.ConfigurationManager.AppSettings("Airline")

        LogPath = System.Configuration.ConfigurationManager.AppSettings("LogPath")

        EmailAlerts = System.Configuration.ConfigurationManager.AppSettings("EmailAlerts")

        If System.Configuration.ConfigurationManager.AppSettings("SMTPHost") <> "" Then
            SMTPHost = System.Configuration.ConfigurationManager.AppSettings("SMTPHost")
            SMTPPort = System.Configuration.ConfigurationManager.AppSettings("SMTPPort")
            SMTPSsl = System.Configuration.ConfigurationManager.AppSettings("SMTPSsl")
            SMTPUsr = System.Configuration.ConfigurationManager.AppSettings("SMTPUsr")
            SMTPPwd = System.Configuration.ConfigurationManager.AppSettings("SMTPPwd")
            SMTPCc = System.Configuration.ConfigurationManager.AppSettings("SMTPCc")
        End If

        DefaultIdUser = System.Configuration.ConfigurationManager.AppSettings("DefaultIdUser")
        ApiToken = System.Configuration.ConfigurationManager.AppSettings("ApiToken")

        SelNgosApi = System.Configuration.ConfigurationManager.AppSettings("SelNgosApi")
        IBAirshoppingApi = System.Configuration.ConfigurationManager.AppSettings("IBAirshoppingApi")
        InsFlightOfferApi = System.Configuration.ConfigurationManager.AppSettings("InsFlightOfferApi")

        SelNgosApiTimeOut = System.Configuration.ConfigurationManager.AppSettings("SelNgosApiTimeOut")
        IBAirshoppingApiTimeOut = System.Configuration.ConfigurationManager.AppSettings("IBAirshoppingApiTimeOut")
        InsFlightOfferApiTimeOut = System.Configuration.ConfigurationManager.AppSettings("InsFlightOfferApiTimeOut")
    End Sub


    Protected Overrides Sub OnStop()
        Log("NGOs Service was stopped.")
    End Sub

    Public Sub New()

    End Sub

    Public Sub timer_elapsed(sender As Object, e As EventArgs)
        If (Ticking Or Processing) Then
            Exit Sub
        End If

        Ticking = True
        Tick()
        Ticking = False
    End Sub

    Private Sub Tick()
        If Not (OnTime() Or DebugMode) Then
            Exit Sub
        End If

        CreateDirectories()

        If Processing Then
            Exit Sub
        End If

        System.Configuration.ConfigurationManager.RefreshSection("appSettings")
        Session = "QL" + Now.Year.ToString + Now.Month.ToString.PadLeft(2, "0") + Now.Day.ToString.PadLeft(2, "0") + Now.Hour.ToString.PadLeft(2, "0") + Now.Minute.ToString.PadLeft(2, "0") + Now.Second.ToString.PadLeft(2, "0")

        LoadGeneralConfiguration()
        Log("Interval Time: " + Date.Now().ToString())

        Process()
    End Sub

    Private Sub Test(ByVal args() As String)
        Me.OnStart(args)
        Console.ReadLine()
        Me.OnStop()
    End Sub

    Private Function OnTime() As Boolean
        Dim res As Boolean = False
        Dim minTime As New TimeSpan(StartTime.Split(":")(0), StartTime.Split(":")(1), 0)
        Dim maxTime As New TimeSpan(EndTime.Split(":")(0), EndTime.Split(":")(1), 0)
        Dim myDate As DateTime = Now

        If minTime > maxTime Then
            Return myDate.TimeOfDay >= minTime OrElse myDate.TimeOfDay < maxTime
        Else
            Return myDate.TimeOfDay >= minTime AndAlso myDate.TimeOfDay < maxTime
        End If

        Return res
    End Function

    Protected Sub Log(logMessage As String)
        Dim FileS As New FileStream(LogPath + "\NGOsService" + Now.ToString("yyyyMMdd") + ".txt", FileMode.Append, FileAccess.Write, FileShare.ReadWrite)

        Using w As New System.IO.StreamWriter(FileS)
            w.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
            DateTime.Now.ToLongDateString() + ":" + logMessage)
        End Using
    End Sub

    Private Sub Process()
        Processing = True

        If DebugMode Then
            Log("DebugMode is " + DebugMode.ToString())
            Processing = False
            Me.Stop()
        End If

        ''Here Code
        Log("Search Ngo")

        'I get NGO's to look for offers
        Dim DataTblNgos As DataTable = SelectInformationNGOs()

        Log("Search Offer for Ngo")
        SearchOffer(DataTblNgos)

        Log("Finish search offer")

        Processing = False
    End Sub

    Protected Function SelectInformationNGOs() As DataTable

        Dim ApiInterface As New ApiInterface()
        Dim DataTbl As New DataTable
        DataTbl = ApiInterface.API_Get_DataTable(System.Configuration.ConfigurationManager.AppSettings("SelNgosApi"))
        Return DataTbl

    End Function

    Public Sub SearchOffer(ByVal Ngos As DataTable)

        Dim ApiInterface As New ApiInterface()
        Dim Offert As New OfferModel()

        For Each ItemRow In Ngos.Rows

            Dim NgoModel As New NgoModel()
            Dim DataTblOffert As New DataTable
            NgoModel.UserID = ItemRow.ItemArray(0).ToString()
            NgoModel.NgoId = ItemRow.ItemArray(1).ToString()
            NgoModel.Airline = ItemRow.ItemArray(2).ToString()
            NgoModel.AirlineId = ItemRow.ItemArray(3).ToString()
            NgoModel.AirportCode = ItemRow.ItemArray(4).ToString()
            NgoModel.DestinationCode = ItemRow.ItemArray(5)
            NgoModel.DepartureDate = ItemRow.ItemArray(6).ToString()
            NgoModel.ReturnDate = ItemRow.ItemArray(7).ToString()
            NgoModel.TypeTrip = ItemRow.ItemArray(8)
            NgoModel.NumCompanion = ItemRow.ItemArray(9).ToString()

            If ItemRow.ItemArray(2).ToString() = "IBERIA" Then

                DataTblOffert = ApiInterface.API_Get_DataTable(System.Configuration.ConfigurationManager.AppSettings("IBAirshoppingApi"), NgoModel)

            End If

            If DataTblOffert.Rows.Count >= 1 Then
                Dim NgosOffert As New List(Of OfferModel)

                For Each ItemRow2 In DataTblOffert.Rows

                    Offert = New OfferModel()

                    Offert.NgoId = ItemRow2.ItemArray(0).ToString()
                    Offert.ResponseId = ItemRow2.ItemArray(1).ToString()
                    Offert.OfferId = ItemRow2.ItemArray(2).ToString()
                    Offert.OfferItemId = ItemRow2.ItemArray(3).ToString()
                    Offert.Expiration = ItemRow2.ItemArray(4).ToString()
                    Offert.OriginPrice = ItemRow2.ItemArray(5).ToString()
                    Offert.Currency = ItemRow2.ItemArray(6).ToString()
                    Offert.Scales = ItemRow2.ItemArray(7).ToString()
                    Offert.CabinType = ItemRow2.ItemArray(8).ToString()
                    Offert.DepartureDate = ItemRow2.ItemArray(9).ToString()
                    Offert.ReturnDate = ItemRow2.ItemArray(10).ToString()


                    NgosOffert.Add(Offert)


                Next

                Dim InsFlight As New InserOffertInputModel

                InsFlight.NgoDetail = NgoModel
                InsFlight.NgoOffert = NgosOffert

                Log(CStr(DataTblOffert.Rows.Count) + " flight offers were found for the ngoid: " + CStr(ItemRow.ItemArray(1).ToString()))

                Log(" Insert offert in data base for ngoid: " + CStr(ItemRow.ItemArray(1).ToString()))
                Dim InserOffert As DataOutput = ApiInterface.ApiPostData(System.Configuration.ConfigurationManager.AppSettings("InsFlightOfferApi"), InsFlight)
            End If
        Next

    End Sub

End Class
