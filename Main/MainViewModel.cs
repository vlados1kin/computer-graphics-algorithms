using System.ComponentModel;
using System.Numerics;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Main.Models;
using Main.Parser;
using Microsoft.WindowsAPICodePack.Dialogs;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace Main;

public class MainViewModel : INotifyPropertyChanged
{
    private Color _backgroundColor = Colors.Black;

    private Color _foregroundColor = Colors.White;
    
    private Point _lastMousePos;
    
    private RenderMode _selectedRenderMode;

    private WriteableBitmap? _writeableBitmap;
    
    private CubeMap? _cubeMap;

    public MainViewModel()
    {
        Scene.Camera = new Camera();

        Scene.CanvasWidth = 800;
        Scene.CanvasHeight = 600;

        LoadFileCommand = new CommandsHandler(_ => LoadFile());
        MouseWheelCommand = new CommandsHandler(OnMouseWheel);
        MouseMoveCommand = new CommandsHandler(OnMouseMove);
        MouseLeftButtonDownCommand = new CommandsHandler(OnMouseLeftButtonDown);
        MouseRightButtonDownCommand = new CommandsHandler(OnMouseRightButtonDown);
        KeyDownCommand = new CommandsHandler(OnKeyDown);
        LoadCubeMapCommand = new CommandsHandler(_ => LoadCubeMap());

        SelectedRenderMode = RenderMode.Wireframe;
        Scene.Lights.Add(new Light());
    }

    public Scene Scene { get; set; } = new();

    public WriteableBitmap? WriteableBitmap
    {
        get => _writeableBitmap;
        set
        {
            _writeableBitmap = value;
            OnPropertyChanged(nameof(WriteableBitmap));
        }
    }

    public Color ForegroundColor
    {
        get => _foregroundColor;
        set
        {
            _foregroundColor = value;
            UpdateView();
            OnPropertyChanged(nameof(ForegroundColor));
        }
    }

