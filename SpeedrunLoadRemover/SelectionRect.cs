using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;

namespace WpfApp1 {
    class ProportionalRect {
        public double x {get; set;}
        public double y { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public ProportionalRect() {
            x = 0;
            y = 0;
            X = 1;
            Y = 1;
        }
        public ProportionalRect(double x, double y, double X, double Y) {
            this.x = x;
            this.y = y;
            this.X = X;
            this.Y = Y;
        }
    }
    class SelectionRect {
        Rectangle rect = new Rectangle();
        Canvas canvas = new Canvas();
        List<Rectangle> handles = new List<Rectangle>();
        public ProportionalRect video_bounds { get; set; }
        double handle_size = 7;

        public SelectionRect(Canvas c) {
            canvas = c;
            canvas.Children.Add(rect);
            video_bounds = new ProportionalRect();
            rect.Width = 100;
            rect.Height = 50;
            Canvas.SetLeft(rect, 100);
            Canvas.SetTop(rect, 100);
            rect.StrokeThickness = 2;

            //rect.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.AliceBlue);
            rect.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
            int name_number = 1;

            for (double y = 1; y >= 0; y -= 0.5) {
                for (double x = 0; x <= 1; x += 0.5) {
                    if (name_number != 5) {
                        Rectangle handle = new Rectangle();
                        handle.Name = "handle_" + name_number;
                        handle.Width = handle_size;
                        handle.Height = handle_size;
                        handle.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                        handle.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Transparent);
                        Canvas.SetLeft(handle, x * rect.Width + Canvas.GetLeft(rect) - handle_size / 2);
                        Canvas.SetTop(handle, y * rect.Height + Canvas.GetTop(rect) - handle_size / 2);
                        canvas.Children.Add(handle);
                        handle.PreviewMouseMove += handle_PreviewMouseMove;
                        handle.PreviewMouseLeftButtonDown += handle_PreviewMouseLeftButtonDown;
                        handle.PreviewMouseLeftButtonUp += handle_PreviewMouseLeftButtonUp;


                        handles.Add(handle);
                    }
                    name_number++;
                }

            }
            SolveRect();
        }

        private enum Freedom {
            LeftRight,
            UpDown,
            Both,
        }

        private void handle_PreviewMouseLeftButtonDown(object sender, MouseEventArgs e) {
            Rectangle h = (Rectangle)sender;
            h.CaptureMouse();
            Debug.WriteLine(sender);
        }

        private void handle_PreviewMouseLeftButtonUp(object sender, MouseEventArgs e) {
            Rectangle r = (Rectangle)sender;
            r.ReleaseMouseCapture();
        }
        private void handle_PreviewMouseMove(object sender, MouseEventArgs e) {
            if (Mouse.LeftButton == MouseButtonState.Pressed) {
                Rectangle h = (Rectangle)sender;
                char n = h.Name.Last();
                switch (n) {
                    case '1': {
                            MoveHandleByNum('1', Freedom.Both);
                            MoveHandleByNum('7', Freedom.LeftRight);
                            MoveHandleByNum('3', Freedom.UpDown);
                            break;
                        }
                    case '2': {
                            MoveHandleByNum('2', Freedom.UpDown);
                            MoveHandleByNum('1', Freedom.UpDown);
                            MoveHandleByNum('3', Freedom.UpDown);
                            break;
                        }
                    case '3': {
                            MoveHandleByNum('3', Freedom.Both);
                            MoveHandleByNum('9', Freedom.LeftRight);
                            MoveHandleByNum('1', Freedom.UpDown);
                            break;
                        }
                    case '4': {
                            MoveHandleByNum('4', Freedom.LeftRight);
                            MoveHandleByNum('7', Freedom.LeftRight);
                            MoveHandleByNum('1', Freedom.LeftRight);
                            break;
                        }
                    case '6': {
                            MoveHandleByNum('6', Freedom.LeftRight);
                            MoveHandleByNum('3', Freedom.LeftRight);
                            MoveHandleByNum('9', Freedom.LeftRight);
                            break;
                        }
                    case '7': {
                            MoveHandleByNum('7', Freedom.Both);
                            MoveHandleByNum('1', Freedom.LeftRight);
                            MoveHandleByNum('9', Freedom.UpDown);
                            break;
                        }
                    case '8': {
                            MoveHandleByNum('8', Freedom.UpDown);
                            MoveHandleByNum('9', Freedom.UpDown);
                            MoveHandleByNum('7', Freedom.UpDown);
                            break;
                        }
                    case '9': {
                            MoveHandleByNum('9', Freedom.Both);
                            MoveHandleByNum('3', Freedom.LeftRight);
                            MoveHandleByNum('7', Freedom.UpDown);
                            break;
                        }
                    default:
                        break;
                }
                SolveHandleByNum('2');
                SolveHandleByNum('4');
                SolveHandleByNum('6');
                SolveHandleByNum('8');
                SolveRect();

            }
        }

