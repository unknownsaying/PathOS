Imports OpenTK
Imports OpenTK.Graphics
Imports OpenTK.Graphics.OpenGL
Imports System.Drawing

Public Class Form1
    Private WithEvents glControl As GLControl
    Private WithEvents animationTimer As New Timer()
    Private angle As Single = 0.0F
    Private simulationTime As Double = 0.0
    Private planets As List(Of Planet)
    
    Public Sub New()
        InitializeComponent()
        InitializeOpenGL()
        InitializePlanets()
    End Sub
    
    Private Sub InitializeComponent()
        Me.Text = "Kepler's Planetary Orbits with OpenGL"
        Me.WindowState = FormWindowState.Maximized
        Me.DoubleBuffered = False
    End Sub
    
    Private Sub InitializeOpenGL()
        ' Create OpenGL control
        glControl = New GLControl(
            GraphicsMode.Default,
            4, 6, 0,  # OpenGL version 4.6
            GraphicsContextFlags.Default)
        
        glControl.Dock = DockStyle.Fill
        glControl.MakeCurrent()
        glControl.VSync = True
        Me.Controls.Add(glControl)
        
        ' Setup OpenGL
        GL.ClearColor(Color.Black)
        GL.Enable(EnableCap.DepthTest)
        GL.Enable(EnableCap.PointSmooth)
        GL.Hint(HintTarget.PointSmoothHint, HintMode.Nicest)
        GL.Enable(EnableCap.LineSmooth)
        GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest)
        
        ' Setup timer for animation
        animationTimer.Interval = 16  ' ~60 FPS
        animationTimer.Start()
    End Sub
    
    Private Sub InitializePlanets()
        planets = New List(Of Planet) From {
            ' Sun (at center)
            New Planet("Sun", 0.0, 0.0, 0.0, Color.Yellow, 0.4F),
            
            ' Mercury
            New Planet("Mercury", 0.39, 0.2056, 88.0, Color.Gray, 0.1F),
            
            ' Venus
            New Planet("Venus", 0.72, 0.0068, 224.7, Color.Orange, 0.15F),
            
            ' Earth
            New Planet("Earth", 1.0, 0.0167, 365.25, Color.Blue, 0.16F),
            
            ' Mars
            New Planet("Mars", 1.52, 0.0934, 687.0, Color.Red, 0.13F),
            
            ' Jupiter
            New Planet("Jupiter", 5.20, 0.0484, 4331.0, Color.OrangeRed, 0.3F),
            
            ' Saturn
            New Planet("Saturn", 9.58, 0.0542, 10747.0, Color.Goldenrod, 0.28F)
        }
    End Sub
    
    Private Sub GlControl_Paint(sender As Object, e As PaintEventArgs) Handles glControl.Paint
        Render()
    End Sub
    
    Private Sub Render()
        GL.Clear(ClearBufferMask.ColorBufferBit Or ClearBufferMask.DepthBufferBit)
        
        ' Setup projection matrix
        SetupProjection()
        
        ' Setup modelview matrix
        SetupModelView()
        
        ' Draw coordinate system
        DrawCoordinateSystem()
        
        ' Draw orbits and planets
        For Each planet In planets
            DrawOrbit(planet)
            DrawPlanet(planet)
        Next
        
        ' Draw labels
        DrawLabels()
        
        glControl.SwapBuffers()
    End Sub
    
    Private Sub SetupProjection()
        GL.MatrixMode(MatrixMode.Projection)
        GL.LoadIdentity()
        
        Dim aspectRatio As Single = CSng(glControl.Width / glControl.Height)
        GL.Ortho(-15 * aspectRatio, 15 * aspectRatio, -15, 15, -100, 100)
    End Sub
    
    Private Sub SetupModelView()
        GL.MatrixMode(MatrixMode.Modelview)
        GL.LoadIdentity()
        
        ' Add some rotation for better 3D view
        GL.Rotate(30.0F, 1.0F, 0.0F, 0.0F)
        GL.Rotate(angle, 0.0F, 1.0F, 0.0F)
    End Sub
    
    Private Sub DrawCoordinateSystem()
        GL.Begin(PrimitiveType.Lines)
        
        ' X-axis (Red)
        GL.Color3(Color.Red)
        GL.Vertex3(-10, 0, 0)
        GL.Vertex3(10, 0, 0)
        
        ' Y-axis (Green)
        GL.Color3(Color.Green)
        GL.Vertex3(0, -10, 0)
        GL.Vertex3(0, 10, 0)
        
        ' Z-axis (Blue)
        GL.Color3(Color.Blue)
        GL.Vertex3(0, 0, -10)
        GL.Vertex3(0, 0, 10)
        
        GL.End()
    End Sub
    
    Private Sub DrawOrbit(planet As Planet)
        If planet.SemiMajorAxis = 0.0 Then Return ' Skip Sun
        
        ' Calculate orbit parameters based on Kepler's First Law
        Dim a As Double = planet.SemiMajorAxis ' Semi-major axis
        Dim e As Double = planet.Eccentricity  ' Eccentricity
        Dim b As Double = a * Math.Sqrt(1 - e * e) ' Semi-minor axis
        
        GL.Begin(PrimitiveType.LineLoop)
        GL.Color3(planet.Color.R / 255.0, planet.Color.G / 255.0, planet.Color.B / 255.0)
        
        ' Draw ellipse using parametric equation
        For i As Integer = 0 To 360
            Dim theta As Double = i * Math.PI / 180.0
            Dim r As Double = (a * (1 - e * e)) / (1 + e * Math.Cos(theta))
            Dim x As Double = r * Math.Cos(theta)
            Dim y As Double = r * Math.Sin(theta)
            
            ' Apply focus offset (Sun at one focus)
            GL.Vertex3(x - a * e, y, 0)
        Next
        
        GL.End()
    End Sub
    
    Private Sub DrawPlanet(planet As Planet)
        ' Calculate position based on Kepler's Second Law (Equal areas in equal times)
        Dim timeFactor As Double = 0.01
        Dim trueAnomaly As Double = CalculateTrueAnomaly(planet, simulationTime * timeFactor)
        
        Dim r As Double = (planet.SemiMajorAxis * (1 - planet.Eccentricity * planet.Eccentricity)) / 
                          (1 + planet.Eccentricity * Math.Cos(trueAnomaly))
        
        Dim x As Double = r * Math.Cos(trueAnomaly)
        Dim y As Double = r * Math.Sin(trueAnomaly)
        
        ' Apply focus offset
        x -= planet.SemiMajorAxis * planet.Eccentricity
        
        GL.PushMatrix()
        GL.Translate(x, y, 0)
        
        GL.Color3(planet.Color)
        DrawSphere(planet.Size)
        
        GL.PopMatrix()
    End Sub
    
    Private Function CalculateTrueAnomaly(planet As Planet, time As Double) As Double
        ' Kepler's Equation: M = E - e*sin(E)
        ' Where M is mean anomaly, E is eccentric anomaly
        
        Dim period As Double = planet.OrbitalPeriod
        Dim meanAnomaly As Double = (2 * Math.PI * time) / period
        
        ' Solve Kepler's equation using Newton's method
        Dim eccentricAnomaly As Double = meanAnomaly
        Dim tolerance As Double = 0.0001
        
        For i As Integer = 0 To 50
            Dim f As Double = eccentricAnomaly - planet.Eccentricity * Math.Sin(eccentricAnomaly) - meanAnomaly
            Dim fPrime As Double = 1 - planet.Eccentricity * Math.Cos(eccentricAnomaly)
            
            Dim delta As Double = f / fPrime
            eccentricAnomaly -= delta
            
            If Math.Abs(delta) < tolerance Then
                Exit For
            End If
        Next
        
        ' Calculate true anomaly
        Dim trueAnomaly As Double = 2 * Math.Atan2(
            Math.Sqrt(1 + planet.Eccentricity) * Math.Sin(eccentricAnomaly / 2),
            Math.Sqrt(1 - planet.Eccentricity) * Math.Cos(eccentricAnomaly / 2))
        
        Return trueAnomaly
    End Function
    
    Private Sub DrawSphere(radius As Single)
        Dim slices As Integer = 16
        Dim stacks As Integer = 16
        
        For i As Integer = 0 To slices - 1
            GL.Begin(PrimitiveType.QuadStrip)
            
            For j As Integer = 0 To stacks
                Dim theta1 As Double = i * 2 * Math.PI / slices
                Dim theta2 As Double = (i + 1) * 2 * Math.PI / slices
                Dim phi As Double = j * Math.PI / stacks
                
                For k As Integer = 0 To 1
                    Dim theta As Double = If(k = 0, theta1, theta2)
                    
                    Dim x As Double = radius * Math.Sin(phi) * Math.Cos(theta)
                    Dim y As Double = radius * Math.Sin(phi) * Math.Sin(theta)
                    Dim z As Double = radius * Math.Cos(phi)
                    
                    GL.Normal3(x, y, z)
                    GL.Vertex3(x, y, z)
                Next
            Next
            
            GL.End()
        Next
    End Sub
    
    Private Sub DrawLabels()
        GL.MatrixMode(MatrixMode.Projection)
        GL.PushMatrix()
        GL.LoadIdentity()
        GL.Ortho(0, glControl.Width, 0, glControl.Height, -1, 1)
        
        GL.MatrixMode(MatrixMode.Modelview)
        GL.PushMatrix()
        GL.LoadIdentity()
        
        ' Disable depth testing for 2D text
        GL.Disable(EnableCap.DepthTest)
        
        ' Draw Kepler's Laws information
        DrawText("Kepler's Laws of Planetary Motion", 10, glControl.Height - 30, Color.White)
        DrawText("1. Planets move in elliptical orbits with Sun at one focus", 10, glControl.Height - 50, Color.Yellow)
        DrawText("2. Equal areas are swept in equal times (shown by orbital speeds)", 10, glControl.Height - 70, Color.Yellow)
        DrawText("3. T² ∝ a³ (Orbital period squared ∝ semi-major axis cubed)", 10, glControl.Height - 90, Color.Yellow)
        DrawText($"Simulation Time: {simulationTime:F1} Earth Days", 10, glControl.Height - 110, Color.Cyan)
        
        ' Re-enable depth testing
        GL.Enable(EnableCap.DepthTest)
        
        GL.MatrixMode(MatrixMode.Projection)
        GL.PopMatrix()
        GL.MatrixMode(MatrixMode.Modelview)
        GL.PopMatrix()
    End Sub
    
    Private Sub DrawText(text As String, x As Integer, y As Integer, color As Color)
        ' Simple text rendering using GDI (for simplicity)
        Using g As Graphics = glControl.CreateGraphics()
            Using font As New Font("Arial", 12)
                Using brush As New SolidBrush(color)
                    g.DrawString(text, font, brush, x, y)
                End Using
            End Using
        End Using
    End Sub
    
    Private Sub AnimationTimer_Tick(sender As Object, e As EventArgs) Handles animationTimer.Tick
        angle += 0.5F
        If angle > 360 Then angle -= 360
        
        simulationTime += 1.0
        
        glControl.Invalidate()
    End Sub
    
    Private Sub Form1_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        If glControl IsNot Nothing Then
            glControl.Invalidate()
        End If
    End Sub
