Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Numerics
Imports System.Math

Public Class QuantumVisualizer
    ' Quantum system properties
    Private quantumSystem As QuantumSystem
    Private waveFunction As WaveFunction
    Private potentialType As PotentialType = PotentialType.InfiniteSquareWell
    Private currentState As Integer = 1
    Private time As Double = 0
    Private WithEvents animationTimer As New Timer()
    Private isAnimating As Boolean = False
    
    ' Visualization settings
    Private scale As Double = 50
    Private showProbability As Boolean = True
    Private showPhase As Boolean = True
    Private showPotential As Boolean = True
    Private showEnergyLevels As Boolean = True
    
    ' UI Controls
    Private WithEvents picVisualization As New PictureBox
    Private WithEvents cmbPotential As New ComboBox
    Private WithEvents trkState As New TrackBar
    Private WithEvents lblState As New Label
    Private WithEvents btnAnimate As New Button
    Private WithEvents btnMeasure As New Button
    Private WithEvents chkProbability As New CheckBox
    Private WithEvents chkPhase As New CheckBox
    Private WithEvents chkPotential As New CheckBox
    Private WithEvents chkEnergyLevels As New CheckBox
    Private WithEvents trkTimeScale As New TrackBar
    Private WithEvents lblTimeScale As New Label
    Private WithEvents lblEquation As New Label
    Private WithEvents lblQuantumInfo As New Label
    
    Public Sub New()
        InitializeComponent()
        SetupQuantumSystem()
        SetupUI()
        UpdateVisualization()
    End Sub
    
    Private Sub InitializeComponent()
        Me.Text = "Quantum Mechanics Visualizer - Physical Equations"
        Me.Size = New Size(1200, 800)
        Me.DoubleBuffered = True
        
        ' Timer setup
        animationTimer.Interval = 50 ' 20 FPS
    End Sub
    
    Private Sub SetupUI()
        ' Main visualization area
        picVisualization.Dock = DockStyle.Fill
        picVisualization.BackColor = Color.Black
        
        ' Control panel
        Dim controlPanel As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 200,
            .BackColor = Color.FromArgb(40, 40, 60)
        }
        
        ' Potential type selector
        cmbPotential.Items.AddRange({"Infinite Square Well", "Harmonic Oscillator", 
                                    "Finite Square Well", "Coulomb Potential", 
                                    "Double Well", "Step Potential"})
        cmbPotential.SelectedIndex = 0
        cmbPotential.Dock = DockStyle.Top
        cmbPotential.Height = 30
        
        ' State selector
        trkState.Minimum = 1
        trkState.Maximum = 10
        trkState.Value = 1
        trkState.Dock = DockStyle.Top
        trkState.Height = 30
        
        lblState.Text = "Quantum State (n): 1"
        lblState.Dock = DockStyle.Top
        lblState.ForeColor = Color.White
        lblState.Height = 30
        
        ' Checkboxes
        chkProbability.Text = "Show Probability Density"
        chkProbability.Checked = True
        chkProbability.ForeColor = Color.White
        
        chkPhase.Text = "Show Phase (Complex)"
        chkPhase.Checked = True
        chkPhase.ForeColor = Color.White
        
        chkPotential.Text = "Show Potential"
        chkPotential.Checked = True
        chkPotential.ForeColor = Color.White
        
        chkEnergyLevels.Text = "Show Energy Levels"
        chkEnergyLevels.Checked = True
        chkEnergyLevels.ForeColor = Color.White
        
        ' Time scale
        trkTimeScale.Minimum = 1
        trkTimeScale.Maximum = 100
        trkTimeScale.Value = 50
        trkTimeScale.Dock = DockStyle.Top
        trkTimeScale.Height = 30
        
        lblTimeScale.Text = "Time Scale: 50"
        lblTimeScale.Dock = DockStyle.Top
        lblTimeScale.ForeColor = Color.White
        lblTimeScale.Height = 30
        
        ' Buttons
        btnAnimate.Text = "Start Animation"
        btnAnimate.Dock = DockStyle.Left
        btnAnimate.Width = 150
        
        btnMeasure.Text = "Quantum Measurement"
        btnMeasure.Dock = DockStyle.Left
        btnMeasure.Width = 150
        
        ' Equation display
        lblEquation.Dock = DockStyle.Bottom
        lblEquation.Height = 60
        lblEquation.BackColor = Color.FromArgb(20, 20, 40)
        lblEquation.ForeColor = Color.White
        lblEquation.Font = New Font("Cambria Math", 10)
        
        ' Quantum info display
        lblQuantumInfo.Dock = DockStyle.Bottom
        lblQuantumInfo.Height = 40
        lblQuantumInfo.BackColor = Color.FromArgb(20, 20, 40)
        lblQuantumInfo.ForeColor = Color.LightCyan
        lblQuantumInfo.Font = New Font("Consolas", 9)
        
        ' Layout control panel
        Dim topFlow As New FlowLayoutPanel With {
            .Dock = DockStyle.Top,
            .FlowDirection = FlowDirection.LeftToRight,
            .WrapContents = True,
            .Height = 60
        }
        
        topFlow.Controls.AddRange({cmbPotential, lblState, trkState})
        
        Dim checkFlow As New FlowLayoutPanel With {
            .Dock = DockStyle.Top,
            .FlowDirection = FlowDirection.LeftToRight,
            .WrapContents = True,
            .Height = 40
        }
        
        checkFlow.Controls.AddRange({chkProbability, chkPhase, chkPotential, chkEnergyLevels})
        
        Dim timeFlow As New FlowLayoutPanel With {
            .Dock = DockStyle.Top,
            .FlowDirection = FlowDirection.LeftToRight,
            .WrapContents = False,
            .Height = 60
        }
        
        timeFlow.Controls.AddRange({lblTimeScale, trkTimeScale})
        
        Dim buttonFlow As New FlowLayoutPanel With {
            .Dock = DockStyle.Top,
            .FlowDirection = FlowDirection.LeftToRight,
            .WrapContents = False,
            .Height = 40
        }
        
        buttonFlow.Controls.AddRange({btnAnimate, btnMeasure})
        
        controlPanel.Controls.AddRange({topFlow, checkFlow, timeFlow, buttonFlow})
        
        ' Main layout
        Me.Controls.AddRange({picVisualization, lblEquation, lblQuantumInfo, controlPanel})
        
        ' Event handlers
        AddHandler picVisualization.Paint, AddressOf PicVisualization_Paint
        AddHandler picVisualization.Resize, AddressOf PicVisualization_Resize
    End Sub
    
    Private Sub SetupQuantumSystem()
        quantumSystem = New QuantumSystem()
        waveFunction = New WaveFunction()
        UpdateQuantumSystem()
    End Sub
    
    Private Sub UpdateQuantumSystem()
        Select Case potentialType
            Case PotentialType.InfiniteSquareWell
                quantumSystem.PotentialFunction = AddressOf InfiniteSquareWellPotential
                lblEquation.Text = "Schrödinger Equation: (-ħ²/2m)∇²ψ + V(x)ψ = iħ∂ψ/∂t" & vbCrLf &
                                 "Infinite Well: V(x) = {0 if 0<x<L, ∞ otherwise}"
            Case PotentialType.HarmonicOscillator
                quantumSystem.PotentialFunction = AddressOf HarmonicOscillatorPotential
                lblEquation.Text = "Harmonic Oscillator: V(x) = (1/2)mω²x²" & vbCrLf &
                                 "Energy: Eₙ = ħω(n + 1/2)"
            Case PotentialType.FiniteSquareWell
                quantumSystem.PotentialFunction = AddressOf FiniteSquareWellPotential
                lblEquation.Text = "Finite Square Well: V(x) = {V₀ if |x|>a, 0 otherwise}" & vbCrLf &
                                 "Bound States: E < V₀"
            Case PotentialType.CoulombPotential
                quantumSystem.PotentialFunction = AddressOf CoulombPotential
                lblEquation.Text = "Coulomb Potential: V(r) = -k/r" & vbCrLf &
                                 "Hydrogen Atom: Eₙ = -13.6eV/n²"
            Case PotentialType.DoubleWell
                quantumSystem.PotentialFunction = AddressOf DoubleWellPotential
                lblEquation.Text = "Double Well: V(x) = a(x² - b²)²" & vbCrLf &
                                 "Quantum Tunneling between wells"
            Case PotentialType.StepPotential
                quantumSystem.PotentialFunction = AddressOf StepPotential
                lblEquation.Text = "Step Potential: V(x) = {0 if x<0, V₀ if x>0}" & vbCrLf &
                                 "Reflection & Transmission coefficients"
        End Select
        
        waveFunction.Initialize(currentState, potentialType)
        UpdateVisualization()
    End Sub
    
    ' Potential Functions
    Private Function InfiniteSquareWellPotential(x As Double) As Double
        Dim L As Double = 10 ' Well width
        If x >= -L/2 And x <= L/2 Then
            Return 0
        Else
            Return Double.MaxValue
        End If
    End Function
    
    Private Function HarmonicOscillatorPotential(x As Double) As Double
        Dim omega As Double = 1.0
        Return 0.5 * omega * omega * x * x
    End Function
    
    Private Function FiniteSquareWellPotential(x As Double) As Double
        Dim wellWidth As Double = 6
        Dim wellDepth As Double = 10
        If Math.Abs(x) <= wellWidth/2 Then
            Return 0
        Else
            Return wellDepth
        End If
    End Function
    
    Private Function CoulombPotential(x As Double) As Double
        ' Simplified 1D Coulomb potential
        Dim r As Double = Math.Abs(x) + 0.1 ' Avoid singularity
        Return -1.0 / r
    End Function
    
    Private Function DoubleWellPotential(x As Double) As Double
        Dim a As Double = 0.5
        Dim b As Double = 3.0
        Return a * Math.Pow(x * x - b * b, 2)
    End Function
    
    Private Function StepPotential(x As Double) As Double
        Dim stepHeight As Double = 5
        If x < 0 Then
            Return 0
        Else
            Return stepHeight
        End If
    End Function
    
    Private Sub PicVisualization_Paint(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAlias
        
        ' Clear background
        g.Clear(Color.Black)
        
        ' Draw coordinate system
        DrawCoordinateSystem(g)
        
        ' Draw potential
        If showPotential Then
            DrawPotential(g)
        End If
        
        ' Draw energy levels
        If showEnergyLevels Then
            DrawEnergyLevels(g)
        End If
        
        ' Draw wave function
        DrawWaveFunction(g)
        
        ' Draw probability density
        If showProbability Then
            DrawProbabilityDensity(g)
        End If
        
        ' Draw phase if complex
        If showPhase And waveFunction.IsComplex Then
            DrawPhase(g)
        End If
        
        ' Draw quantum information
        DrawQuantumInfo(g)
    End Sub
    
    Private Sub DrawCoordinateSystem(g As Graphics)
        Dim centerX As Integer = picVisualization.Width \ 2
        Dim centerY As Integer = picVisualization.Height \ 2
        
        ' Draw axes
        Using axisPen As New Pen(Color.FromArgb(80, 100, 100), 1)
            ' X-axis
            g.DrawLine(axisPen, 0, centerY, picVisualization.Width, centerY)
            ' Y-axis
            g.DrawLine(axisPen, centerX, 0, centerX, picVisualization.Height)
        End Using
        
        ' Draw grid
        Using gridPen As New Pen(Color.FromArgb(40, 40, 40), 1)
            Dim gridSpacing As Integer = CInt(scale)
            For x As Integer = centerX Mod gridSpacing To picVisualization.Width Step gridSpacing
                g.DrawLine(gridPen, x, 0, x, picVisualization.Height)
            Next
            
            For y As Integer = centerY Mod gridSpacing To picVisualization.Height Step gridSpacing
                g.DrawLine(gridPen, 0, y, picVisualization.Width, y)
            Next
        End Using
        
        ' Draw axis labels
        Using labelFont As New Font("Arial", 10)
            Using labelBrush As New SolidBrush(Color.Gray)
                g.DrawString("Position (x)", labelFont, labelBrush, picVisualization.Width - 100, centerY + 10)
                g.DrawString("V(x), ψ(x)", labelFont, labelBrush, centerX + 10, 10)
            End Using
        End Using
    End Sub
    
    Private Sub DrawPotential(g As Graphics)
        Dim centerX As Integer = picVisualization.Width \ 2
        Dim centerY As Integer = picVisualization.Height \ 2
        
        Using potentialPen As New Pen(Color.FromArgb(200, 100, 100), 2)
            Dim points As New List(Of PointF)
            
            For i As Integer = 0 To picVisualization.Width
                Dim x As Double = (i - centerX) / scale
                Dim potential As Double = quantumSystem.GetPotential(x)
                
                ' Scale potential for display
                Dim displayY As Double = centerY - potential * scale * 0.5
                
                If displayY > 0 And displayY < picVisualization.Height Then
                    points.Add(New PointF(i, CSng(displayY)))
                End If
            Next
            
            If points.Count > 1 Then
                g.DrawLines(potentialPen, points.ToArray())
            End If
            
            ' Label
            Using labelFont As New Font("Arial", 12, FontStyle.Bold)
                g.DrawString("Potential V(x)", labelFont, Brushes.LightCoral, 10, 30)
            End Using
        End Using
    End Sub
    
    Private Sub DrawEnergyLevels(g As Graphics)
        Dim centerY As Integer = picVisualization.Height \ 2
        Dim energyLevels As Double() = quantumSystem.GetEnergyLevels(currentState)
        
        Using energyPen As New Pen(Color.FromArgb(100, 200, 100), 1)
            For Each energy In energyLevels
                Dim y As Single = CSng(centerY - energy * scale * 0.5)
                g.DrawLine(energyPen, 0, y, picVisualization.Width, y)
                
                ' Label energy value
                Using labelFont As New Font("Arial", 9)
                    g.DrawString($"E = {energy:F2}", labelFont, Brushes.LightGreen, 
                                picVisualization.Width - 70, y - 15)
                End Using
            Next
        End Using
    End Sub
    
    Private Sub DrawWaveFunction(g As Graphics)
        Dim centerX As Integer = picVisualization.Width \ 2
        Dim centerY As Integer = picVisualization.Height \ 2
        
        ' Real part
        Using realPen As New Pen(Color.Cyan, 2)
            DrawWaveComponent(g, realPen, Function(x) waveFunction.GetValue(x, time).Real, centerX, centerY)
        End Using
        
        ' Imaginary part (if complex)
        If waveFunction.IsComplex Then
            Using imagPen As New Pen(Color.Magenta, 2)
                DrawWaveComponent(g, imagPen, Function(x) waveFunction.GetValue(x, time).Imaginary, centerX, centerY)
            End Using
        End If
        
        ' Labels
        Using labelFont As New Font("Arial", 12, FontStyle.Bold)
            g.DrawString("Wave Function ψ(x)", labelFont, Brushes.Cyan, 10, 50)
            If waveFunction.IsComplex Then
                g.DrawString("Real: Cyan, Imag: Magenta", labelFont, Brushes.Magenta, 10, 70)
            End If
        End Using
    End Sub
    
    Private Sub DrawWaveComponent(g As Graphics, pen As Pen, waveFunc As Func(Of Double, Double), 
                                 centerX As Integer, centerY As Integer)
        Dim points As New List(Of PointF)
        
        For i As Integer = 0 To picVisualization.Width
            Dim x As Double = (i - centerX) / scale
            Dim psi As Double = waveFunc(x)
            
            ' Scale wave function for display
            Dim displayY As Double = centerY - psi * scale * 10
            
            If displayY > 0 And displayY < picVisualization.Height Then
                points.Add(New PointF(i, CSng(displayY)))
            ElseIf points.Count > 0 Then
                ' Draw segment and start new one
                g.DrawLines(pen, points.ToArray())
                points.Clear()
            End If
        Next
        
        If points.Count > 1 Then
            g.DrawLines(pen, points.ToArray())
        End If
    End Sub
    
    Private Sub DrawProbabilityDensity(g As Graphics)
        Dim centerX As Integer = picVisualization.Width \ 2
        Dim centerY As Integer = picVisualization.Height \ 2
        
        Using probPen As New Pen(Color.Yellow, 2)
            Using probBrush As New SolidBrush(Color.FromArgb(50, 255, 255, 0))
                Dim points As New List(Of PointF)
                
                For i As Integer = 0 To picVisualization.Width
                    Dim x As Double = (i - centerX) / scale
                    Dim psi As Complex = waveFunction.GetValue(x, time)
                    Dim probability As Double = psi.Real * psi.Real + psi.Imaginary * psi.Imaginary
                    
                    ' Scale probability for display
                    Dim displayY As Double = centerY - probability * scale * 20
                    
                    points.Add(New PointF(i, CSng(centerY))) ' Bottom point
                    points.Add(New PointF(i, CSng(displayY))) ' Top point
                    
                    ' Fill probability area
                    If i > 0 Then
                        Dim fillPoints() As PointF = {
                            New PointF(i-1, CSng(centerY)),
                            New PointF(i-1, points(points.Count-3).Y),
                            New PointF(i, CSng(displayY)),
                            New PointF(i, CSng(centerY))
                        }
                        g.FillPolygon(probBrush, fillPoints)
                    End If
                Next
                
                ' Draw probability density line
                Dim densityPoints As New List(Of PointF)
                For i As Integer = 0 To picVisualization.Width Step 2
                    Dim x As Double = (i - centerX) / scale
                    Dim psi As Complex = waveFunction.GetValue(x, time)
                    Dim probability As Double = psi.Real * psi.Real + psi.Imaginary * psi.Imaginary
                    Dim displayY As Double = centerY - probability * scale * 20
                    densityPoints.Add(New PointF(i, CSng(displayY)))
                Next
                
                If densityPoints.Count > 1 Then
                    g.DrawLines(probPen, densityPoints.ToArray())
                End If
            End Using
        End Using
        
        ' Label
        Using labelFont As New Font("Arial", 12, FontStyle.Bold)
            g.DrawString("Probability Density |ψ|²", labelFont, Brushes.Yellow, 10, 90)
        End Using
    End Sub
    
    Private Sub DrawPhase(g As Graphics)
        Dim centerX As Integer = picVisualization.Width \ 2
        Dim centerY As Integer = picVisualization.Height \ 2
        
        Using phasePen As New Pen(Color.FromArgb(150, 255, 150, 255), 1)
            Dim spacing As Integer = 20
            
            For xPixel As Integer = spacing To picVisualization.Width Step spacing
                Dim x As Double = (xPixel - centerX) / scale
                Dim psi As Complex = waveFunction.GetValue(x, time)
                
                ' Calculate phase angle
                Dim phase As Double = Math.Atan2(psi.Imaginary, psi.Real)
                Dim magnitude As Double = Math.Sqrt(psi.Real * psi.Real + psi.Imaginary * psi.Imaginary)
                
                ' Draw phase arrow
                Dim arrowLength As Integer = CInt(magnitude * scale * 5)
                Dim endX As Single = xPixel + CSng(Math.Cos(phase) * arrowLength)
                Dim endY As Single = centerY - CSng(Math.Sin(phase) * arrowLength)
                
                ' Draw arrow
                g.DrawLine(phasePen, xPixel, centerY, endX, endY)
                
                ' Draw arrowhead
                Dim arrowSize As Integer = 5
                Dim arrowPoints() As PointF = {
                    New PointF(endX, endY),
                    New PointF(endX - arrowSize * CSng(Math.Cos(phase - Math.PI/6)), 
                              endY + arrowSize * CSng(Math.Sin(phase - Math.PI/6))),
                    New PointF(endX - arrowSize * CSng(Math.Cos(phase + Math.PI/6)), 
                              endY + arrowSize * CSng(Math.Sin(phase + Math.PI/6)))
                }
                g.FillPolygon(Brushes.Magenta, arrowPoints)
            Next
        End Using
        
        ' Label
        Using labelFont As New Font("Arial", 12, FontStyle.Bold)
            g.DrawString("Phase Angle (Complex Plane)", labelFont, Brushes.Magenta, 10, 110)
        End Using
    End Sub
    
    Private Sub DrawQuantumInfo(g As Graphics)
        Dim info As String = waveFunction.GetQuantumInfo(currentState, time)
        Using infoFont As New Font("Consolas", 10)
            Using infoBrush As New SolidBrush(Color.LightCyan)
                g.DrawString(info, infoFont, infoBrush, 10, picVisualization.Height - 100)
            End Using
        End Using
    End Sub
    
    Private Sub UpdateVisualization()
        picVisualization.Invalidate()
        
        ' Update quantum info label
        Dim energy As Double = quantumSystem.CalculateEnergy(currentState)
        Dim wavelength As Double = quantumSystem.CalculateWavelength(energy)
        
        lblQuantumInfo.Text = $"State: n={currentState} | Energy: {energy:F4} | " &
                            $"Wavelength: {wavelength:F4} | " &
                            $"Probability Norm: {waveFunction.CalculateNorm():F4}"
    End Sub
    
    ' Event Handlers
    Private Sub CmbPotential_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbPotential.SelectedIndexChanged
        potentialType = CType(cmbPotential.SelectedIndex, PotentialType)
        UpdateQuantumSystem()
    End Sub
    
    Private Sub TrkState_Scroll(sender As Object, e As EventArgs) Handles trkState.Scroll
        currentState = trkState.Value
        lblState.Text = $"Quantum State (n): {currentState}"
        waveFunction.SetState(currentState)
        UpdateVisualization()
    End Sub
    
    Private Sub ChkProbability_CheckedChanged(sender As Object, e As EventArgs) Handles chkProbability.CheckedChanged
        showProbability = chkProbability.Checked
        UpdateVisualization()
    End Sub
    
    Private Sub ChkPhase_CheckedChanged(sender As Object, e As EventArgs) Handles chkPhase.CheckedChanged
        showPhase = chkPhase.Checked
        UpdateVisualization()
    End Sub
    
    Private Sub ChkPotential_CheckedChanged(sender As Object, e As EventArgs) Handles chkPotential.CheckedChanged
        showPotential = chkPotential.Checked
        UpdateVisualization()
    End Sub
    
    Private Sub ChkEnergyLevels_CheckedChanged(sender As Object, e As EventArgs) Handles chkEnergyLevels.CheckedChanged
        showEnergyLevels = chkEnergyLevels.Checked
        UpdateVisualization()
    End Sub
    
    Private Sub TrkTimeScale_Scroll(sender As Object, e As EventArgs) Handles trkTimeScale.Scroll
        lblTimeScale.Text = $"Time Scale: {trkTimeScale.Value}"
    End Sub
    
    Private Sub BtnAnimate_Click(sender As Object, e As EventArgs) Handles btnAnimate.Click
        isAnimating = Not isAnimating
        If isAnimating Then
            btnAnimate.Text = "Stop Animation"
            animationTimer.Start()
        Else
            btnAnimate.Text = "Start Animation"
            animationTimer.Stop()
        End If
    End Sub
    
    Private Sub BtnMeasure_Click(sender As Object, e As EventArgs) Handles btnMeasure.Click
        PerformQuantumMeasurement()
    End Sub
    
    Private Sub AnimationTimer_Tick(sender As Object, e As EventArgs) Handles animationTimer.Tick
        time += 0.05 * (trkTimeScale.Value / 50.0)
        If time > 2 * Math.PI Then time = 0
        UpdateVisualization()
    End Sub
    
    Private Sub PicVisualization_Resize(sender As Object, e As EventArgs)
        UpdateVisualization()
    End Sub
    
    Private Sub PerformQuantumMeasurement()
        ' Simulate quantum measurement
        Dim measurementResult As Double = waveFunction.SimulateMeasurement()
        
        MessageBox.Show($"Quantum Measurement Result:{vbCrLf}" &
                       $"Measured Position: {measurementResult:F4}{vbCrLf}" &
                       $"Wave Function Collapsed!{vbCrLf}" &
                       $"Uncertainty Principle: ΔxΔp ≥ ħ/2",
                       "Quantum Measurement", MessageBoxButtons.OK, MessageBoxIcon.Information)
        
        ' Visualize measurement
        waveFunction.Collapse(measurementResult)
        UpdateVisualization()
    End Sub
End Class

Public Enum PotentialType
    InfiniteSquareWell
    HarmonicOscillator
    FiniteSquareWell
    CoulombPotential
    DoubleWell
    StepPotential
End Enum


Imports System.Numerics
Imports System.Math

Public Class QuantumSystem
    ' Physical constants (atomic units)
    Public Shared ReadOnly ħ As Double = 1.0 ' Reduced Planck constant
    Public Shared ReadOnly m As Double = 1.0 ' Particle mass
    Public Shared ReadOnly e As Double = 1.0 ' Elementary charge
    
    Public Property PotentialFunction As Func(Of Double, Double)
    
    Public Sub New()
        ' Default potential: infinite square well
        PotentialFunction = AddressOf DefaultPotential
    End Sub
    
    Private Function DefaultPotential(x As Double) As Double
        Dim L As Double = 10
        If Math.Abs(x) <= L/2 Then Return 0 Else Return Double.MaxValue
    End Function
    
    Public Function GetPotential(x As Double) As Double
        Return PotentialFunction.Invoke(x)
    End Function
    
    Public Function CalculateEnergy(n As Integer) As Double
        ' Different energy formulas for different potentials
        Select Case quantumVisualizer?.PotentialType ' Access from main form
            Case PotentialType.InfiniteSquareWell
                Dim L As Double = 10
                Return (n * n * Math.PI * Math.PI * ħ * ħ) / (2 * m * L * L)
                
            Case PotentialType.HarmonicOscillator
                Dim omega As Double = 1.0
                Return ħ * omega * (n + 0.5)
                
            Case PotentialType.CoulombPotential
                ' Hydrogen-like atom energies
                Return -13.6 / (n * n) ' eV
                
            Case Else
                Return n * n * 0.5 ' Arbitrary scaling
        End Select
    End Function
    
    Public Function CalculateWavelength(energy As Double) As Double
        If energy <= 0 Then Return Double.MaxValue
        Dim p As Double = Math.Sqrt(2 * m * energy) ' Momentum
        Return 2 * Math.PI * ħ / p ' De Broglie wavelength
    End Function
    
    Public Function GetEnergyLevels(maxN As Integer) As Double()
        Dim levels(maxN - 1) As Double
        For i As Integer = 1 To maxN
            levels(i - 1) = CalculateEnergy(i)
        Next
        Return levels
    End Function
    
    ' Solve time-independent Schrödinger equation numerically
    Public Function SolveSchrodinger(n As Integer, Optional steps As Integer = 1000) As Double()
        Dim psi(steps) As Double
        Dim dx As Double = 0.01
        Dim x As Double = -5
        
        ' Initial conditions for even/odd states
        If n Mod 2 = 0 Then
            psi(0) = 0
            psi(1) = 0.001
        Else
            psi(0) = 0.001
            psi(1) = psi(0) * (1 - dx * dx * (GetPotential(x) - CalculateEnergy(n)) / (ħ * ħ / (2 * m)))
        End If
        
        ' Numerov method for 1D Schrödinger equation
        For i As Integer = 2 To steps
            x += dx
            Dim V As Double = GetPotential(x)
            Dim k2 As Double = 2 * m * (CalculateEnergy(n) - V) / (ħ * ħ)
            
            ' Numerov propagation
            psi(i) = (2 * (1 - 5 * dx * dx * k2 / 12) * psi(i - 1) -
                     (1 + dx * dx * k2 / 12) * psi(i - 2)) / (1 + dx * dx * k2 / 12)
        Next
        
        ' Normalize
        Dim norm As Double = Math.Sqrt(Integrate(Function(xi) psi(CInt((xi + 5) / dx)) * psi(CInt((xi + 5) / dx)), -5, 5))
        For i As Integer = 0 To steps
            psi(i) /= norm
        Next
        
        Return psi
    End Function
    
    Private Function Integrate(func As Func(Of Double, Double), a As Double, b As Double) As Double
        Dim steps As Integer = 1000
        Dim dx As Double = (b - a) / steps
        Dim sum As Double = 0
        
        For i As Integer = 0 To steps - 1
            Dim x As Double = a + i * dx
            sum += func(x) * dx
        Next
        
        Return sum
    End Function
    
    ' Calculate expectation values
    Public Function ExpectationValue(operatorType As OperatorType, psi As Double()) As Double
        Select Case operatorType
            Case OperatorType.Position
                Return IntegrateArray(Function(i, xi) xi * psi(i) * psi(i))
                
            Case OperatorType.Momentum
                ' Derivative using finite differences
                Dim sum As Double = 0
                Dim dx As Double = 0.01
                For i As Integer = 1 To psi.Length - 2
                    Dim derivative As Double = (psi(i + 1) - psi(i - 1)) / (2 * dx)
                    sum += psi(i) * (-Complex.ImaginaryOne * ħ * derivative) * psi(i) * dx
                Next
                Return sum
                
            Case OperatorType.Energy
                Return CalculateEnergy(1) ' Simplified
                
            Case Else
                Return 0
        End Select
    End Function
    
    Private Function IntegrateArray(func As Func(Of Integer, Double, Double)) As Double
        Dim steps As Integer = 1000
        Dim dx As Double = 0.01
        Dim sum As Double = 0
        
        For i As Integer = 0 To steps
            Dim x As Double = -5 + i * dx
            sum += func(i, x) * dx
        Next
        
        Return sum
    End Function
End Class

Public Enum OperatorType
    Position
    Momentum
    Energy
    AngularMomentum
End Enum


Imports System.Numerics
Imports System.Math

Public Class WaveFunction
    Private Property N As Integer = 1
    Private Property CurrentPotential As PotentialType
    Private Property IsStationary As Boolean = True
    Private Property LastMeasurement As Double = 0
    
    Public ReadOnly Property IsComplex As Boolean
        Get
            Return Not IsStationary
        End Get
    End Property
    
    Public Sub Initialize(n As Integer, potentialType As PotentialType)
        Me.N = n
        Me.CurrentPotential = potentialType
    End Sub
    
    Public Sub SetState(n As Integer)
        Me.N = n
    End Sub
    
    Public Function GetValue(x As Double, t As Double) As Complex
        Select Case CurrentPotential
            Case PotentialType.InfiniteSquareWell
                Return InfiniteSquareWellPsi(x, t)
                
            Case PotentialType.HarmonicOscillator
                Return HarmonicOscillatorPsi(x, t)
                
            Case PotentialType.CoulombPotential
                Return HydrogenPsi(x, t)
                
            Case Else
                Return SimpleWavePacket(x, t)
        End Select
    End Function
    
    Private Function InfiniteSquareWellPsi(x As Double, t As Double) As Complex
        Dim L As Double = 10 ' Well width
        Dim k As Double = N * PI / L
        Dim omega As Double = (ħ * k * k) / (2 * m)
        
        If Math.Abs(x) > L/2 Then
            Return Complex.Zero
        Else
            Dim amplitude As Double = Math.Sqrt(2 / L)
            Dim spatialPart As Double = amplitude * Math.Sin(k * (x + L/2))
            Dim timePart As Complex = Complex.Exp(-Complex.ImaginaryOne * omega * t)
            Return spatialPart * timePart
        End If
    End Function
    
    Private Function HarmonicOscillatorPsi(x As Double, t As Double) As Complex
        ' Hermite polynomial approximation
        Dim omega As Double = 1.0
        Dim alpha As Double = Math.Sqrt(m * omega / ħ)
        Dim xi As Double = alpha * x
        
        ' Ground state for simplicity
        Dim psi0 As Double = Math.Pow(alpha / Math.Sqrt(PI), 0.5) * Math.Exp(-xi * xi / 2)
        
        ' Excited states using Hermite polynomials
        Dim Hn As Double = HermitePolynomial(N, xi)
        Dim spatialPart As Double = psi0 * Hn / Math.Sqrt(Math.Pow(2, N) * Factorial(N))
        Dim energy As Double = ħ * omega * (N + 0.5)
        Dim timePart As Complex = Complex.Exp(-Complex.ImaginaryOne * energy * t / ħ)
        
        Return spatialPart * timePart
    End Function
    
    Private Function HydrogenPsi(x As Double, t As Double) As Complex
        ' Simplified 1D hydrogen-like wavefunction
        Dim a0 As Double = 1.0 ' Bohr radius
        Dim r As Double = Math.Abs(x) + 0.001
        
        Select Case N
            Case 1 ' 1s orbital
                Dim psi As Double = 2 * Math.Exp(-r / a0) / Math.Sqrt(4 * PI)
                Return psi * Complex.Exp(-Complex.ImaginaryOne * 13.6 * t / ħ)
                
            Case 2 ' 2s orbital
                Dim psi As Double = (1 / Math.Sqrt(32 * PI)) * (2 - r / a0) * Math.Exp(-r / (2 * a0))
                Return psi * Complex.Exp(-Complex.ImaginaryOne * 3.4 * t / ħ)
                
            Case Else
                Return Complex.Zero
        End Select
    End Function
    
    Private Function SimpleWavePacket(x As Double, t As Double) As Complex
        ' Gaussian wave packet
        Dim sigma As Double = 1.0 ' Width
        Dim k0 As Double = N ' Central wave number
        Dim x0 As Double = 0 ' Center position
        
        Dim envelope As Double = Math.Exp(-(x - x0) * (x - x0) / (4 * sigma * sigma))
        Dim oscillation As Complex = Complex.Exp(Complex.ImaginaryOne * k0 * x)
        Dim timeEvolution As Complex = Complex.Exp(-Complex.ImaginaryOne * ħ * k0 * k0 * t / (2 * m))
        
        Dim normalization As Double = 1 / Math.Sqrt(sigma * Math.Sqrt(2 * PI))
        Return normalization * envelope * oscillation * timeEvolution
    End Function
    
    ' Mathematical helper functions
    Private Function HermitePolynomial(n As Integer, x As Double) As Double
        ' Calculate Hermite polynomial H_n(x)
        Select Case n
            Case 0 : Return 1
            Case 1 : Return 2 * x
            Case 2 : Return 4 * x * x - 2
            Case 3 : Return 8 * x * x * x - 12 * x
            Case 4 : Return 16 * x * x * x * x - 48 * x * x + 12
            Case Else
                ' Recurrence relation
                Dim Hn2 As Double = HermitePolynomial(n - 2, x)
                Dim Hn1 As Double = HermitePolynomial(n - 1, x)
                Return 2 * x * Hn1 - 2 * (n - 1) * Hn2
        End Select
    End Function
    
    Private Function Factorial(n As Integer) As Double
        Dim result As Double = 1
        For i As Integer = 2 To n
            result *= i
        Next
        Return result
    End Function
    
    Public Function CalculateNorm() As Double
        ' Calculate ∫|ψ|² dx
        Dim norm As Double = 0
        Dim dx As Double = 0.01
        
        For x As Double = -10 To 10 Step dx
            Dim psi As Complex = GetValue(x, 0)
            norm += (psi.Real * psi.Real + psi.Imaginary * psi.Imaginary) * dx
        Next
        
        Return norm
    End Function
    
    Public Function SimulateMeasurement() As Double
        ' Monte Carlo sampling of probability distribution
        Dim rng As New Random()
        Dim cumulativeProb As Double = 0
        Dim target As Double = rng.NextDouble()
        Dim dx As Double = 0.01
        
        For x As Double = -10 To 10 Step dx
            Dim psi As Complex = GetValue(x, 0)
            Dim prob As Double = (psi.Real * psi.Real + psi.Imaginary * psi.Imaginary) * dx
            cumulativeProb += prob
            
            If cumulativeProb >= target Then
                LastMeasurement = x
                Return x
            End If
        Next
        
        Return 0
    End Function
    
    Public Sub Collapse(position As Double)
        ' Simulate wave function collapse to position eigenstate
        IsStationary = False
        LastMeasurement = position
    End Sub
    
    Public Function GetQuantumInfo(n As Integer, t As Double) As String
        Dim energy As Double = ħ * 1.0 * (n + 0.5) ' Simplified
        Dim wavelength As Double = 2 * PI / Math.Sqrt(2 * m * energy / (ħ * ħ))
        
        Return $"Quantum State Information:{vbCrLf}" &
               $"Principal Quantum Number: n = {n}{vbCrLf}" &
               $"Energy: E_{n} = {energy:F4} (ħω units){vbCrLf}" &
               $"De Broglie Wavelength: λ = {wavelength:F4}{vbCrLf}" &
               $"Time Evolution Factor: exp(-iE_{n}t/ħ){vbCrLf}" &
               $"Probability Current: Active"
    End Function
End Class


Public Module QuantumPhysics
    ' Fundamental Physical Constants
    Public Const ħ As Double = 1.0545718E-34 ' Reduced Planck constant (J·s)
    Public Const h As Double = 6.62607015E-34 ' Planck constant (J·s)
    Public Const c As Double = 299792458 ' Speed of light (m/s)
    Public Const ε0 As Double = 8.8541878128E-12 ' Vacuum permittivity (F/m)
    Public Const μ0 As Double = 1.25663706212E-6 ' Vacuum permeability (N/A²)
    Public Const e As Double = 1.602176634E-19 ' Elementary charge (C)
    Public Const me As Double = 9.10938356E-31 ' Electron mass (kg)
    Public Const mp As Double = 1.67262192369E-27 ' Proton mass (kg)
    Public Const mn As Double = 1.67492749804E-27 ' Neutron mass (kg)
    Public Const kB As Double = 1.380649E-23 ' Boltzmann constant (J/K)
    Public Const G As Double = 6.67430E-11 ' Gravitational constant (m³/kg·s²)
    Public Const NA As Double = 6.02214076E23 ' Avogadro's number
    Public Const R As Double = 8.314462618 ' Gas constant (J/mol·K)
    Public Const σ As Double = 5.670374419E-8 ' Stefan-Boltzmann constant (W/m²·K⁴)
    Public Const α As Double = 7.2973525693E-3 ' Fine structure constant
    Public Const a0 As Double = 5.29177210903E-11 ' Bohr radius (m)
    
    ' Quantum Mechanics Equations
    Public Function TimeDependentSchrodinger(psi As Complex, V As Double, m As Double, 
                                           x As Double, t As Double) As Complex
        ' iħ ∂ψ/∂t = -ħ²/2m ∇²ψ + Vψ
        Dim laplacian As Double = CalculateLaplacian(psi, x)
        Return Complex.ImaginaryOne * ħ * Complex(0, 1) - 
               (ħ * ħ / (2 * m)) * laplacian + V * psi
    End Function
    
    Public Function CalculateLaplacian(psi As Complex, x As Double) As Double
        ' Second derivative approximation
        Dim dx As Double = 0.001
        Dim psiPlus As Complex = psi + Complex(0, 0) ' Placeholder
        Dim psiMinus As Complex = psi - Complex(0, 0) ' Placeholder
        Return (psiPlus - 2 * psi + psiMinus) / (dx * dx)
    End Function
    
    ' Heisenberg Uncertainty Principle
    Public Function UncertaintyPrinciple(Δx As Double, Δp As Double) As Boolean
        Return Δx * Δp >= ħ / 2
    End Function
    
    ' De Broglie Wavelength
    Public Function DeBroglieWavelength(p As Double) As Double
        Return h / p
    End Function
    
    ' Compton Wavelength
    Public Function ComptonWavelength(m As Double) As Double
        Return h / (m * c)
    End Function
    
    ' Schrödinger Equation Solutions
    
    ' 1. Particle in a Box
    Public Function ParticleInBoxWaveFunction(n As Integer, L As Double, x As Double) As Double
        ' ψ_n(x) = √(2/L) sin(nπx/L)
        If x < 0 Or x > L Then Return 0
        Return Math.Sqrt(2 / L) * Math.Sin(n * PI * x / L)
    End Function
    
    Public Function ParticleInBoxEnergy(n As Integer, L As Double, m As Double) As Double
        ' E_n = (n²π²ħ²)/(2mL²)
        Return (n * n * PI * PI * ħ * ħ) / (2 * m * L * L)
    End Function
    
    ' 2. Quantum Harmonic Oscillator
    Public Function HarmonicOscillatorEnergy(n As Integer, omega As Double) As Double
        ' E_n = ħω(n + 1/2)
        Return ħ * omega * (n + 0.5)
    End Function
    
    ' 3. Hydrogen Atom
    Public Function HydrogenEnergy(n As Integer) As Double
        ' E_n = -13.6 eV / n²
        Return -13.6 / (n * n)
    End Function
    
    Public Function HydrogenWaveFunction(n As Integer, l As Integer, m As Integer, 
                                        r As Double, theta As Double, phi As Double) As Complex
        ' ψ_nlm(r,θ,φ) = R_nl(r)Y_lm(θ,φ)
        Dim radial As Double = HydrogenRadial(n, l, r)
        Dim spherical As Complex = SphericalHarmonic(l, m, theta, phi)
        Return radial * spherical
    End Function
    
    Private Function HydrogenRadial(n As Integer, l As Integer, r As Double) As Double
        ' Simplified radial wave function
        Dim a As Double = a0
        Select Case n
            Case 1
                Return 2 * Math.Exp(-r / a) / Math.Sqrt(4 * PI)
            Case 2
                If l = 0 Then
                    Return (1 / Math.Sqrt(32 * PI * a * a * a)) * (2 - r / a) * Math.Exp(-r / (2 * a))
                ElseIf l = 1 Then
                    Return (1 / Math.Sqrt(32 * PI * a * a * a)) * (r / a) * Math.Exp(-r / (2 * a))
                End If
        End Select
        Return 0
    End Function
    
    Private Function SphericalHarmonic(l As Integer, m As Integer, theta As Double, phi As Double) As Complex
        ' Simplified spherical harmonics
        Select Case l
            Case 0
                Return 1 / Math.Sqrt(4 * PI)
            Case 1
                Select Case m
                    Case 0 : Return Math.Sqrt(3 / (4 * PI)) * Math.Cos(theta)
                    Case 1 : Return -Math.Sqrt(3 / (8 * PI)) * Math.Sin(theta) * Complex.Exp(Complex.ImaginaryOne * phi)
                    Case -1 : Return Math.Sqrt(3 / (8 * PI)) * Math.Sin(theta) * Complex.Exp(-Complex.ImaginaryOne * phi)
                End Select
        End Select
        Return 0
    End Function
    
    ' 4. Tunneling Probability
    Public Function TunnelingProbability(E As Double, V As Double, a As Double, m As Double) As Double
        ' Transmission coefficient for rectangular barrier
        If E > V Then
            Return 1 / (1 + (V * V * Math.Sin(2 * a * Math.Sqrt(2 * m * (E - V)) / ħ) / (4 * E * (E - V))))
        Else
            Dim kappa As Double = Math.Sqrt(2 * m * (V - E)) / ħ
            Return Math.Exp(-2 * kappa * a)
        End If
    End Function
    
    ' 5. Fermi-Dirac Distribution
    Public Function FermiDirac(E As Double, mu As Double, T As Double) As Double
        ' f(E) = 1 / (exp((E-μ)/kBT) + 1)
        Return 1 / (Math.Exp((E - mu) / (kB * T)) + 1)
    End Function
    
    ' 6. Bose-Einstein Distribution
    Public Function BoseEinstein(E As Double, mu As Double, T As Double) As Double
        ' n(E) = 1 / (exp((E-μ)/kBT) - 1)
        Return 1 / (Math.Exp((E - mu) / (kB * T)) - 1)
    End Function
    
    ' Quantum Operators
    Public Class QuantumOperator
        Public Shared Function PositionOperator(psi As Func(Of Double, Complex)) As Func(Of Double, Complex)
            Return Function(x) x * psi(x)
        End Function
        
        Public Shared Function MomentumOperator(psi As Func(Of Double, Complex)) As Func(Of Double, Complex)
            Return Function(x) -Complex.ImaginaryOne * ħ * Derivative(psi, x)
        End Function
        
        Public Shared Function HamiltonianOperator(V As Func(Of Double, Double), 
                                                 psi As Func(Of Double, Complex)) As Func(Of Double, Complex)
            Return Function(x) -(ħ * ħ / (2 * me)) * SecondDerivative(psi, x) + V(x) * psi(x)
        End Function
        
        Private Shared Function Derivative(f As Func(Of Double, Complex), x As Double) As Complex
            Dim dx As Double = 0.001
            Return (f(x + dx) - f(x - dx)) / (2 * dx)
        End Function
        
        Private Shared Function SecondDerivative(f As Func(Of Double, Complex), x As Double) As Complex
            Dim dx As Double = 0.001
            Return (f(x + dx) - 2 * f(x) + f(x - dx)) / (dx * dx)
        End Function
    End Class
    
    ' Quantum Statistical Mechanics
    Public Function PartitionFunction(energies As Double(), T As Double) As Double
        Dim Z As Double = 0
        For Each E In energies
            Z += Math.Exp(-E / (kB * T))
        Next
        Return Z
    End Function
    
    Public Function ThermalAverage(A As Double(), energies As Double(), T As Double) As Double
        Dim Z As Double = PartitionFunction(energies, T)
        Dim sum As Double = 0
        For i As Integer = 0 To energies.Length - 1
            sum += A(i) * Math.Exp(-energies(i) / (kB * T))
        Next
        Return sum / Z
    End Function
    
    ' Quantum Information
    Public Function QubitState(theta As Double, phi As Double) As Complex()
        ' |ψ⟩ = cos(θ/2)|0⟩ + e^(iφ)sin(θ/2)|1⟩
        Return New Complex() {
            Math.Cos(theta / 2),
            Complex.Exp(Complex.ImaginaryOne * phi) * Math.Sin(theta / 2)
        }
    End Function
    
    Public Function DensityMatrix(psi As Complex()) As Complex(,)
        Dim rho(psi.Length - 1, psi.Length - 1) As Complex
        For i As Integer = 0 To psi.Length - 1
            For j As Integer = 0 To psi.Length - 1
                rho(i, j) = psi(i) * Complex.Conjugate(psi(j))
            Next
        Next
        Return rho
    End Function
    
    ' Relativistic Quantum Mechanics
    Public Function KleinGordonEquation(phi As Complex, m As Double) As Complex
        ' (∂²/∂t² - ∇² + m²)φ = 0
        Return Complex.Zero ' Simplified
    End Function
    
    Public Function DiracEquation(psi As Complex(), m As Double) As Complex()
        ' (iγᵘ∂ᵘ - m)ψ = 0
        Return New Complex() {Complex.Zero, Complex.Zero, Complex.Zero, Complex.Zero}
    End Function
    
    ' Path Integral Formulation
    Public Function Propagator(xf As Double, xi As Double, t As Double, m As Double) As Complex
        ' K(xf, xi; t) = ⟨xf|exp(-iHt/ħ)|xi⟩
        Dim S As Double = ClassicalAction(xf, xi, t, m)
        Return Complex.Exp(Complex.ImaginaryOne * S / ħ)
    End Function
    
    Private Function ClassicalAction(xf As Double, xi As Double, t As Double, m As Double) As Double
        ' S = ∫L dt = ∫(1/2 m v² - V) dt
        Dim v As Double = (xf - xi) / t
        Return 0.5 * m * v * v * t ' Free particle
    End Function
End Module

Imports System.Windows.Forms

Module Program
    <STAThread>
    Sub Main()
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        Application.Run(New QuantumVisualizer())
    End Sub
End Module
