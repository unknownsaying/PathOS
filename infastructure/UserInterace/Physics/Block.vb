Imports Emgu.CV
Imports Emgu.CV.CvEnum
Imports Emgu.CV.Structure
Imports Emgu.CV.UI
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Collections.Generic
Imports System.Math

Public Class Form1
    ' Camera and video
    Private WithEvents capture As VideoCapture
    Private WithEvents processingTimer As New Timer()
    Private frameCount As Integer = 0
    Private fps As Double = 30
    
    ' Physics simulation
    Private objects As New List(Of PhysicsObject)
    Private gravity As Double = 9.81
    Private simulationTime As Double = 0
    Private lastUpdateTime As DateTime = DateTime.Now
    
    ' Object detection
    Private prevFrame As Mat
    Private motionHistory As New Mat
    Private contours As New List(Of VectorOfPoint)
    Private minObjectArea As Integer = 500
    Private maxObjectArea As Integer = 50000
    
    ' UI Controls
    Private WithEvents picOriginal As New PictureBox
    Private WithEvents picMotion As New PictureBox
    Private WithEvents picTrajectory As New PictureBox
    Private WithEvents btnStart As New Button
    Private WithEvents btnStop As New Button
    Private WithEvents btnAddObject As New Button
    Private WithEvents btnReset As New Button
    Private WithEvents lstObjects As New ListBox
    Private WithEvents chkShowVectors As New CheckBox
    Private WithEvents chkShowForces As New CheckBox
    Private WithEvents chkShowPaths As New CheckBox
    Private WithEvents lblInfo As New Label
    
    ' Trajectory tracking
    Private trajectoryPoints As New Dictionary(Of Integer, List(Of Point))
    Private trajectoryMat As Mat
    
    Public Sub New()
        InitializeComponent()
        SetupCamera()
        InitializePhysicsObjects()
        SetupUI()
    End Sub
    
    Private Sub InitializeComponent()
        Me.Text = "Newtonian Physics Block Detection & Motion Analysis"
        Me.WindowState = FormWindowState.Maximized
        Me.DoubleBuffered = True
        
        ' Timer setup
        processingTimer.Interval = 33 ' ~30 FPS
    End Sub
    
    Private Sub SetupUI()
        ' Create split containers for better layout
        Dim splitMain As New SplitContainer With {
            .Dock = DockStyle.Fill,
            .Orientation = Orientation.Horizontal,
            .SplitterDistance = 300
        }
        
        Dim splitLeft As New SplitContainer With {
            .Dock = DockStyle.Fill,
            .Orientation = Orientation.Vertical,
            .SplitterDistance = 250
        }
        
        ' PictureBox setup
        picOriginal.SizeMode = PictureBoxSizeMode.Zoom
        picMotion.SizeMode = PictureBoxSizeMode.Zoom
        picTrajectory.SizeMode = PictureBoxSizeMode.Zoom
        
        ' Button setup
        btnStart.Text = "Start Camera"
        btnStop.Text = "Stop Camera"
        btnAddObject.Text = "Add Simulated Object"
        btnReset.Text = "Reset Simulation"
        
        btnStart.Dock = DockStyle.Top
        btnStop.Dock = DockStyle.Top
        btnAddObject.Dock = DockStyle.Top
        btnReset.Dock = DockStyle.Top
        
        ' Checkboxes
        chkShowVectors.Text = "Show Velocity Vectors"
        chkShowForces.Text = "Show Force Vectors"
        chkShowPaths.Text = "Show Trajectory Paths"
        chkShowVectors.Checked = True
        chkShowForces.Checked = True
        chkShowPaths.Checked = True
        
        ' ListBox
        lstObjects.Dock = DockStyle.Fill
        
        ' Info label
        lblInfo.Dock = DockStyle.Bottom
        lblInfo.Height = 50
        lblInfo.BackColor = Color.Black
        lblInfo.ForeColor = Color.White
        lblInfo.Font = New Font("Consolas", 10)
        
        ' Layout containers
        Dim leftPanel As New Panel With {.Dock = DockStyle.Fill}
        leftPanel.Controls.AddRange({lstObjects, lblInfo})
        
        Dim rightPanel As New Panel With {.Dock = DockStyle.Fill}
        Dim tabControl As New TabControl With {.Dock = DockStyle.Fill}
        
        Dim tab1 As New TabPage("Original View")
        Dim tab2 As New TabPage("Motion Detection")
        Dim tab3 As New TabPage("Trajectory Analysis")
        
        tab1.Controls.Add(picOriginal)
        tab2.Controls.Add(picMotion)
        tab3.Controls.Add(picTrajectory)
        
        tabControl.TabPages.AddRange({tab1, tab2, tab3})
        rightPanel.Controls.Add(tabControl)
        
        splitLeft.Panel1.Controls.Add(leftPanel)
        splitLeft.Panel2.Controls.Add(rightPanel)
        
        ' Control panel
        Dim controlPanel As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 120
        }
        
        Dim flowLayout As New FlowLayoutPanel With {
            .Dock = DockStyle.Fill,
            .FlowDirection = FlowDirection.LeftToRight,
            .WrapContents = True
        }
        
        flowLayout.Controls.AddRange({
            btnStart, btnStop, btnAddObject, btnReset,
            chkShowVectors, chkShowForces, chkShowPaths
        })
        
        controlPanel.Controls.Add(flowLayout)
        
        splitMain.Panel1.Controls.AddRange({controlPanel, splitLeft})
        
        Me.Controls.Add(splitMain)
    End Sub
    
    Private Sub SetupCamera()
        Try
            capture = New VideoCapture(0) ' Use default camera
            If capture.IsOpened Then
                ' Initialize motion history
                motionHistory = New Mat(capture.Height, capture.Width, DepthType.Cv32F, 1)
                trajectoryMat = New Mat(capture.Height, capture.Width, DepthType.Cv8U, 3)
                CvInvoke.PutText(trajectoryMat, "Trajectory Analysis", New Point(10, 30),
                               FontFace.HersheySimplex, 1, New MCvScalar(255, 255, 255), 2)
            Else
                MessageBox.Show("Camera not found. Using simulation mode.")
                ' Initialize with default size
                motionHistory = New Mat(480, 640, DepthType.Cv32F, 1)
                trajectoryMat = New Mat(480, 640, DepthType.Cv8U, 3)
            End If
        Catch ex As Exception
            MessageBox.Show($"Camera error: {ex.Message}")
        End Try
    End Sub
    
    Private Sub InitializePhysicsObjects()
        ' Create some initial simulated objects with Newtonian physics
        objects.Clear()
        trajectoryPoints.Clear()
        
        ' Object 1: Moving right with constant velocity (Newton's 1st Law)
        objects.Add(New PhysicsObject With {
            .Id = 1,
            .Position = New PointF(100, 100),
            .Velocity = New PointF(2, 0),
            .Acceleration = New PointF(0, 0),
            .Mass = 1.0,
            .Color = Color.Red,
            .Radius = 20,
            .IsStatic = False
        })
        
        ' Object 2: Accelerating downward (Newton's 2nd Law: F=ma)
        objects.Add(New PhysicsObject With {
            .Id = 2,
            .Position = New PointF(200, 100),
            .Velocity = New PointF(0, 0),
            .Acceleration = New PointF(0, gravity),
            .Mass = 2.0,
            .Color = Color.Blue,
            .Radius = 25,
            .IsStatic = False
        })
        
        ' Object 3: Static object (Newton's 3rd Law: Reaction force)
        objects.Add(New PhysicsObject With {
            .Id = 3,
            .Position = New PointF(400, 300),
            .Velocity = New PointF(0, 0),
            .Acceleration = New PointF(0, 0),
            .Mass = 100.0,
            .Color = Color.Green,
            .Radius = 40,
            .IsStatic = True
        })
        
        ' Initialize trajectory tracking
        For Each obj In objects
            trajectoryPoints(obj.Id) = New List(Of Point)
        Next
        
        UpdateObjectList()
    End Sub
    
    Private Sub UpdateObjectList()
        lstObjects.Items.Clear()
        For Each obj In objects
            Dim info As String = $"ID: {obj.Id} | Pos: ({obj.Position.X:F1}, {obj.Position.Y:F1}) | " &
                               $"Vel: ({obj.Velocity.X:F1}, {obj.Velocity.Y:F1}) | " &
                               $"Mass: {obj.Mass:F1}kg"
            lstObjects.Items.Add(info)
        Next
    End Sub
    
    Private Sub BtnStart_Click(sender As Object, e As EventArgs) Handles btnStart.Click
        If capture IsNot Nothing AndAlso capture.IsOpened Then
            processingTimer.Start()
            btnStart.Enabled = False
            btnStop.Enabled = True
        End If
    End Sub
    
    Private Sub BtnStop_Click(sender As Object, e As EventArgs) Handles btnStop.Click
        processingTimer.Stop()
        btnStart.Enabled = True
        btnStop.Enabled = False
    End Sub
    
    Private Sub BtnAddObject_Click(sender As Object, e As EventArgs) Handles btnAddObject.Click
        Dim rnd As New Random()
        Dim newId As Integer = objects.Count + 1
        
        Dim newObj As New PhysicsObject With {
            .Id = newId,
            .Position = New PointF(rnd.Next(50, 600), rnd.Next(50, 400)),
            .Velocity = New PointF(rnd.Next(-3, 3), rnd.Next(-3, 3)),
            .Acceleration = New PointF(0, gravity),
            .Mass = rnd.NextDouble() * 5 + 1,
            .Color = Color.FromArgb(rnd.Next(200, 255), rnd.Next(200, 255), rnd.Next(200, 255)),
            .Radius = rnd.Next(15, 35),
            .IsStatic = False
        }
        
        objects.Add(newObj)
        trajectoryPoints(newId) = New List(Of Point)
        UpdateObjectList()
    End Sub
    
    Private Sub BtnReset_Click(sender As Object, e As EventArgs) Handles btnReset.Click
        InitializePhysicsObjects()
        trajectoryMat.SetTo(New MCvScalar(0, 0, 0))
        CvInvoke.PutText(trajectoryMat, "Trajectory Analysis", New Point(10, 30),
                       FontFace.HersheySimplex, 1, New MCvScalar(255, 255, 255), 2)
        picTrajectory.Image = trajectoryMat.ToBitmap()
        simulationTime = 0
    End Sub
    
    Private Sub ProcessingTimer_Tick(sender As Object, e As EventArgs) Handles processingTimer.Tick
        If capture IsNot Nothing AndAlso capture.IsOpened Then
            ProcessFrame()
        Else
            SimulatePhysics()
        End If
        UpdatePhysics()
        RenderGraphics()
        UpdateInfoDisplay()
    End Sub
    
    Private Sub ProcessFrame()
        Using frame As New Mat()
            capture.Read(frame)
            
            If frame.IsEmpty Then Exit Sub
            
            ' Resize for display
            Dim displayFrame As New Mat()
            CvInvoke.Resize(frame, displayFrame, New Size(picOriginal.Width, picOriginal.Height))
            
            ' Detect motion
            DetectMotion(frame)
            
            ' Detect objects/blocks
            DetectObjects(frame)
            
            ' Display original frame
            picOriginal.Image = displayFrame.ToBitmap()
            
            ' Update previous frame
            If prevFrame Is Nothing Then
                prevFrame = frame.Clone()
            Else
                frame.CopyTo(prevFrame)
            End If
        End Using
    End Sub
    
    Private Sub DetectMotion(inputFrame As Mat)
        ' Convert to grayscale
        Dim grayFrame As New Mat()
        CvInvoke.CvtColor(inputFrame, grayFrame, ColorConversion.Bgr2Gray)
        
        ' Apply Gaussian blur
        CvInvoke.GaussianBlur(grayFrame, grayFrame, New Size(21, 21), 0)
        
        If prevFrame IsNot Nothing Then
            ' Compute absolute difference
            Dim diff As New Mat()
            CvInvoke.Absdiff(prevFrame, grayFrame, diff)
            
            ' Threshold
            Dim thresh As New Mat()
            CvInvoke.Threshold(diff, thresh, 25, 255, ThresholdType.Binary)
            
            ' Dilate to fill gaps
            CvInvoke.Dilate(thresh, thresh, Nothing, Point.Empty, 2, BorderType.Constant, New MCvScalar(0))
            
            ' Update motion history
            Dim timestamp As Single = CSng(Environment.TickCount / 1000.0)
            CvInvoke.Motempl.UpdateMotionHistory(thresh, motionHistory, timestamp, 0.5)
            
            ' Convert motion history to displayable format
            Dim motionDisplay As New Mat()
            CvInvoke.Normalize(motionHistory, motionDisplay, 0, 255, NormType.MinMax, DepthType.Cv8U)
            CvInvoke.CvtColor(motionDisplay, motionDisplay, ColorConversion.Gray2Bgr)
            
            ' Resize for display
            Dim resizedMotion As New Mat()
            CvInvoke.Resize(motionDisplay, resizedMotion, New Size(picMotion.Width, picMotion.Height))
            
            picMotion.Image = resizedMotion.ToBitmap()
            
            thresh.Dispose()
            diff.Dispose()
        End If
        
        grayFrame.Dispose()
    End Sub
    
    Private Sub DetectObjects(frame As Mat)
        ' Convert to HSV for better color segmentation
        Dim hsvFrame As New Mat()
        CvInvoke.CvtColor(frame, hsvFrame, ColorConversion.Bgr2Hsv)
        
        ' Define color ranges for block detection (adjust as needed)
        Dim lowerRed1 As New ScalarArray(New MCvScalar(0, 120, 70))
        Dim upperRed1 As New ScalarArray(New MCvScalar(10, 255, 255))
        Dim lowerRed2 As New ScalarArray(New MCvScalar(170, 120, 70))
        Dim upperRed2 As New ScalarArray(New MCvScalar(180, 255, 255))
        
        Dim mask1 As New Mat()
        Dim mask2 As New Mat()
        Dim maskRed As New Mat()
        
        CvInvoke.InRange(hsvFrame, lowerRed1, upperRed1, mask1)
        CvInvoke.InRange(hsvFrame, lowerRed2, upperRed2, mask2)
        CvInvoke.BitwiseOr(mask1, mask2, maskRed)
        
        ' Find contours
        Dim tempContours As New VectorOfVectorOfPoint()
        Dim hierarchy As New Mat()
        
        CvInvoke.FindContours(maskRed, tempContours, hierarchy, RetrType.External, ChainApproxMethod.ChainApproxSimple)
        
        ' Filter contours by area and shape
        contours.Clear()
        For i As Integer = 0 To tempContours.Size - 1
            Dim contour As VectorOfPoint = tempContours(i)
            Dim area As Double = CvInvoke.ContourArea(contour)
            
            If area > minObjectArea And area < maxObjectArea Then
                ' Check for rectangular shape
                Dim approx As New VectorOfPoint()
                Dim peri As Double = CvInvoke.ArcLength(contour, True)
                CvInvoke.ApproxPolyDP(contour, approx, 0.02 * peri, True)
                
                If approx.Size = 4 Then ' Likely a rectangle
                    contours.Add(contour)
                    
                    ' Calculate moments for center
                    Dim moments As Moments = CvInvoke.Moments(contour)
                    If moments.M00 > 0 Then
                        Dim cx As Integer = CInt(moments.M10 / moments.M00)
                        Dim cy As Integer = CInt(moments.M01 / moments.M00)
                        
                        ' Create or update physics object
                        UpdateOrCreatePhysicsObject(cx, cy, area)
                    End If
                End If
            End If
        Next
        
        hsvFrame.Dispose()
        mask1.Dispose()
        mask2.Dispose()
        maskRed.Dispose()
    End Sub
    
    Private Sub UpdateOrCreatePhysicsObject(x As Integer, y As Integer, area As Double)
        ' Find nearest existing object
        Dim minDist As Double = Double.MaxValue
        Dim nearestObj As PhysicsObject = Nothing
        
        For Each obj In objects
            If Not obj.IsStatic Then
                Dim dist As Double = Sqrt((obj.Position.X - x) ^ 2 + (obj.Position.Y - y) ^ 2)
                If dist < 50 And dist < minDist Then ' Within 50 pixels
                    minDist = dist
                    nearestObj = obj
                End If
            End If
        Next
        
        If nearestObj IsNot Nothing Then
            ' Update existing object position and calculate velocity
            Dim dt As Double = 1.0 / fps
            Dim vx As Single = (x - nearestObj.Position.X) / CSng(dt)
            Dim vy As Single = (y - nearestObj.Position.Y) / CSng(dt)
            
            nearestObj.Velocity = New PointF(vx, vy)
            nearestObj.Position = New PointF(x, y)
            
            ' Add to trajectory
            trajectoryPoints(nearestObj.Id).Add(New Point(x, y))
            If trajectoryPoints(nearestObj.Id).Count > 100 Then
                trajectoryPoints(nearestObj.Id).RemoveAt(0)
            End If
        Else
            ' Create new object
            Dim newId As Integer = objects.Count + 1
            Dim newObj As New PhysicsObject With {
                .Id = newId,
                .Position = New PointF(x, y),
                .Velocity = New PointF(0, 0),
                .Acceleration = New PointF(0, gravity),
                .Mass = area / 1000.0, ' Mass proportional to area
                .Color = Color.FromArgb(255, 255, 0, 0),
                .Radius = CInt(Sqrt(area / Math.PI)),
                .IsStatic = False
            }
            
            objects.Add(newObj)
            trajectoryPoints(newId) = New List(Of Point) From {New Point(x, y)}
        End If
    End Sub
    
    Private Sub SimulatePhysics()
        ' If no camera, simulate object motion
        For Each obj In objects
            If Not obj.IsStatic Then
                ' Update position based on velocity
                obj.Position = New PointF(
                    obj.Position.X + obj.Velocity.X,
                    obj.Position.Y + obj.Velocity.Y)
                
                ' Update velocity based on acceleration
                obj.Velocity = New PointF(
                    obj.Velocity.X + obj.Acceleration.X,
                    obj.Velocity.Y + obj.Acceleration.Y)
                
                ' Boundary collision
                Dim width As Integer = If(picOriginal.Image IsNot Nothing, picOriginal.Image.Width, 640)
                Dim height As Integer = If(picOriginal.Image IsNot Nothing, picOriginal.Image.Height, 480)
                
                If obj.Position.X < obj.Radius Or obj.Position.X > width - obj.Radius Then
                    obj.Velocity = New PointF(-obj.Velocity.X * 0.8F, obj.Velocity.Y)
                    obj.Position = New PointF(
                        Math.Max(obj.Radius, Math.Min(width - obj.Radius, obj.Position.X)),
                        obj.Position.Y)
                End If
                
                If obj.Position.Y < obj.Radius Or obj.Position.Y > height - obj.Radius Then
                    obj.Velocity = New PointF(obj.Velocity.X, -obj.Velocity.Y * 0.8F)
                    obj.Position = New PointF(
                        obj.Position.X,
                        Math.Max(obj.Radius, Math.Min(height - obj.Radius, obj.Position.Y)))
                End If
                
                ' Add to trajectory
                trajectoryPoints(obj.Id).Add(New Point(CInt(obj.Position.X), CInt(obj.Position.Y)))
                If trajectoryPoints(obj.Id).Count > 100 Then
                    trajectoryPoints(obj.Id).RemoveAt(0)
                End If
            End If
        Next
    End Sub
    
    Private Sub UpdatePhysics()
        ' Apply Newton's Laws
        Dim currentTime As DateTime = DateTime.Now
        Dim dt As Double = (currentTime - lastUpdateTime).TotalSeconds
        lastUpdateTime = currentTime
        simulationTime += dt
        
        ' Apply forces and collisions (Newton's 3rd Law)
        For i As Integer = 0 To objects.Count - 1
            For j As Integer = i + 1 To objects.Count - 1
                CheckCollision(objects(i), objects(j))
            Next
        Next
    End Sub
    
    Private Sub CheckCollision(obj1 As PhysicsObject, obj2 As PhysicsObject)
        Dim dx As Single = obj1.Position.X - obj2.Position.X
        Dim dy As Single = obj1.Position.Y - obj2.Position.Y
        Dim distance As Single = Sqrt(dx * dx + dy * dy)
        Dim minDistance As Single = obj1.Radius + obj2.Radius
        
        If distance < minDistance And distance > 0 Then
            ' Collision detected - apply Newton's 3rd Law
            Dim normalX As Single = dx / distance
            Dim normalY As Single = dy / distance
            
            ' Relative velocity
            Dim relVelX As Single = obj1.Velocity.X - obj2.Velocity.X
            Dim relVelY As Single = obj1.Velocity.Y - obj2.Velocity.Y
            
            ' Velocity along normal
            Dim velAlongNormal As Single = relVelX * normalX + relVelY * normalY
            
            ' Do not resolve if velocities are separating
            If velAlongNormal > 0 Then Return
            
            ' Calculate impulse scalar (elastic collision)
            Dim restitution As Single = 0.8F
            Dim j As Single = -(1 + restitution) * velAlongNormal
            j /= (1 / obj1.Mass + 1 / obj2.Mass)
            
            ' Apply impulse
            Dim impulseX As Single = j * normalX
            Dim impulseY As Single = j * normalY
            
            If Not obj1.IsStatic Then
                obj1.Velocity = New PointF(
                    obj1.Velocity.X + impulseX / obj1.Mass,
                    obj1.Velocity.Y + impulseY / obj1.Mass)
            End If
            
            If Not obj2.IsStatic Then
                obj2.Velocity = New PointF(
                    obj2.Velocity.X - impulseX / obj2.Mass,
                    obj2.Velocity.Y - impulseY / obj2.Mass)
            End If
            
            ' Position correction
            Dim percent As Single = 0.2F
            Dim slop As Single = 0.01F
            Dim correction As Single = Math.Max(distance - minDistance + slop, 0) / 
                                      (1 / obj1.Mass + 1 / obj2.Mass) * percent
            
            Dim correctionX As Single = correction * normalX
            Dim correctionY As Single = correction * normalY
            
            If Not obj1.IsStatic Then
                obj1.Position = New PointF(
                    obj1.Position.X + correctionX / obj1.Mass,
                    obj1.Position.Y + correctionY / obj1.Mass)
            End If
            
            If Not obj2.IsStatic Then
                obj2.Position = New PointF(
                    obj2.Position.X - correctionX / obj2.Mass,
                    obj2.Position.Y - correctionY / obj2.Mass)
            End If
        End If
    End Sub
    
    Private Sub RenderGraphics()
        ' Create display images
        Dim width As Integer = 640
        Dim height As Integer = 480
        
        If capture IsNot Nothing AndAlso capture.IsOpened Then
            width = capture.Width
            height = capture.Height
        End If
        
        Dim displayImage As New Mat(height, width, DepthType.Cv8U, 3)
        displayImage.SetTo(New MCvScalar(40, 40, 40))
        
        ' Draw grid for reference
        DrawGrid(displayImage)
        
        ' Draw all objects
        For Each obj In objects
            DrawPhysicsObject(displayImage, obj)
        Next
        
        ' Draw contours if available
        DrawContours(displayImage)
        
        ' Update trajectory display
        UpdateTrajectoryDisplay()
        
        ' Display in picture boxes
        picOriginal.Image = displayImage.ToBitmap()
        displayImage.Dispose()
    End Sub
    
    Private Sub DrawGrid(mat As Mat)
        Dim gridSize As Integer = 50
        Dim color As New MCvScalar(80, 80, 80)
        
        For x As Integer = 0 To mat.Width Step gridSize
            CvInvoke.Line(mat, New Point(x, 0), New Point(x, mat.Height), color, 1)
        Next
        
        For y As Integer = 0 To mat.Height Step gridSize
            CvInvoke.Line(mat, New Point(0, y), New Point(mat.Width, y), color, 1)
        Next
        
        ' Draw coordinate axes
        CvInvoke.Line(mat, New Point(10, 10), New Point(100, 10), New MCvScalar(255, 0, 0), 2)
        CvInvoke.Line(mat, New Point(10, 10), New Point(10, 100), New MCvScalar(0, 255, 0), 2)
        
        CvInvoke.PutText(mat, "X", New Point(105, 15), FontFace.HersheySimplex, 
                       0.5, New MCvScalar(255, 0, 0), 1)
        CvInvoke.PutText(mat, "Y", New Point(5, 105), FontFace.HersheySimplex, 
                       0.5, New MCvScalar(0, 255, 0), 1)
    End Sub
    
    Private Sub DrawPhysicsObject(mat As Mat, obj As PhysicsObject)
        Dim center As New Point(CInt(obj.Position.X), CInt(obj.Position.Y))
        Dim radius As Integer = obj.Radius
        
        ' Draw object
        Dim color As New MCvScalar(obj.Color.B, obj.Color.G, obj.Color.R)
        CvInvoke.Circle(mat, center, radius, color, -1)
        
        ' Draw object border
        CvInvoke.Circle(mat, center, radius, New MCvScalar(255, 255, 255), 2)
        
        ' Draw ID label
        CvInvoke.PutText(mat, $"ID: {obj.Id}", New Point(center.X - 15, center.Y - radius - 5),
                       FontFace.HersheySimplex, 0.5, New MCvScalar(255, 255, 255), 1)
        
        ' Draw mass label
        CvInvoke.PutText(mat, $"M: {obj.Mass:F1}kg", New Point(center.X - 20, center.Y + radius + 15),
                       FontFace.HersheySimplex, 0.4, New MCvScalar(200, 200, 200), 1)
        
        ' Draw velocity vector (Newton's 1st & 2nd Laws)
        If chkShowVectors.Checked AndAlso Not obj.IsStatic Then
            Dim velocityScale As Single = 5.0F
            Dim velocityEnd As New Point(
                center.X + CInt(obj.Velocity.X * velocityScale),
                center.Y + CInt(obj.Velocity.Y * velocityScale))
            
            CvInvoke.ArrowedLine(mat, center, velocityEnd, New MCvScalar(0, 255, 255), 2)
            CvInvoke.PutText(mat, $"V: ({obj.Velocity.X:F1}, {obj.Velocity.Y:F1})",
                           New Point(center.X + 10, center.Y + 10),
                           FontFace.HersheySimplex, 0.4, New MCvScalar(0, 255, 255), 1)
        End If
        
        ' Draw force vector (Newton's 2nd Law: F=ma)
        If chkShowForces.Checked AndAlso Not obj.IsStatic Then
            Dim forceX As Single = obj.Acceleration.X * obj.Mass
            Dim forceY As Single = obj.Acceleration.Y * obj.Mass
            Dim forceScale As Single = 10.0F
            
            Dim forceEnd As New Point(
                center.X + CInt(forceX * forceScale),
                center.Y + CInt(forceY * forceScale))
            
            CvInvoke.ArrowedLine(mat, center, forceEnd, New MCvScalar(255, 0, 255), 2)
            CvInvoke.PutText(mat, $"F: ({forceX:F1}, {forceY:F1})N",
                           New Point(center.X + 10, center.Y + 25),
                           FontFace.HersheySimplex, 0.4, New MCvScalar(255, 0, 255), 1)
        End If
    End Sub
    
    Private Sub DrawContours(mat As Mat)
        For Each contour In contours
            CvInvoke.DrawContours(mat, New VectorOfVectorOfPoint(contour), -1, 
                                New MCvScalar(0, 255, 0), 2)
        Next
    End Sub
    
    Private Sub UpdateTrajectoryDisplay()
        If Not chkShowPaths.Checked Then Return
        
        ' Clear trajectory mat
        trajectoryMat.SetTo(New MCvScalar(0, 0, 0))
        
        ' Draw title
        CvInvoke.PutText(trajectoryMat, "Trajectory Analysis - Newton's Laws", 
                       New Point(10, 30), FontFace.HersheySimplex, 0.7, 
                       New MCvScalar(255, 255, 255), 2)
        
        ' Draw legend
        DrawTrajectoryLegend()
        
        ' Draw each object's trajectory
        For Each kvp In trajectoryPoints
            Dim objId As Integer = kvp.Key
            Dim points As List(Of Point) = kvp.Value
            
            If points.Count > 1 Then
                Dim objColor As Color = If(objects.Any(Function(o) o.Id = objId), 
                                         objects.First(Function(o) o.Id = objId).Color, 
                                         Color.White)
                Dim trajectoryColor As New MCvScalar(objColor.B, objColor.G, objColor.R)
                
                ' Draw trajectory path
                For i As Integer = 0 To points.Count - 2
                    Dim alpha As Integer = CInt(255 * (i / points.Count))
                    Dim fadedColor As New MCvScalar(
                        trajectoryColor.V0 * alpha / 255,
                        trajectoryColor.V1 * alpha / 255,
                        trajectoryColor.V2 * alpha / 255)
                    
                    CvInvoke.Line(trajectoryMat, points(i), points(i + 1), fadedColor, 2)
                Next
                
                ' Draw position dots along trajectory
                For i As Integer = 0 To points.Count - 1 Step 5
                    CvInvoke.Circle(trajectoryMat, points(i), 3, trajectoryColor, -1)
                Next
                
                ' Calculate and display trajectory metrics
                If points.Count > 10 Then
                    DisplayTrajectoryMetrics(objId, points)
                End If
            End If
        Next
        
        picTrajectory.Image = trajectoryMat.ToBitmap()
    End Sub
    
    Private Sub DrawTrajectoryLegend()
        Dim startY As Integer = 60
        Dim lineHeight As Integer = 25
        
        CvInvoke.PutText(trajectoryMat, "Legend:", New Point(10, startY),
                       FontFace.HersheySimplex, 0.6, New MCvScalar(255, 255, 255), 1)
        
        For i As Integer = 0 To Math.Min(5, objects.Count - 1)
            Dim obj As PhysicsObject = objects(i)
            Dim yPos As Integer = startY + (i + 1) * lineHeight
            
            Dim colorBox As New Rectangle(10, yPos - 10, 10, 10)
            CvInvoke.Rectangle(trajectoryMat, colorBox,
                             New MCvScalar(obj.Color.B, obj.Color.G, obj.Color.R), -1)
            
            Dim info As String = $"ID {obj.Id}: m={obj.Mass:F1}kg, v=({obj.Velocity.X:F1},{obj.Velocity.Y:F1})"
            CvInvoke.PutText(trajectoryMat, info, New Point(25, yPos),
                           FontFace.HersheySimplex, 0.4, New MCvScalar(255, 255, 255), 1)
        Next
    End Sub
    
    Private Sub DisplayTrajectoryMetrics(objId As Integer, points As List(Of Point))
        If points.Count < 2 Then Return
        
        ' Calculate displacement (integral of velocity)
        Dim totalDistance As Double = 0
        For i As Integer = 0 To points.Count - 2
            Dim dx As Double = points(i + 1).X - points(i).X
            Dim dy As Double = points(i + 1).Y - points(i).Y
            totalDistance += Sqrt(dx * dx + dy * dy)
        Next
        
        ' Calculate average velocity
        Dim obj = objects.FirstOrDefault(Function(o) o.Id = objId)
        If obj IsNot Nothing Then
            Dim avgSpeed As Double = totalDistance / (points.Count / fps)
            
            ' Display metrics
            Dim startY As Integer = 200 + (objId - 1) * 60
            
            CvInvoke.PutText(trajectoryMat, $"Object {objId} Metrics:", 
                           New Point(400, startY), FontFace.HersheySimplex, 0.5, 
                           New MCvScalar(255, 255, 255), 1)
            
            CvInvoke.PutText(trajectoryMat, $"Distance: {totalDistance:F1} px", 
                           New Point(400, startY + 20), FontFace.HersheySimplex, 0.4, 
                           New MCvScalar(200, 200, 200), 1)
            
            CvInvoke.PutText(trajectoryMat, $"Avg Speed: {avgSpeed:F1} px/s", 
                           New Point(400, startY + 35), FontFace.HersheySimplex, 0.4, 
                           New MCvScalar(200, 200, 200), 1)
            
            CvInvoke.PutText(trajectoryMat, $"Newton's Laws Applied:", 
                           New Point(400, startY + 50), FontFace.HersheySimplex, 0.4, 
                           New MCvScalar(0, 255, 255), 1)
        End If
    End Sub
    
    Private Sub UpdateInfoDisplay()
        Dim infoText As String = $"Simulation Time: {simulationTime:F1}s | " &
                               $"Objects: {objects.Count} | " &
                               $"FPS: {fps:F1} | " &
                               "Newton's Laws: 1) Inertia, 2) F=ma, 3) Action-Reaction"
        
        lblInfo.Text = infoText
    End Sub
    
    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        MyBase.OnFormClosing(e)
        
        If capture IsNot Nothing Then
            capture.Dispose()
        End If
        
        If processingTimer IsNot Nothing Then
            processingTimer.Stop()
            processingTimer.Dispose()
        End If
    End Sub