    public Color BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            _backgroundColor = value;
            UpdateView();
            OnPropertyChanged(nameof(BackgroundColor));
        }
    }

    public CubeMap? CubeMap
    {
        get => _cubeMap;
        set
        {
            _cubeMap = value;
            UpdateView();
            OnPropertyChanged(nameof(CubeMap));
        }
    }

    public ICommand LoadFileCommand { get; }
    public ICommand MouseWheelCommand { get; }
    public ICommand MouseMoveCommand { get; }
    public ICommand MouseLeftButtonDownCommand { get; }
    public ICommand MouseRightButtonDownCommand { get; }
    public ICommand KeyDownCommand { get; }
    public ICommand LoadCubeMapCommand { get; }
    
    private float RotateSensitivity => MathF.PI / 360.0f;
    
    public RenderMode SelectedRenderMode
    {
        get => _selectedRenderMode;
        set
        {
            _selectedRenderMode = value;
            UpdateView();
            OnPropertyChanged(nameof(SelectedRenderMode));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void LoadFile()
    {
        using var dialog = new CommonOpenFileDialog();
        dialog.Filters.Add(new CommonFileDialogFilter("OBJ Files", "*.obj"));
        if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
        {
            try
            {
                var loadedModel = ObjParser.Parse(dialog.FileName!);
                WriteableBitmap ??= new WriteableBitmap(
                    Scene.CanvasWidth, Scene.CanvasHeight, 96, 96, PixelFormats.Bgra32, null);

                Scene.Models.Add(loadedModel);
                Scene.SelectedModel = loadedModel;
                UpdateView();
                OnPropertyChanged(nameof(Scene));
            }
            catch (Exception exception)
            {
                MessageBox.Show("Ошибка загрузки файла: " + exception.Message);
            }
        }
    }

    private void LoadCubeMap()
    {
        using var dialog = new CommonOpenFileDialog();
        if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
        {
            try
            {
                CubeMap = new CubeMap(dialog.FileName!);
                UpdateView();
                OnPropertyChanged(nameof(Scene));
            }
            catch (Exception exception)
            {
                MessageBox.Show("Ошибка загрузки файла: " + exception.Message);
            }
        }
    }

    private void OnMouseWheel(object? parameter)
    {
        if (parameter is MouseWheelEventArgs e)
        {
            if (Scene.SelectedModel != null &&
                (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
            {
                if (e.Delta > 0)
                    Scene.SelectedModel.Scale += Scene.SelectedModel.Delta;
                else
                    Scene.SelectedModel.Scale -= Scene.SelectedModel.Delta;
            }
            else
            {
                Scene.Camera.Radius -= e.Delta / 1000.0f;
                if (Scene.Camera.Radius < Scene.Camera.ZNear)
                    Scene.Camera.Radius = Scene.Camera.ZNear;
                if (Scene.Camera.Radius > Scene.Camera.ZFar)
                    Scene.Camera.Radius = Scene.Camera.ZFar;
            }

            e.Handled = true;

            UpdateView();
            OnPropertyChanged(nameof(Scene));
        }
    }

    private void OnMouseMove(object? parameter)
    {
        if (parameter is MouseEventArgs e)
        {
            if (Scene.SelectedModel != null)
            {
                if (e.LeftButton == MouseButtonState.Pressed && e.RightButton != MouseButtonState.Pressed)
                {
                    var currentPos = e.GetPosition(null);
                    var delta = currentPos - _lastMousePos;
                    if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                        Scene.SelectedModel.Rotation = new Vector3(
                            Scene.SelectedModel.Rotation.X,
                            Scene.SelectedModel.Rotation.Y,
                            Scene.SelectedModel.Rotation.Z - (float)delta.X * RotateSensitivity);
                    else
                        Scene.SelectedModel.Rotation = new Vector3(
                            Scene.SelectedModel.Rotation.X + (float)delta.Y * RotateSensitivity,
                            Scene.SelectedModel.Rotation.Y + (float)delta.X * RotateSensitivity,
                            Scene.SelectedModel.Rotation.Z);

                    _lastMousePos = currentPos;

                    UpdateView();
                    OnPropertyChanged(nameof(Scene));
                }
            }

            if (e.RightButton == MouseButtonState.Pressed && e.LeftButton != MouseButtonState.Pressed)
            {
                var currentPos = e.GetPosition(null);

                var xOffset = (float)(currentPos.X - _lastMousePos.X);
                var yOffset = (float)(currentPos.Y - _lastMousePos.Y);

                Scene.Camera.Zeta -= yOffset * 0.005f;
                Scene.Camera.Phi += xOffset * 0.005f;

                if (Scene.Camera.Zeta >= Math.PI)
                    Scene.Camera.Zeta = (float)Math.PI - 0.01f;
                if (Scene.Camera.Zeta <= 0)
                    Scene.Camera.Zeta = 0.01f;

                _lastMousePos = currentPos;
                UpdateView();
                OnPropertyChanged(nameof(Scene));
            }
        }
    }

    private void OnMouseLeftButtonDown(object? parameter)
    {
        if (parameter is MouseButtonEventArgs e)
        {
            _lastMousePos = e.GetPosition(null);
            if (e.OriginalSource is UIElement uiElement)
            {
                uiElement.Focus();
            }
        }
    }

    private void OnMouseRightButtonDown(object? parameter)
    {
        if (parameter is MouseButtonEventArgs e)
        {
            _lastMousePos = e.GetPosition(null);
            var clickPoint = _lastMousePos;
            var pickedModel = Scene.PickModel(clickPoint);
            Scene.SelectedModel = pickedModel;
            UpdateView();
            OnPropertyChanged(nameof(Scene));
        }
    }

    private void OnKeyDown(object? parameter)
    {
        if (parameter is KeyEventArgs e)
        {
            if (Scene.SelectedModel != null)
            {
                if (e.Key == Key.Delete)
                {
                    Scene.Models.Remove(Scene.SelectedModel);
                    Scene.SelectedModel = Scene.Models.FirstOrDefault();

                    UpdateView();
                    OnPropertyChanged(nameof(Scene));
                    return;
                }

                var step = Scene.SelectedModel.GetOptimalTranslationStep();

                switch (e.Key)
                {
                    case Key.Right:
                        Scene.SelectedModel.Translation += new Vector3(step.X, 0, 0);
                        break;
                    case Key.Left:
                        Scene.SelectedModel.Translation += new Vector3(-step.X, 0, 0);
                        break;
                    case Key.Up:
                        Scene.SelectedModel.Translation += new Vector3(0, step.Y, 0);
                        break;
                    case Key.Down:
                        Scene.SelectedModel.Translation += new Vector3(0, -step.Y, 0);
                        break;
                    case Key.S:
                        Scene.SelectedModel.Translation += new Vector3(0, 0, -step.Z);
                        break;
                    case Key.W:
                        Scene.SelectedModel.Translation += new Vector3(0, 0, step.Z);
                        break;
                }
            }
            else
            {
                switch (e.Key)
                {
                    case Key.Left:
                        Scene.Camera.Target += new Vector3(-0.5f, 0, 0);
                        break;
                    case Key.Right:
                        Scene.Camera.Target += new Vector3(0.5f, 0, 0);
                        break;
                    case Key.Up:
                        Scene.Camera.Target += new Vector3(0.0f, 0.5f, 0);
                        break;
                    case Key.Down:
                        Scene.Camera.Target += new Vector3(0.0f, -0.5f, 0);
                        break;
                }
            }

            UpdateView();
            OnPropertyChanged(nameof(Scene));
        }
    }

    private void UpdateView()
    {
        Renderer.Renderer.Render(Scene, CubeMap, WriteableBitmap, BackgroundColor, ForegroundColor, SelectedRenderMode);
        
        OnPropertyChanged(nameof(WriteableBitmap));
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}