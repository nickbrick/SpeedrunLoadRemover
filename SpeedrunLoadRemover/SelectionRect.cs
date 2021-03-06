﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;

namespace SLR {
    class LogicalRect {
        public double x { get; set; }
        public double y { get; set; }
        public double X { get; set; }
        public double Y { get; set; }

        public LogicalRect() {
            x = 0;
            y = 0;
            X = 1;
            Y = 1;
        }
        public LogicalRect(double x, double y, double X, double Y) {
            Contract.Requires(x <= X);
            Contract.Requires(y <= Y);
            this.x = x;
            this.y = y;
            this.X = X;
            this.Y = Y;
        }

        public static LogicalRect operator * (LogicalRect rect, Size s) {
            return new LogicalRect(rect.x * s.Width,
                                    rect.y * s.Height,
                                    rect.X * s.Width,
                                    rect.Y * s.Height);
        }
        public static LogicalRect operator / (LogicalRect rect, Size s) {
            Contract.Requires(s.Width != 0 && s.Height != 0);
            return new LogicalRect(rect.x / s.Width,
                                    rect.y / s.Height,
                                    rect.X / s.Width,
                                    rect.Y / s.Height);
        }
        public System.Drawing.Rectangle ToRectangle() {
            var rectangle = new System.Drawing.Rectangle();
            rectangle.X = (int)Math.Round(x);
            rectangle.Y = (int)Math.Round(y);
            rectangle.Width = (int)Math.Round(X-x);
            rectangle.Height = (int)Math.Round(Y-y);
            return rectangle;
        }
    }
    class SelectionRect {
        Border shape = new Border();
        Rectangle dash = new Rectangle();
        public LogicalRect prop_rect = new LogicalRect();
        public LogicalRect abs_rect = new LogicalRect();
        Canvas canvas = new Canvas();
        List<Rectangle> handles = new List<Rectangle>();
        double handle_size = 7;
        Rectangle top_left;
        Rectangle bottom_right;
        System.Windows.Media.SolidColorBrush stroke_enabled = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.OrangeRed);
        System.Windows.Media.SolidColorBrush fill_enabled = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
        private Point init_mouse = new Point();

        public SelectionRect(Canvas c) {
            canvas = c;
            AbsFromProp();

            canvas.Children.Add(shape);
            shape.Width = canvas.ActualWidth - handle_size;
            shape.Height = canvas.ActualHeight - handle_size;
            Canvas.SetLeft(shape, 0);
            Canvas.SetTop(shape, 0);
            shape.BorderThickness = new Thickness(1);

            shape.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DarkOrange);

            StringBuilder sb = new StringBuilder();
            sb.Append(@"<Rectangle xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' 
                            xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' StrokeDashArray='4 4' Stroke='Gray' StrokeThickness='1' 
            Width = '{Binding RelativeSource={RelativeSource AncestorType={x:Type Border}}, Path=ActualWidth}'
                  Height = '{Binding RelativeSource={RelativeSource AncestorType={x:Type Border}}, Path=ActualHeight}' Margin='-1'/>");
            dash = (Rectangle)System.Windows.Markup.XamlReader.Parse(sb.ToString());
            shape.Child = dash;
            dash.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Transparent);
            dash.Cursor = Cursors.SizeAll;
            dash.PreviewMouseLeftButtonDown += Shape_PreviewMouseLeftButtonDown;
            dash.PreviewMouseMove += Shape_PreviewMouseMove;
            dash.PreviewMouseLeftButtonUp += Shape_PreviewMouseLeftButtonUp;


            int name_number = 1;