End Class

Public Class PhysicsObject
    Public Property Id As Integer
    Public Property Position As PointF
    Public Property Velocity As PointF
    Public Property Acceleration As PointF
    Public Property Mass As Double
    Public Property Color As Color
    Public Property Radius As Integer
    Public Property IsStatic As Boolean
    
    ' Additional physics properties
    Public Property Force As PointF
    Public Property Momentum As PointF
    Public Property KineticEnergy As Double
    Public Property PotentialEnergy As Double
    
    Public Sub New()
        ' Initialize with default values
        Force = New PointF(0, 0)
        UpdateDerivedProperties()
    End Sub
    
    Public Sub UpdateDerivedProperties()
        ' Calculate momentum: p = mv
        Momentum = New PointF(CSng(Velocity.X * Mass), CSng(Velocity.Y * Mass))
        
        ' Calculate kinetic energy: KE = 0.5 * m * v^2
        Dim speedSquared As Double = Velocity.X * Velocity.X + Velocity.Y * Velocity.Y
        KineticEnergy = 0.5 * Mass * speedSquared
        
        ' Calculate force: F = ma
        Force = New PointF(CSng(Acceleration.X * Mass), CSng(Acceleration.Y * Mass))
    End Sub
    
    Public Function GetNewtonianLawsSummary() As String
        Dim laws As New List(Of String)
        
        ' Law 1: Inertia
        If Math.Abs(Force.X) < 0.001 And Math.Abs(Force.Y) < 0.001 Then
            laws.Add("1st Law: Constant velocity (no net force)")
        Else
            laws.Add("1st Law: Changing velocity (force applied)")
        End If
        
        ' Law 2: F=ma
        Dim netForce As Double = Math.Sqrt(Force.X * Force.X + Force.Y * Force.Y)
        laws.Add($"2nd Law: F={netForce:F2}N, m={Mass:F2}kg, a={Math.Sqrt(Acceleration.X^2 + Acceleration.Y^2):F2}m/s²")
        
        ' Law 3: Action-Reaction
        laws.Add("3rd Law: Forces occur in pairs")
        
        Return String.Join(" | ", laws)
    End Function