End Class

Public Class Planet
    Public Property Name As String
    Public Property SemiMajorAxis As Double ' In AU (Astronomical Units)
    Public Property Eccentricity As Double
    Public Property OrbitalPeriod As Double ' In Earth days
    Public Property Color As Color
    Public Property Size As Single
    
    Public Sub New(name As String, semiMajorAxis As Double, eccentricity As Double, 
                   orbitalPeriod As Double, color As Color, size As Single)
        Me.Name = name
        Me.SemiMajorAxis = semiMajorAxis
        Me.Eccentricity = eccentricity
        Me.OrbitalPeriod = orbitalPeriod
        Me.Color = color
        Me.Size = size
    End Sub
End Class

Public Class KeplerMath
    ' Calculate orbital position using Kepler's equations
    
    Public Shared Function CalculateOrbitPosition(a As Double, e As Double, 
                                                 meanAnomaly As Double) As PointD
        ' Solve Kepler's equation: M = E - e*sin(E)
        Dim eccentricAnomaly As Double = SolveKeplerEquation(meanAnomaly, e)
        
        ' Calculate true anomaly
        Dim trueAnomaly As Double = CalculateTrueAnomaly(eccentricAnomaly, e)
        
        ' Calculate radius
        Dim r As Double = a * (1 - e * e) / (1 + e * Math.Cos(trueAnomaly))
        
        ' Calculate Cartesian coordinates
        Dim x As Double = r * Math.Cos(trueAnomaly)
        Dim y As Double = r * Math.Sin(trueAnomaly)
        
        ' Adjust for focus (Sun at one focus)
        x -= a * e
        
        Return New PointD(x, y)
    End Function
    
    Private Shared Function SolveKeplerEquation(M As Double, e As Double) As Double
        ' Newton's method to solve Kepler's equation
        Dim E As Double = M
        Dim tolerance As Double = 0.0000001
        
        For i As Integer = 0 To 50
            Dim f As Double = E - e * Math.Sin(E) - M
            Dim fPrime As Double = 1 - e * Math.Cos(E)
            
            Dim delta As Double = f / fPrime
            E -= delta
            
            If Math.Abs(delta) < tolerance Then
                Exit For
            End If
        Next
        
        Return E
    End Function
    
    Private Shared Function CalculateTrueAnomaly(E As Double, e As Double) As Double
        Return 2 * Math.Atan2(
            Math.Sqrt(1 + e) * Math.Sin(E / 2),
            Math.Sqrt(1 - e) * Math.Cos(E / 2))
    End Function
    
    ' Calculate orbital velocity based on Kepler's Second Law
    Public Shared Function CalculateVelocity(a As Double, e As Double, 
                                           trueAnomaly As Double, period As Double) As Double
        Dim r As Double = a * (1 - e * e) / (1 + e * Math.Cos(trueAnomaly))
        Dim h As Double = 2 * Math.PI * a * Math.Sqrt(1 - e * e) / period
        Return h / r
    End Function
End Class

Public Structure PointD
    Public X As Double
    Public Y As Double
    Public Sub New(x As Double, y As Double)
        Me.X = x
        Me.Y = y
    End Sub
End Structure

Imports System.Windows.Forms

Module Program
    <STAThread>
    Sub Main()
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        Application.Run(New Form1())
    End Sub
End Module