            for (double y = 1; y >= 0; y -= 0.5) {
                for (double x = 0; x <= 1; x += 0.5) {
                    if (name_number != 5) {
                        Rectangle handle = new Rectangle();
                        handle.Name = "handle_" + name_number;
                        handle.Width = handle_size;
                        handle.Height = handle_size;
                        handle.Stroke = stroke_enabled;
                        handle.Fill = fill_enabled;
                        handle.StrokeThickness = 1;
                        Canvas.SetLeft(handle, x * shape.Width + Canvas.GetLeft(shape) );
                        Canvas.SetTop(handle, y * shape.Height + Canvas.GetTop(shape) );
                        canvas.Children.Add(handle);
                        handle.PreviewMouseMove += handle_PreviewMouseMove;
                        handle.PreviewMouseLeftButtonDown += handle_PreviewMouseLeftButtonDown;
                        handle.PreviewMouseLeftButtonUp += handle_PreviewMouseLeftButtonUp;
                        if (name_number == 7) top_left = handle;
                        if (name_number == 3) bottom_right = handle;
                        
                        switch (name_number) {
                            case 1:
                            case 9: {
                                    handle.Cursor = Cursors.SizeNESW;
                                    break;
                                }
                            case 3:
                            case 7: {
                                    handle.Cursor = Cursors.SizeNWSE;
                                    break;
                                }
                            case 2:
                            case 8: {
                                    handle.Cursor = Cursors.SizeNS;
                                    break;
                                }
                            case 4:
                            case 6: {
                                    handle.Cursor = Cursors.SizeWE;
                                    break;
                                }

                            default:
                                break;
                        }

                        handles.Add(handle);
                    }
                    name_number++;
                }

            }
            ShapeFromHandles();
        }


        private void Shape_PreviewMouseMove(object sender, MouseEventArgs e) {
            if (Mouse.LeftButton == MouseButtonState.Pressed) {
                Canvas.SetLeft(shape, Mouse.GetPosition(canvas).X - init_mouse.X);
                if (Mouse.GetPosition(canvas).X - init_mouse.X < 0)
                    Canvas.SetLeft(shape, 0);
                if (Mouse.GetPosition(canvas).X - init_mouse.X > canvas.ActualWidth - shape.ActualWidth)
                    Canvas.SetLeft(shape, canvas.ActualWidth - shape.ActualWidth);
                Canvas.SetTop(shape, Mouse.GetPosition(canvas).Y - init_mouse.Y);
                if (Mouse.GetPosition(canvas).Y - init_mouse.Y < 0)
                    Canvas.SetTop(shape, 0);
                if (Mouse.GetPosition(canvas).Y - init_mouse.Y > canvas.ActualHeight - shape.ActualHeight)
                    Canvas.SetTop(shape, canvas.ActualHeight - shape.ActualHeight);
                AbsFromShape();
                PropFromAbs();
                HandlesFromAbs();
                

            }
        }

        private void Shape_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            Rectangle s = (Rectangle)sender;
            s.CaptureMouse();
            init_mouse = Mouse.GetPosition(shape);
        }

        private void Shape_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            Rectangle s = (Rectangle)sender;
            s.ReleaseMouseCapture();
        }

        public void AbsFromProp() {
            abs_rect = prop_rect * new Size(canvas.ActualWidth, canvas.ActualHeight);
        }

        public void PropFromAbs() {
            prop_rect = abs_rect / new Size(canvas.ActualWidth, canvas.ActualHeight);
        }

        private enum Freedom {
            LeftRight,
            UpDown,
            Both,
        }

        private void handle_PreviewMouseLeftButtonDown(object sender, MouseEventArgs e) {
            Rectangle h = (Rectangle)sender;
            h.CaptureMouse();
            init_mouse = Mouse.GetPosition(h);

            Debug.WriteLine(sender);
        }

        private void handle_PreviewMouseLeftButtonUp(object sender, MouseEventArgs e) {
            Rectangle h = (Rectangle)sender;
            h.ReleaseMouseCapture();
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
                AbsFromHandles();
                PropFromAbs();
                ShapeFromHandles();

            }
        }

        private void MoveHandleByNum(char n, Freedom f) {
            Rectangle h = handles.Find(i => i.Name.Last() == n);
            switch (f) {
                case Freedom.LeftRight:
                    Canvas.SetLeft(h, Mouse.GetPosition(canvas).X - handle_size/2 - init_mouse.X);
                    break;
                case Freedom.UpDown:
                    Canvas.SetTop(h, Mouse.GetPosition(canvas).Y - handle_size/2 - init_mouse.Y);
                    break;
                case Freedom.Both:
                    Canvas.SetLeft(h, Mouse.GetPosition(canvas).X - handle_size/2 - init_mouse.X);
                    Canvas.SetTop(h, Mouse.GetPosition(canvas).Y - handle_size/2 - init_mouse.Y);
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
                case '1': {
                        char n1 = '7';
                        char n2 = '3';
                        Rectangle h1 = handles.Find(i => i.Name.Last() == n1);
                        Rectangle h2 = handles.Find(i => i.Name.Last() == n2);
                        left = Canvas.GetLeft(h1);
                        top = Canvas.GetTop(h2);
                        break;
                    }
                case '9': {
                        char n1 = '3';
                        char n2 = '7';
                        Rectangle h1 = handles.Find(i => i.Name.Last() == n1);
                        Rectangle h2 = handles.Find(i => i.Name.Last() == n2);
                        left = Canvas.GetLeft(h1);
                        top = Canvas.GetTop(h2);
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
                        if (Canvas.GetLeft(h) < 0)
                            Canvas.SetLeft(h, 0);
                        if (Canvas.GetTop(h) > canvas.ActualHeight - handle_size)
                            Canvas.SetTop(h, canvas.ActualHeight - handle_size);
                        break;
                    }
                case '3': {
                        Rectangle h2 = handles.Find(i => i.Name.Last() == '7');
                        if (Canvas.GetLeft(h) < Canvas.GetLeft(h2) + handle_size * 3) Canvas.SetLeft(h, Canvas.GetLeft(h2) + handle_size * 3);
                        if (Canvas.GetTop(h) < Canvas.GetTop(h2) + handle_size * 3) Canvas.SetTop(h, Canvas.GetTop(h2) + handle_size * 3);
                        if (Canvas.GetLeft(h) > canvas.ActualWidth - handle_size)
                            Canvas.SetLeft(h, canvas.ActualWidth - handle_size);
                        if (Canvas.GetTop(h) > canvas.ActualHeight - handle_size)
                            Canvas.SetTop(h, canvas.ActualHeight - handle_size);
                        break;
                    }
                case '7': {
                        Rectangle h2 = handles.Find(i => i.Name.Last() == '3');
                        if (Canvas.GetLeft(h) > Canvas.GetLeft(h2) - handle_size * 3) Canvas.SetLeft(h, Canvas.GetLeft(h2) - handle_size * 3);
                        if (Canvas.GetTop(h) > Canvas.GetTop(h2) - handle_size * 3) Canvas.SetTop(h, Canvas.GetTop(h2) - handle_size * 3);
                        if (Canvas.GetLeft(h) < 0)
                            Canvas.SetLeft(h, 0);
                        if (Canvas.GetTop(h) < 0)
                            Canvas.SetTop(h, 0);
                        break;
                    }
                case '9': {
                        Rectangle h2 = handles.Find(i => i.Name.Last() == '1');
                        if (Canvas.GetLeft(h) < Canvas.GetLeft(h2) + handle_size * 3) Canvas.SetLeft(h, Canvas.GetLeft(h2) + handle_size * 3);
                        if (Canvas.GetTop(h) > Canvas.GetTop(h2) - handle_size * 3) Canvas.SetTop(h, Canvas.GetTop(h2) - handle_size * 3);
                        if (Canvas.GetLeft(h) > canvas.ActualWidth - handle_size)
                            Canvas.SetLeft(h, canvas.ActualWidth - handle_size);
                        if (Canvas.GetTop(h) < 0)
                            Canvas.SetTop(h, 0);
                        break;
                    }
                default:
                    break;
            }
        }

        private void AbsFromHandles() {
            abs_rect.x = Canvas.GetLeft(top_left);
            abs_rect.y = Canvas.GetTop(top_left);
            abs_rect.X = Canvas.GetLeft(bottom_right) + handle_size;
            abs_rect.Y = Canvas.GetTop(bottom_right) + handle_size;
        }

        private void AbsFromShape() {
            abs_rect.x = Canvas.GetLeft(shape);
            abs_rect.y = Canvas.GetTop(shape);
            abs_rect.X = Canvas.GetLeft(shape) + shape.ActualWidth;
            abs_rect.Y = Canvas.GetTop(shape) + shape.ActualHeight;
        }

        public void HandlesFromAbs() {
            Canvas.SetLeft(top_left, abs_rect.x);
            Canvas.SetTop(top_left, abs_rect.y);
            Canvas.SetLeft(bottom_right, abs_rect.X - handle_size);
            Canvas.SetTop(bottom_right, abs_rect.Y - handle_size);
            SolveHandleByNum('1');
            SolveHandleByNum('9');
            SolveHandleByNum('2');
            SolveHandleByNum('4');
            SolveHandleByNum('6');
            SolveHandleByNum('8');
        }

        public void ShapeFromHandles() {
            Canvas.SetLeft(shape, Canvas.GetLeft(top_left));
            Canvas.SetTop(shape, Canvas.GetTop(top_left));
            shape.Width = (Canvas.GetLeft(bottom_right) - Canvas.GetLeft(top_left) + handle_size);
            shape.Height = (Canvas.GetTop(bottom_right) - Canvas.GetTop(top_left) + handle_size);
        }

        private static Size FitToRect(double w, double h, double W, double H) {
            double r = w / h;
            double R = W / H;
            if (R > r) return new Size(w * H / h, H); else return new Size(W, h * W / w);
        }

        public void Reset() {
            Canvas.SetLeft(top_left, 0);
            Canvas.SetTop(top_left, 0);
            Canvas.SetLeft(bottom_right, canvas.ActualWidth - handle_size);
            Canvas.SetTop(bottom_right, canvas.ActualHeight - handle_size);
            SolveHandleByNum('1');
            SolveHandleByNum('9');
            SolveHandleByNum('2');
            SolveHandleByNum('4');
            SolveHandleByNum('6');
            SolveHandleByNum('8');
            ShapeFromHandles();
            AbsFromHandles();
            PropFromAbs();
        }

        public void Disable() {
            foreach (Rectangle h in handles) {
                h.IsEnabled = false;
                h.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Transparent);
                h.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Transparent);
            }
            dash.IsEnabled = false; 

        }

        public void Enable() {
            foreach (Rectangle h in handles) {
                h.IsEnabled = true;
                h.Stroke = stroke_enabled;
                h.Fill = fill_enabled;
            }
            dash.IsEnabled = true;



        }

    }
}