End Class

Imports System.Drawing
Imports System.Drawing.Drawing2D

Public Class NewtonLawsDisplay
    Inherits UserControl
    
    Private laws As New List(Of LawDefinition)
    
    Public Sub New()
        DoubleBuffered = True
        InitializeLaws()
    End Sub
    
    Private Sub InitializeLaws()
        laws.Add(New LawDefinition With {
            .Number = 1,
            .Title = "Law of Inertia",
            .Description = "An object at rest stays at rest and an object in motion stays in motion with the same speed and in the same direction unless acted upon by an unbalanced force.",
            .Formula = "ΣF = 0 ⇒ dv/dt = 0",
            .Color = Color.FromArgb(255, 100, 100)
        })
        
        laws.Add(New LawDefinition With {
            .Number = 2,
            .Title = "F = ma",
            .Description = "The acceleration of an object is directly proportional to the net force acting on it and inversely proportional to its mass.",
            .Formula = "F = m × a",
            .Color = Color.FromArgb(100, 255, 100)
        })
        
        laws.Add(New LawDefinition With {
            .Number = 3,
            .Title = "Action-Reaction",
            .Description = "For every action, there is an equal and opposite reaction.",
            .Formula = "F₁₂ = -F₂₁",
            .Color = Color.FromArgb(100, 100, 255)
        })
    End Sub
    
    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        
        Dim y As Integer = 10
        Dim lawHeight As Integer = (Height - 20) \ laws.Count
        
        For Each law In laws
            DrawLaw(g, law, 10, y, Width - 20, lawHeight - 10)
            y += lawHeight
        Next
    End Sub
    
    Private Sub DrawLaw(g As Graphics, law As LawDefinition, x As Integer, y As Integer, width As Integer, height As Integer)
        ' Draw background
        Using bgBrush As New SolidBrush(Color.FromArgb(40, law.Color))
            g.FillRectangle(bgBrush, x, y, width, height)
        End Using
        
        Using borderPen As New Pen(law.Color, 2)
            g.DrawRectangle(borderPen, x, y, width, height)
        End Using
        
        ' Draw law number
        Using font As New Font("Arial", 16, FontStyle.Bold)
            Using brush As New SolidBrush(law.Color)
                g.DrawString($"Law #{law.Number}", font, brush, x + 10, y + 10)
            End Using
        End Using
        
        ' Draw title
        Using font As New Font("Arial", 12, FontStyle.Bold)
            g.DrawString(law.Title, font, Brushes.White, x + 100, y + 10)
        End Using
        
        ' Draw formula
        Using font As New Font("Cambria Math", 14, FontStyle.Bold)
            Using format As New StringFormat With {.Alignment = StringAlignment.Far}
                g.DrawString(law.Formula, font, Brushes.Cyan, x + width - 10, y + 10, format)
            End Using
        End Using
        
        ' Draw description
        Using font As New Font("Arial", 9)
            Using format As New StringFormat With {.Alignment = StringAlignment.Near}
                Dim rect As New RectangleF(x + 10, y + 40, width - 20, height - 50)
                g.DrawString(law.Description, font, Brushes.LightGray, rect, format)
            End Using
        End Using
    End Sub