        private void MoveHandleByNum(char n, Freedom f) {
            Rectangle h = handles.Find(i => i.Name.Last() == n);
            switch (f) {
                case Freedom.LeftRight:
                    Canvas.SetLeft(h, Mouse.GetPosition(canvas).X - handle_size/2);
                    break;
                case Freedom.UpDown:
                    Canvas.SetTop(h, Mouse.GetPosition(canvas).Y - handle_size/2);
                    break;
                case Freedom.Both:
                    Canvas.SetLeft(h, Mouse.GetPosition(canvas).X - handle_size/2);
                    Canvas.SetTop(h, Mouse.GetPosition(canvas).Y - handle_size/2);
                    break;
                default:
                    break;
            }
            ClampHandleByNum(n);
        }

        private void SolveHandleByNum(char n) {
            double left = 0, top = 0;
            switch (n) {
                case '2': {
                        char n1 = '1';
                        char n2 = '3';
                        Rectangle h1 = handles.Find(i => i.Name.Last() == n1);
                        Rectangle h2 = handles.Find(i => i.Name.Last() == n2);
                        left = (Canvas.GetLeft(h1) + Canvas.GetLeft(h2)) / 2;
                        top = Canvas.GetTop(h1);
                        break;
                    }
                case '4': {
                        char n1 = '1';
                        char n2 = '7';
                        Rectangle h1 = handles.Find(i => i.Name.Last() == n1);
                        Rectangle h2 = handles.Find(i => i.Name.Last() == n2);
                        left = Canvas.GetLeft(h1);
                        top = (Canvas.GetTop(h1) + Canvas.GetTop(h2)) / 2;
                        break;
                    }
                case '6': {
                        char n1 = '3';
                        char n2 = '9';
                        Rectangle h1 = handles.Find(i => i.Name.Last() == n1);
                        Rectangle h2 = handles.Find(i => i.Name.Last() == n2);
                        left = Canvas.GetLeft(h1);
                        top = (Canvas.GetTop(h1) + Canvas.GetTop(h2)) / 2;
                        break;
                    }
                case '8': {
                        char n1 = '7';
                        char n2 = '9';
                        Rectangle h1 = handles.Find(i => i.Name.Last() == n1);
                        Rectangle h2 = handles.Find(i => i.Name.Last() == n2);
                        left = (Canvas.GetLeft(h1) + Canvas.GetLeft(h2)) / 2;
                        top = Canvas.GetTop(h1);
                        break;
                    }
                default:
                    break;
            }
            Rectangle h = handles.Find(i => i.Name.Last() == n);
            Canvas.SetLeft(h, left);
            Canvas.SetTop(h, top);
        }

