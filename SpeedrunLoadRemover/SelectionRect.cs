using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;

namespace WpfApp1 {
    class SelectionRect{
        Rectangle rect = new Rectangle();
        Canvas canvas = new Canvas();
        Point last_mouse = new Point();
        Size last_real_player_size = new Size();
        Size last_rect = new Size();

        public SelectionRect() {
            canvas.Children.Add(rect);
        }
        private static Size FitToRect(double w, double h, double W, double H) {
            double r = w / h;
            double R = W / H;
            if (R > r) return new Size(w * H / h, H); else return new Size(W, h * W / w);
        }

        private void SetTopLeft(MouseButtonEventArgs e, MediaElement mediaElement) {
            last_mouse = Mouse.GetPosition(canvas);
            last_real_player_size = new Size(mediaElement.ActualWidth, mediaElement.ActualHeight);
            last_rect = rect.RenderSize;
            Canvas.SetLeft(rect, last_mouse.X);
            //lvList.Items.Insert(0, "mouse : " + last_mouse);
            last_mouse = new Point(last_mouse.X / canvas.Width, last_mouse.Y / canvas.Height);
        }
    }
}
