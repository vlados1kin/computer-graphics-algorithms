using System.ComponentModel;
using System.Numerics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.WindowsAPICodePack.Dialogs;
using Parser;
using Parser.Models;
using Vector = System.Windows.Vector;

namespace Main;

public class MainViewModel : INotifyPropertyChanged
{
    public Scene Scene { get; set; } = new();

    private WriteableBitmap? _writeableBitmap;

    public WriteableBitmap? WriteableBitmap
    {
        get => _writeableBitmap;
        set
        {
            _writeableBitmap = value;
            OnPropertyChanged(nameof(WriteableBitmap));
        }
    }

    private Color _foregroundColor = Colors.White;

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

    private Color _backgroundColor = Colors.Black;

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

    private string _selectedModelInfo = string.Empty;

    public string SelectedModelInfo
    {
        get => _selectedModelInfo;
        set
        {
            _selectedModelInfo = value;
            OnPropertyChanged(nameof(SelectedModelInfo));
        }
    }

    public ICommand LoadFileCommand { get; }
    public ICommand MouseWheelCommand { get; }
    public ICommand MouseMoveCommand { get; }
    public ICommand MouseLeftButtonDownCommand { get; }
    public ICommand MouseRightButtonDownCommand { get; }
    public ICommand KeyDownCommand { get; }
    
    private Point _lastMousePos;
    private float RotateSensitivity => MathF.PI / 360.0f;

    public MainViewModel()
    {
        Scene.Camera = new Camera();

        Scene.CanvasWidth = 800;
        Scene.CanvasHeight = 600;

        LoadFileCommand = new CommandHandlers(_ => LoadFile());

        MouseWheelCommand = new CommandHandlers(OnMouseWheel);
        MouseMoveCommand = new CommandHandlers(OnMouseMove);
        MouseLeftButtonDownCommand = new CommandHandlers(OnMouseLeftButtonDown);
        MouseRightButtonDownCommand = new CommandHandlers(OnMouseRightButtonDown);
        KeyDownCommand = new CommandHandlers(OnKeyDown);
    }

    private void LoadFile()
    {
        using var dlg = new CommonOpenFileDialog();
        dlg.Filters.Add(new CommonFileDialogFilter("OBJ Files", "*.obj"));
        if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
        {
            try
            {
                var loadedModel = ObjParser.Parse(dlg.FileName!);
                WriteableBitmap ??= new WriteableBitmap(
                    Scene.CanvasWidth, Scene.CanvasHeight, 96, 96, PixelFormats.Bgra32, null);

                Scene.Models.Add(loadedModel);
                Scene.SelectedModel = loadedModel;
                UpdateView();
                OnPropertyChanged(nameof(Scene));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки файла: " + ex.Message);
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
                    Point currentPos = e.GetPosition(null);
                    Vector delta = currentPos - _lastMousePos;
                    if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                    {
                        Scene.SelectedModel.Rotation = new Vector3(
                            Scene.SelectedModel.Rotation.X,
                            Scene.SelectedModel.Rotation.Y,
                            Scene.SelectedModel.Rotation.Z - (float)delta.X * RotateSensitivity);
                    }
                    else
                    {
                        Scene.SelectedModel.Rotation = new Vector3(
                            Scene.SelectedModel.Rotation.X + (float)delta.Y * RotateSensitivity,
                            Scene.SelectedModel.Rotation.Y + (float)delta.X * RotateSensitivity,
                            Scene.SelectedModel.Rotation.Z);
                    }

                    _lastMousePos = currentPos;

                    UpdateView();
                    OnPropertyChanged(nameof(Scene));
                }
            }

            if (e.RightButton == MouseButtonState.Pressed && e.LeftButton != MouseButtonState.Pressed)
            {
                var currentPos = e.GetPosition(null);

                float xOffset = (float)(currentPos.X - _lastMousePos.X);
                float yOffset = (float)(currentPos.Y - _lastMousePos.Y);


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
            Point clickPoint = _lastMousePos;
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
        if (WriteableBitmap == null) return;

        Renderer.ClearBitmap(WriteableBitmap, BackgroundColor);

        Scene.Camera.ChangeEye();
        Scene.UpdateAllModels();

        foreach (var model in Scene.Models)
        {
            Renderer.DrawWireframe(model, WriteableBitmap, ForegroundColor, Scene.Camera);
        }
        
        OnPropertyChanged(nameof(WriteableBitmap));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}