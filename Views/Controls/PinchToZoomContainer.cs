namespace Sphere.Views.Controls;

public class PinchToZoomContainer : ContentView
{
    private double currentScale = 1;
    private double xOffset = 0;
    private double yOffset = 0;

    public PinchToZoomContainer()
    {
        var pinchGesture = new PinchGestureRecognizer();
        pinchGesture.PinchUpdated += OnPinchUpdated;
        GestureRecognizers.Add(pinchGesture);

        var panGesture = new PanGestureRecognizer();
        panGesture.PanUpdated += OnPanUpdated;
        GestureRecognizers.Add(panGesture);
    }

    private void OnPinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
    {
        if (e.Status == GestureStatus.Started)
        {
            Content.AnchorX = 0;
            Content.AnchorY = 0;
        }

        if (e.Status == GestureStatus.Running)
        {
            // Tính scale m?i d?a vào scale ban ??u * scale t? gesture
            currentScale = Math.Max(1, e.Scale * currentScale);
            Content.Scale = currentScale;

            // ?i?u ch?nh v? trí
            double newX = xOffset - ((e.ScaleOrigin.X * Content.Width) * (currentScale - 1));
            double newY = yOffset - ((e.ScaleOrigin.Y * Content.Height) * (currentScale - 1));

            Content.TranslationX = Math.Clamp(newX, -Content.Width * (currentScale - 1), 0);
            Content.TranslationY = Math.Clamp(newY, -Content.Height * (currentScale - 1), 0);
        }

        if (e.Status == GestureStatus.Completed)
        {
            // L?u l?i v? trí sau pinch
            xOffset = Content.TranslationX;
            yOffset = Content.TranslationY;
        }
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (e.StatusType == GestureStatus.Running)
        {
            var maxX = Content.Width * (currentScale - 1);
            var maxY = Content.Height * (currentScale - 1);

            double newX = xOffset + e.TotalX;
            double newY = yOffset + e.TotalY;

            Content.TranslationX = Math.Clamp(newX, -maxX, 0);
            Content.TranslationY = Math.Clamp(newY, -maxY, 0);
        }

        if (e.StatusType == GestureStatus.Completed)
        {
            xOffset = Content.TranslationX;
            yOffset = Content.TranslationY;
        }
    }
}
