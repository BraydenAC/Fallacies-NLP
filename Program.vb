Imports Gtk
Imports System.Net.Http
Imports System.Text
Imports System.Threading.Tasks
Imports System.Text.Json
Imports System.Diagnostics

Module Program
    Dim login_vbox As VBox
    Dim ui_vbox As VBox
    Dim window As Window

    Private Async Function CallPythonEcho(text As String) As Task(Of String)
        Using client As New HttpClient()
            Dim json = "{""message"":""" & text & """}"
            Dim content = New StringContent(json, Encoding.UTF8, "application/json")
            Dim response = Await client.PostAsync("http://localhost:5005/echo", content)
            Dim responseString = Await response.Content.ReadAsStringAsync()
            Return responseString
        End Using
    End Function

    Private Async Function CallPythonAnalyze(text As String) As Task(Of String)
        Using client As New HttpClient()
            Dim json = "{""text"":""" & text & """}"
            Dim content = New StringContent(json, Encoding.UTF8, "application/json")
            Dim response = Await client.PostAsync("http://localhost:5005/analyze", content)
            Dim responseString = Await response.Content.ReadAsStringAsync()

            ' Extract just the "result" field from JSON
            Dim doc = JsonDocument.Parse(responseString)
            Return doc.RootElement.GetProperty("result").GetString()
        End Using
    End Function

    Private Async Function CallPythonLogin(text As String) As Task(Of Integer)
    Using client As New HttpClient()
        Try
            Dim json = "{""text"":""" & text & """}"
            Dim content = New StringContent(json, Encoding.UTF8, "application/json")
            Dim response = Await client.PostAsync("http://localhost:5005/login", content)

            If response.IsSuccessStatusCode Then
                Dim responseString = Await response.Content.ReadAsStringAsync()
                Console.WriteLine("Response: " & responseString) ' Log the raw response for debugging

                ' Extract just the "result" field from JSON
                Dim doc = JsonDocument.Parse(responseString)
                Return doc.RootElement.GetProperty("result").GetInt32()
            Else
                ' Log the error status and response for debugging
                Console.WriteLine("Error: " & response.StatusCode)
                Return -1 ' Return -1 or some error code to indicate failure
            End If
        Catch ex As Exception
            ' Log the exception message if something goes wrong
            Console.WriteLine("Exception during login request: " & ex.Message)
            Return -1
        End Try
    End Using
End Function

    Sub Main(args As String())
        Application.Init() ' Initialize GTK#
        Dim pythonServer As Process = Nothing

        Try
            ' Check for orphaned server on port 5005 and kill if found
            Dim orphanedProcess As Process = GetOrphanedServerOnPort(5005)
            If orphanedProcess IsNot Nothing Then
                orphanedProcess.Kill()
                Console.WriteLine("Orphaned Python server killed.")
            End If

            ' Start a new Python server
            Dim psi As New ProcessStartInfo()
            psi.FileName = "python3"
            psi.Arguments = "server.py"
            psi.UseShellExecute = False
            psi.CreateNoWindow = True
            pythonServer = Process.Start(psi)
            Console.WriteLine("Python server started.")
        Catch ex As Exception
            Console.WriteLine("Error starting Python server: " & ex.Message)
        End Try

        ' Create a window
        window = New Window("Visual Basic NLP Project")
        window.SetDefaultSize(500, 500)
        window.SetPosition(WindowPosition.Center)
        AddHandler window.DeleteEvent, Sub(o, args2)
                                        If pythonServer IsNot Nothing AndAlso Not pythonServer.HasExited Then
                                            Try
                                                pythonServer.Kill()
                                                Console.WriteLine("Python server killed.")
                                            Catch ex As Exception
                                                Console.WriteLine("Failed to kill server: " & ex.Message)
                                            End Try
                                        End If
                                        Application.Quit()
                                    End Sub

        'Create vbox
        login_vbox = New VBox(False, 10)
        login_vbox.BorderWidth = 10
        ui_vbox = New VBox(False, 10)
        ui_vbox.BorderWidth = 10

        ' Create an input field
        Dim inputField As New Entry()
        inputField.PlaceHolderText = "Type here..."

        Dim passwordField As New Entry()
        passwordField.PlaceHolderText = "Type token here..."

        Dim reqestLogin As New Label("Please enter your authentication HF token")
        Dim loginInstructions As New Label("
            To get a Llama Authorized token, follow these steps:

            - Go to https://huggingface.co/meta-llama/Llama-3.2-1B-Instruct
            - Input your details into the provided form
            - Wait for a confirmation email
            - Click on your profile picture in the top right, then click on Access Tokens
            - Click Create New Token, then make a read-only token
            - Place that string of characters in the input field above!")
        Dim outputField As New TextView()
        outputField.SetSizeRequest(480, 425)
        outputField.editable = False
        outputField.WrapMode = WrapMode.Word

        Dim doLogin As New Button("Login")
        AddHandler doLogin.Clicked, Async Sub(sender, e)
                                            Console.WriteLine("Login button clicked")
                                            Dim response As Integer = Await CallPythonLogin(passwordField.Text)
                                            If response = 1 Then
                                                loginSuccess()
                                            Else
                                                Dim dialog As New MessageDialog(window, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok, "Login Failed!")
                                                dialog.Run()
                                                dialog.Destroy()
                                            End If
                                        End Sub

        ' Create a button
        Dim checkFallacies As New Button("Check for Fallacies")
        AddHandler checkFallacies.Clicked, Async Sub(sender, e)
                                            outputField.Buffer.Text = "（´-`）. ｡oO（Thinking)"
                                            Dim response As String = Await CallPythonAnalyze(inputField.Text)
                                            outputField.Buffer.Text = response
                                        End Sub

        ' Create the vbox layout
        login_vbox.PackStart(reqestLogin, False, False, 0)
        login_vbox.PackStart(passwordField, False, False, 0)
        login_vbox.PackStart(doLogin, False, False, 0)
        login_vbox.PackStart(loginInstructions, False, False, 0)

        ui_vbox.PackStart(inputField, False, False, 0)
        ui_vbox.PackStart(checkFallacies, False, False, 0)
        ui_vbox.PackStart(outputField, False, False, 0)
        
        ' Add elements to the window
        window.Add(login_vbox)

        ' Show everything
        window.ShowAll()

        ' Run the application
        Application.Run()
    End Sub

    ' Function to find orphaned server on the specified port
    Private Function GetOrphanedServerOnPort(port As Integer) As Process
        Try
            Dim startInfo As New ProcessStartInfo("netstat", $"-aon | findstr :{port}")
            startInfo.RedirectStandardOutput = True
            startInfo.UseShellExecute = False
            startInfo.CreateNoWindow = True
            Dim netstatProcess As Process = Process.Start(startInfo)
            Dim output As String = netstatProcess.StandardOutput.ReadToEnd()
            netstatProcess.WaitForExit()

            If Not String.IsNullOrEmpty(output) Then
                ' Extract the PID of the process using the port
                Dim pid As String = output.Split(New Char() {" "c}, StringSplitOptions.RemoveEmptyEntries).Last()
                pid = New String(pid.Where(Function(c) Char.IsDigit(c)).ToArray())
                Dim runningProcess As Process = Process.GetProcessById(Integer.Parse(pid))
                Return runningProcess
            End If
        Catch ex As Exception
            Console.WriteLine("Error checking for orphaned server: " & ex.Message)
        End Try
        Return Nothing
    End Function

    Private Sub loginSuccess()
        ' Check if login_vbox is initialized
        If login_vbox Is Nothing Then
            Console.WriteLine("login_vbox is not initialized.")
            Return
        End If

        ' Check if ui_vbox is initialized
        If ui_vbox Is Nothing Then
            Console.WriteLine("ui_vbox is not initialized.")
            Return
        End If

        ' Check if window is initialized
        If window Is Nothing Then
            Console.WriteLine("window is not initialized.")
            Return
        End If

        ' If all objects are initialized, proceed with the layout changes
        window.Remove(login_vbox)
        window.Add(ui_vbox)
        window.ShowAll()
    End Sub

End Module