        private void ClampHandleByNum(char n) {
            Rectangle h = handles.Find(i => i.Name.Last() == n);
            switch (n) {
                case '1': {
                        Rectangle h2 = handles.Find(i => i.Name.Last() == '9');
                        if (Canvas.GetLeft(h) > Canvas.GetLeft(h2) - handle_size * 3) Canvas.SetLeft(h, Canvas.GetLeft(h2) - handle_size * 3);
                        if (Canvas.GetTop(h) < Canvas.GetTop(h2) + handle_size * 3) Canvas.SetTop(h, Canvas.GetTop(h2) + handle_size * 3);
                        if (Canvas.GetLeft(h) < video_bounds.x * canvas.ActualWidth)
                            Canvas.SetLeft(h, video_bounds.x * canvas.ActualWidth);
                        if (Canvas.GetTop(h) > video_bounds.Y * canvas.ActualHeight - handle_size)
                            Canvas.SetTop(h, video_bounds.Y * canvas.ActualHeight - handle_size);
                        break;
                    }
                case '3': {
                        Rectangle h2 = handles.Find(i => i.Name.Last() == '7');
                        if (Canvas.GetLeft(h) < Canvas.GetLeft(h2) + handle_size * 3) Canvas.SetLeft(h, Canvas.GetLeft(h2) + handle_size * 3);
                        if (Canvas.GetTop(h) < Canvas.GetTop(h2) + handle_size * 3) Canvas.SetTop(h, Canvas.GetTop(h2) + handle_size * 3);
                        if (Canvas.GetLeft(h) > video_bounds.X * canvas.ActualWidth - handle_size)
                            Canvas.SetLeft(h, video_bounds.X * canvas.ActualWidth - handle_size);
                        if (Canvas.GetTop(h) > video_bounds.Y * canvas.ActualHeight - handle_size)
                            Canvas.SetTop(h, video_bounds.Y * canvas.ActualHeight - handle_size);
                        break;
                    }
                case '7': {
                        Rectangle h2 = handles.Find(i => i.Name.Last() == '3');
                        if (Canvas.GetLeft(h) > Canvas.GetLeft(h2) - handle_size * 3) Canvas.SetLeft(h, Canvas.GetLeft(h2) - handle_size * 3);
                        if (Canvas.GetTop(h) > Canvas.GetTop(h2) - handle_size * 3) Canvas.SetTop(h, Canvas.GetTop(h2) - handle_size * 3);
                        if (Canvas.GetLeft(h) < video_bounds.x * canvas.ActualWidth)
                            Canvas.SetLeft(h, video_bounds.x * canvas.ActualWidth);
                        if (Canvas.GetTop(h) < video_bounds.y * canvas.ActualHeight)
                            Canvas.SetTop(h, video_bounds.y * canvas.ActualHeight);
                        break;
                    }
                case '9': {
                        Rectangle h2 = handles.Find(i => i.Name.Last() == '1');
                        if (Canvas.GetLeft(h) < Canvas.GetLeft(h2) + handle_size * 3) Canvas.SetLeft(h, Canvas.GetLeft(h2) + handle_size * 3);
                        if (Canvas.GetTop(h) > Canvas.GetTop(h2) - handle_size * 3) Canvas.SetTop(h, Canvas.GetTop(h2) - handle_size * 3);
                        if (Canvas.GetLeft(h) > video_bounds.X * canvas.ActualWidth - handle_size)
                            Canvas.SetLeft(h, video_bounds.X * canvas.ActualWidth - handle_size);
                        if (Canvas.GetTop(h) < video_bounds.y * canvas.ActualHeight)
                            Canvas.SetTop(h, video_bounds.y * canvas.ActualHeight);
                        break;
                    }
                default:
                    break;
            }
        }

        private void SolveRect() {
            char n1 = '7';
            char n2 = '3';
            Rectangle h1 = handles.Find(i => i.Name.Last() == n1);
            Rectangle h2 = handles.Find(i => i.Name.Last() == n2);
            Canvas.SetLeft(rect, Canvas.GetLeft(h1));
            Canvas.SetTop(rect, Canvas.GetTop(h1));
            rect.Width = (Canvas.GetLeft(h2) - Canvas.GetLeft(h1) + handle_size);
            rect.Height = (Canvas.GetTop(h2) - Canvas.GetTop(h1) + handle_size);

        }

        private static Size FitToRect(double w, double h, double W, double H) {
            double r = w / h;
            double R = W / H;
            if (R > r) return new Size(w * H / h, H); else return new Size(W, h * W / w);
        }

    }
}