End Class

Public Class LawDefinition
    Public Property Number As Integer
    Public Property Title As String
    Public Property Description As String
    Public Property Formula As String
    Public Property Color As Color
End Class


Public Class GeometryIntegral
    ' Calculate area under curve using numerical integration
    Public Shared Function CalculateAreaUnderCurve(points As List(Of PointF)) As Double
        If points.Count < 2 Then Return 0
        
        Dim area As Double = 0
        For i As Integer = 0 To points.Count - 2
            area += (points(i + 1).X - points(i).X) * 
                   (points(i).Y + points(i + 1).Y) / 2
        Next
        
        Return Math.Abs(area)
    End Function
    
    ' Calculate centroid of a polygon
    Public Shared Function CalculateCentroid(points As List(Of PointF)) As PointF
        If points.Count = 0 Then Return PointF.Empty
        
        Dim cx As Double = 0
        Dim cy As Double = 0
        Dim area As Double = 0
        
        For i As Integer = 0 To points.Count - 1
            Dim j As Integer = (i + 1) Mod points.Count
            Dim cross As Double = points(i).X * points(j).Y - points(j).X * points(i).Y
            
            cx += (points(i).X + points(j).X) * cross
            cy += (points(i).Y + points(j).Y) * cross
            area += cross
        Next
        
        area /= 2
        Dim factor As Double = 1 / (6 * area)
        
        Return New PointF(CSng(cx * factor), CSng(cy * factor))
    End Function
    
    ' Calculate moment of inertia for a rectangular block
    Public Shared Function CalculateMomentOfInertia(mass As Double, width As Double, height As Double) As Double
        ' I = (1/12) * m * (w² + h²)
        Return (1 / 12) * mass * (width * width + height * height)
    End Function
    
    ' Calculate work done by force along path (W = ∫F·dx)
    Public Shared Function CalculateWorkDone(forcePath As List(Of PointF), displacementPath As List(Of PointF)) As Double
        If forcePath.Count <> displacementPath.Count Then Return 0
        
        Dim work As Double = 0
        For i As Integer = 0 To forcePath.Count - 2
            Dim dx As Double = displacementPath(i + 1).X - displacementPath(i).X
            Dim dy As Double = displacementPath(i + 1).Y - displacementPath(i).Y
            
            Dim fx As Double = (forcePath(i).X + forcePath(i + 1).X) / 2
            Dim fy As Double = (forcePath(i).Y + forcePath(i + 1).Y) / 2
            
            work += fx * dx + fy * dy
        Next
        
        Return work
    End Function
    
    ' Calculate impulse (∫F dt)
    Public Shared Function CalculateImpulse(forceTimeSeries As List(Of Tuple(Of Double, PointF))) As PointF
        Dim impulseX As Double = 0
        Dim impulseY As Double = 0
        
        For i As Integer = 0 To forceTimeSeries.Count - 2
            Dim dt As Double = forceTimeSeries(i + 1).Item1 - forceTimeSeries(i).Item1
            Dim avgForceX As Double = (forceTimeSeries(i).Item2.X + forceTimeSeries(i + 1).Item2.X) / 2
            Dim avgForceY As Double = (forceTimeSeries(i).Item2.Y + forceTimeSeries(i + 1).Item2.Y) / 2
            
            impulseX += avgForceX * dt
            impulseY += avgForceY * dt
        Next
        
        Return New PointF(CSng(impulseX), CSng(impulseY))
    End Function
End Class


Imports System.Windows.Forms

Module Program
    <STAThread>
    Sub Main()
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        Application.Run(New Form1())
    End Sub
End Module
