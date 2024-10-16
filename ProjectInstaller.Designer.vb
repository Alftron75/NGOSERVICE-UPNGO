<System.ComponentModel.RunInstaller(True)> Partial Class ProjectInstaller
    Inherits System.Configuration.Install.Installer

    'Installer overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Component Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Component Designer
    'It can be modified using the Component Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.NGOsServiceProcessInstaller = New System.ServiceProcess.ServiceProcessInstaller()
        Me.NGOsServiceInstaller = New System.ServiceProcess.ServiceInstaller()
        '
        'NGOsServiceProcessInstaller
        '
        Me.NGOsServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem
        Me.NGOsServiceProcessInstaller.Password = Nothing
        Me.NGOsServiceProcessInstaller.Username = Nothing
        '
        'NGOsServiceInstaller
        '
        Me.NGOsServiceInstaller.Description = "UPNGO Airline Flight Offers Search Service"
        Me.NGOsServiceInstaller.DisplayName = "NGOs Service"
        Me.NGOsServiceInstaller.ServiceName = "NGOs"
        '
        'ProjectInstaller
        '
        Me.Installers.AddRange(New System.Configuration.Install.Installer() {Me.NGOsServiceProcessInstaller, Me.NGOsServiceInstaller})

    End Sub

    Friend WithEvents NGOsServiceProcessInstaller As ServiceProcess.ServiceProcessInstaller
    Friend WithEvents NGOsServiceInstaller As ServiceProcess.ServiceInstaller
End Class
