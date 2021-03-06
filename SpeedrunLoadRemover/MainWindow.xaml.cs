﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Diagnostics;
using Emgu.CV;
using Emgu.CV.CvEnum;
using System.Collections.Concurrent;
using YoutubeExplode;

namespace SLR {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
        string video_path = "";
        System.Windows.Threading.DispatcherTimer sliderSyncTimer = new System.Windows.Threading.DispatcherTimer();
        static int log_proc_count = 0;
        bool is_playing = false;
        bool resume_on_up = false;
        bool is_working = false;
        ConcurrentQueue<double> loading_ticks_queue = new ConcurrentQueue<double>();
        ConcurrentQueue<int> loading_frames_queue = new ConcurrentQueue<int>();
        double framerate = 0;
        double video_length_msec = 0;
        int video_frame_count = 0;
        double run_length_msec = 0;
        int run_frame_count = 0;
        double run_start_msec = 0;
        double run_end_msec = 0;
        int run_start_frame = 0;
        int run_end_frame = 0;
        Mat template = new Mat();
        double baseline_error = 0;
        Stopwatch progress_stopwatch = new Stopwatch();
        int progress_old_value = 0;
        Size real_player_size = new Size();
        Size video_natural_dimensions = new Size();
        SelectionRect selection;
        LogicalRect last_selection_prop;
        double reduction_factor = 1;
        System.Threading.CancellationTokenSource count_cancellation_source = new System.Threading.CancellationTokenSource();




        public MainWindow() {
            InitializeComponent();
            sliderSyncTimer.Tick += sliderSyncTimer_Tick;
            sliderSyncTimer.Interval = new TimeSpan(0, 0, 0, 0, 30);
            sliderSyncTimer.Start();
            sldrVideoTime.DataContext = this;
            Console.WriteLine("Number Of Logical Processors: {0}", Environment.ProcessorCount);
            log_proc_count = Environment.ProcessorCount;
            sldrVideoTime.ApplyTemplate();
            System.Windows.Controls.Primitives.Thumb thumb = (sldrVideoTime.Template.FindName("PART_Track", sldrVideoTime) as System.Windows.Controls.Primitives.Track).Thumb;
            thumb.MouseEnter += new MouseEventHandler(thumb_MouseEnter);
            
        }

        private double TestFrame(int Frame_idx = -1) {
            int frame_idx = Frame_idx;
            if (frame_idx == -1)
                frame_idx = (int)(Math.Round(mediaPlayer.Position.TotalMilliseconds * framerate / 1000));
            Mat frame = new Mat();
            VideoCapture cap = new VideoCapture(video_path);
            if (frame_idx >= video_frame_count) {
                frame_idx = video_frame_count - 1;
            }
            cap.SetCaptureProperty(CapProp.PosFrames, frame_idx);
            frame = cap.QueryFrame();
            frame = new Mat(frame, (selection.prop_rect * video_natural_dimensions).ToRectangle());
            CvInvoke.Resize(frame, frame, template.Size);
            cap.Dispose();
            Mat templ = template;
            double error = 0;
            /*
            CvInvoke.NamedWindow("Ltemp");
            CvInvoke.NamedWindow("frame");
            CvInvoke.Imshow("Ltemp", templ);
            CvInvoke.Imshow("frame", frame);
            */
            int result_cols = frame.Cols - template.Cols + 1;
            int result_rows = frame.Rows - template.Rows + 1;
            if (result_cols != 1 || result_rows != 1) { MessageBox.Show("Template is not the same size as frame.", "Error", MessageBoxButton.OK, MessageBoxImage.Error); return int.MaxValue; }

            Mat result = new Mat(1, 1, DepthType.Cv32F, 1);
            CvInvoke.MatchTemplate(frame, template, result, TemplateMatchingType.Sqdiff);
            error = result.GetValueRange().Min;
            var floor = (selection.abs_rect.X - selection.abs_rect.x) * (selection.abs_rect.Y - selection.abs_rect.y) / 100;
            Debug.WriteLine(string.Format("Testing: error {0}", Math.Max(error, floor)));
            
            Debug.WriteLine(string.Format("floor {0}", floor));

            return Math.Max(error, floor);
        }

        private void PreviewTemplate() {
            //var time = mediaPlayer.Position.TotalMilliseconds;// - 1000 / framerate;
            int frame_idx = (int)(Math.Round(mediaPlayer.Position.TotalMilliseconds * framerate / 1000));
            Debug.WriteLine(frame_idx);
            VideoCapture cap = new VideoCapture(video_path);
            if (frame_idx == cap.GetCaptureProperty(CapProp.FrameCount))
                frame_idx-=3;
            //cap.SetCaptureProperty(CapProp.PosMsec, time);
            cap.SetCaptureProperty(CapProp.PosFrames, frame_idx);


            var large_template = new Mat(cap.QueryFrame(), (selection.prop_rect * video_natural_dimensions).ToRectangle());
            CvInvoke.Resize(large_template, template, new System.Drawing.Size((int)(large_template.Cols * reduction_factor), (int)(large_template.Rows * reduction_factor)));

            baseline_error = double.PositiveInfinity;
            int step = 3;
            for (int f = frame_idx - 1* step; f < frame_idx + 2* step; f += 2* step) {
                cap.SetCaptureProperty(CapProp.PosFrames, f);
                double e = TestFrame(f);
                Debug.WriteLine("f " + f + " e: " + e);
                if (e < baseline_error) baseline_error = e;
            }
            cap.Dispose();
            baseline_error *= 1000;
            Debug.WriteLine("base " + baseline_error);

            System.Drawing.Bitmap bm = large_template.Bitmap;
            System.Windows.Media.Imaging.BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
            bm.GetHbitmap(),
            IntPtr.Zero,
            System.Windows.Int32Rect.Empty,
            System.Windows.Media.Imaging.BitmapSizeOptions.FromWidthAndHeight(bm.Width, bm.Height));
            ImageBrush ib = new ImageBrush(bs);
            templ_preview.Source = ib.ImageSource;



        }

        private void InitVideo() {
            mediaPlayer.Source = new Uri(video_path);
            sldrVideoTime.IsEnabled = true;
            btnPlay.IsEnabled = true;
            btnMoveBack.IsEnabled = true;
            btnMoveForward.IsEnabled = true;
            btnSnap.IsEnabled = true;
            btnMarkStart.IsEnabled = true;
            btnMarkEnd.IsEnabled = true;

            VideoCapture cap = new VideoCapture(video_path);
            mediaPlayer.Play();
            mediaPlayer.Stop();
            framerate = cap.GetCaptureProperty(CapProp.Fps);

            video_frame_count = (int)cap.GetCaptureProperty(CapProp.FrameCount);
            video_length_msec = (int)(video_frame_count * 1000 / framerate);
            run_start_msec = 0;
            run_start_frame = 0;
            run_end_msec = video_length_msec;
            run_end_frame = (int)cap.GetCaptureProperty(CapProp.FrameCount);
            run_length_msec = run_end_msec;
            run_frame_count = run_end_frame;
            var width = cap.GetCaptureProperty(CapProp.FrameWidth);
            var height = cap.GetCaptureProperty(CapProp.FrameHeight);
            video_natural_dimensions = new Size(width, height);
            var duration = string.Format("{0:h\\:mm\\:ss\\.fff}", new TimeSpan(0, 0, 0, 0, (int)(video_frame_count * 1000 / framerate)));
            if (width + height + framerate != 0) {
                lvList.Items.Insert(0, string.Format("Video loaded: Total runtime {0}, {1}x{2}@{3}", duration, width, height, (int)Math.Round(framerate)));
                progress_bar.Maximum = video_frame_count;
                sldrVideoTime.Minimum = 1;
                sldrVideoTime.Maximum = video_length_msec;
                sldrVideoTime.Value = sldrVideoTime.Minimum;
                sldrVideoTime.SelectionStart = run_start_msec;
                sldrVideoTime.SelectionEnd = run_end_msec;
            }
        }

        private void UpdateRunLengths() {
            run_length_msec = video_length_msec - (video_length_msec - run_end_msec) - run_start_msec;
            run_frame_count = video_frame_count - (video_frame_count - run_end_frame) - run_start_frame;
            lvList.Items.Insert(0, string.Format("Run length: {0}", string.Format("{0:h\\:mm\\:ss\\.fff}", new TimeSpan(0, 0, 0, 0, (int)Math.Round(run_length_msec)))));
            progress_bar.Maximum = run_frame_count;
        }

        private int CountLoadsToQueue(int core_idx, int core_count, System.Threading.CancellationToken token) {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            VideoCapture cap = new VideoCapture(video_path);
            Mat templ = template;//.Clone();
            double error;
            Mat frame;
            int loading_frames = 0;

            int chunk_length = run_frame_count / core_count;
            int start_frame = run_start_frame + chunk_length * core_idx;
            if (core_idx == core_count - 1) chunk_length = run_end_frame - start_frame;
            Debug.WriteLine("core {0} of {1}: start frame {2}, length {3}.", core_idx, core_count, start_frame, chunk_length);
            cap.SetCaptureProperty(CapProp.PosFrames, start_frame);
            for (int i = start_frame; i < start_frame + chunk_length; i++) {
                if (!token.IsCancellationRequested) {

                    progress_bar.Dispatcher.Invoke((Action)(() => progress_bar.Value += 1));
                    frame = cap.QuerySmallFrame();
                    if (frame == null) { Debug.WriteLine("cant even i: " + i); continue; }
                    frame = new Mat(frame, (selection.prop_rect * video_natural_dimensions * new Size(0.5, 0.5)).ToRectangle());
                    CvInvoke.Resize(frame, frame, new System.Drawing.Size(templ.Cols, templ.Rows));

                    int result_cols = frame.Cols - templ.Cols + 1;
                    int result_rows = frame.Rows - templ.Rows + 1;
                    if (result_cols != 1 || result_rows != 1) { MessageBox.Show("Template is not the same size as frame.", "Error", MessageBoxButton.OK, MessageBoxImage.Error); return 0; }
                    Mat result = new Mat(1, 1, DepthType.Cv32F, 1);

                    CvInvoke.MatchTemplate(frame, templ, result, TemplateMatchingType.Sqdiff);
                    error = result.GetValueRange().Min;

                    if (error < baseline_error) { // is a match
                        loading_frames++;
                        loading_frames_queue.Enqueue(i + 1);
                    }

                }
                else {
                    progress_bar.Dispatcher.Invoke((Action)(() => progress_bar.Value =0));
                    lbl_eta.Dispatcher.Invoke((Action)(() => lbl_eta.Content= ""));
                    progress_old_value = 0;
                    stopWatch.Stop();
                    token.ThrowIfCancellationRequested();
                    return 0;
                }
            }
            stopWatch.Stop();
            return loading_frames;
        }

        private void IsPlaying(bool flag) {
            is_playing = flag;
        }

        private static string MakeValidFileName(string name) {
            string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);
            return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
        }

        public double QuantizeTime(double ms) {
            return Math.Round((ms / (double)(1000 / framerate)),
             MidpointRounding.AwayFromZero) * (1000 / framerate) + 1;
        }

        private Size FitToRect(double w, double h, double W, double H) {
            double r = w / h;
            double R = W / H;
            if (R > r) return new Size(w * H / h, H); else return new Size(W, h * W / w);
        }
        private void btnPlay_Click(object sender, RoutedEventArgs e) {
            if (is_playing) {
                btnPlay.Content = "Play";
                IsPlaying(false);
                mediaPlayer.Pause();
                mediaPlayer.Position = new System.TimeSpan(0, 0, 0, 0, (int)QuantizeTime(mediaPlayer.Position.TotalMilliseconds));
            }
            else {
                btnPlay.Content = "Pause";
                IsPlaying(true);
                mediaPlayer.Play();
            }
        }

        private void btnMoveBack_Click(object sender, RoutedEventArgs e) {
            double step = 1000 / framerate;
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) step = 1000;
            mediaPlayer.Pause();
            IsPlaying(false);
            btnPlay.Content = "Play";
            //mediaPlayer.Position -= TimeSpan.FromMilliseconds(1000 / framerate);
            mediaPlayer.Position = new System.TimeSpan(0, 0, 0, 0, (int)QuantizeTime(mediaPlayer.Position.TotalMilliseconds));

            mediaPlayer.Play();
            mediaPlayer.Pause();

            sldrVideoTime.Value -= step;
        }

        private void btnMoveForward_Click(object sender, RoutedEventArgs e) {
            double step = 1000 / framerate;
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) step = 1000;
            mediaPlayer.Pause();
            IsPlaying(false);
            btnPlay.Content = "Play";
            //mediaPlayer.Position += TimeSpan.FromMilliseconds(step);
            mediaPlayer.Position = new System.TimeSpan(0, 0, 0, 0, (int)QuantizeTime(mediaPlayer.Position.TotalMilliseconds));

            mediaPlayer.Play();
            mediaPlayer.Pause();
            sldrVideoTime.Value += step;
        }



        private async void btnOpen_Click(object sender, RoutedEventArgs e) {
            // Configure open file dialog box 
            IsPlaying(false);
            btnPlay.Content = "Play";
            mediaPlayer.Stop();
            loading_frames_queue = new ConcurrentQueue<int>();
            dialog.Filter = "Movie Files|*.mp4;*.mpg;*.avi;*.mov;*.wmv;*.mkv";
            dialog.FilterIndex = 1;
            dialog.Title = "Select video file or paste YouTube video ID";
            dialog.FileName = "File or YT ID"; // Default file name 
            //dialog.FileName = "C_VheAwZBuQ"; // Default file name 
            dialog.CheckFileExists = false;
            // Show open file dialog box 
            btnOpen.IsEnabled = false;
            Nullable<bool> result = dialog.ShowDialog();

            // Process open file dialog box results  
            if (result == true) {
                Debug.WriteLine(dialog.FileName);
                video_path = dialog.FileName;

                if (!System.IO.File.Exists(dialog.FileName)) { //no file exists, try youtube
                    var video_id = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
                    var client = new YoutubeClient();
                    var streamInfoSet = await client.GetVideoMediaStreamInfosAsync(video_id);
                    var stream_list = streamInfoSet.Muxed;
                    var stream_size = stream_list.OrderByDescending(s => s.VideoQuality).FirstOrDefault().Size;
                    Debug.WriteLine(stream_size);
                    var url = "https://youtu.be/" + video_id;
                    var t = Task.Factory.StartNew(new Action(() => {
                        try {
                            using (var service = VideoLibrary.Client.For(VideoLibrary.YouTube.Default)) {
                                VideoLibrary.YouTubeVideo video = service.GetVideo(url);
                                    video_path = System.IO.Path.GetDirectoryName(dialog.FileName) + "\\" + MakeValidFileName(video.FullName);
                                    lvList.Dispatcher.Invoke((Action)(() => lvList.Items.Insert(0, "Downloading " + video.Title+ "...")));
                                    lvList.Dispatcher.Invoke((Action)(() => lvList.Items.Insert(0, "Size: " + (int)(stream_size/1024/1024) + " MB")));

                                using (var outFile = System.IO.File.OpenWrite(video_path)) {
                                        using (var ps = new CGS.ProgressStream(outFile)) {
                                        

                                            if (stream_size > 0)
                                                progress_bar.Dispatcher.Invoke((Action)(() => progress_bar.Maximum = stream_size));

                                            ps.BytesMoved += (sender_, args) => {
                                                progress_bar.Dispatcher.Invoke((Action)(() => progress_bar.Value = args.StreamPosition));
                                            };
                                            video.Stream().CopyTo(ps);
                                        }
                                    }
                                    Debug.WriteLine(video.FullName);
                                    Debug.WriteLine(MakeValidFileName(video.FullName));
                                
                            }
                        }
                    catch (System.Net.Http.HttpRequestException) {
                            lvList.Dispatcher.Invoke((Action)(() => lvList.Items.Insert(0, "Unable to download video. Please try to download it manually and open from disk.")));
                            video_path = "none";

                        }
                        catch (System.InvalidOperationException) {
                            lvList.Dispatcher.Invoke((Action)(() => lvList.Items.Insert(0, "Bad filename or YouTube ID.")));
                            video_path = "none";
                        }
                    finally {
                            //video_path = dialog.FileName;
                            btnOpen.Dispatcher.Invoke((Action)(() => btnOpen.IsEnabled = true));
                        }
                    }));
                    await Task.WhenAll(t);
                }
                if (System.IO.File.Exists(video_path)) { //file supposedly exists
                    if (System.IO.Path.GetExtension(video_path) == ".webm") {
                        lvList.Dispatcher.Invoke((Action)(() => lvList.Items.Insert(0, "Sorry, .webm file not supported by player.")));
                        return;
                    }
                    //video_path = dialog.FileName;
                    if (new System.IO.FileInfo(video_path).Length > 0)  { //file or youtube really exists
                        btnOpen.IsEnabled = true;

                        InitVideo();
                    }
                }
            }
            else { //cancelled
                btnOpen.IsEnabled = true;
            }
        }

        private void btnSnap_Click(object sender, RoutedEventArgs e) {
            if (is_playing) mediaPlayer.Pause();
            last_selection_prop = selection.prop_rect;
            reduction_factor = Math.Sqrt(10000 / (((selection.prop_rect.X - selection.prop_rect.x)*video_natural_dimensions.Width) * ((selection.prop_rect.Y - selection.prop_rect.y)*video_natural_dimensions.Height)));
            reduction_factor = Math.Min(reduction_factor, 1);
            var xx = (int)((selection.prop_rect.X - selection.prop_rect.x) * video_natural_dimensions.Width);
            var yy = (int)((selection.prop_rect.Y - selection.prop_rect.y) * video_natural_dimensions.Height);
            //lvList.Items.Insert(0, "Selection natural size: " + xx + "x" + yy);
            //lvList.Items.Insert(0, "Selection reduced size: " + (int)(xx*reduction_factor) + "x" + (int)(yy* reduction_factor) + " = " + xx*yy*reduction_factor*reduction_factor);


            PreviewTemplate();
            btnCount.IsEnabled = true;
            btnTest.IsEnabled = true;
            if (is_playing) mediaPlayer.Play();

        }

        private void sliderSyncTimer_Tick(object sender, EventArgs e) {
            if (is_playing) sldrVideoTime.Value = mediaPlayer.Position.TotalMilliseconds;
            int frame_idx = (int)(Math.Round(mediaPlayer.Position.TotalMilliseconds * framerate / 1000));
            if (loading_frames_queue.Contains(frame_idx+1)) {
                led_matched.Fill = Brushes.Green;
            }
            else {
                led_matched.Fill = Brushes.Transparent;

            }
            txt_time.Text = string.Format("{0:h\\:mm\\:ss\\.fff}", mediaPlayer.Position) + " " + frame_idx;
        }

        private void sldrVideoTime_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            sldrVideoTime.Value = QuantizeTime(sldrVideoTime.Value);
            if (resume_on_up) {
                IsPlaying(true);
                mediaPlayer.Play();
            }
        }

        private void sldrVideoTime_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            mediaPlayer.Pause();
            IsPlaying(false);
        }

        private void thumb_MouseEnter(object sender, MouseEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed && e.MouseDevice.Captured == null) {
                // the left button is pressed on mouse enter
                // but the mouse isn't captured, so the thumb
                // must have been moved under the mouse in response
                // to a click on the track.
                // Generate a MouseLeftButtonDown event.
                MouseButtonEventArgs args = new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.Left);
                args.RoutedEvent = MouseLeftButtonDownEvent;
                (sender as System.Windows.Controls.Primitives.Thumb).RaiseEvent(args);
                if (!is_playing)
                    resume_on_up = false;
                else
                    resume_on_up = true;
                IsPlaying(false);
                mediaPlayer.Pause();
                mediaPlayer.Position = new System.TimeSpan(0, 0, 0, 0, (int)sldrVideoTime.Value);
            }
        }

        private async void btnCount_Click(object sender, RoutedEventArgs e) {
            if (btnCount.Content.ToString() == "Cancel") { btnCount.Content = "Count"; count_cancellation_source.Cancel(); }
            else {
                btnCount.Content = "Cancel";
                Stopwatch master_stopwatch = new Stopwatch();
                master_stopwatch.Start();
                is_working = true;
                List<Task> task_list = new List<Task>();
                ConcurrentBag<int> frame_count_bag = new ConcurrentBag<int>();
                loading_frames_queue = new ConcurrentQueue<int>();

                sldrVideoTime.Ticks = new DoubleCollection(new double[] { 10 });
                DoubleCollection times = new DoubleCollection();
                btnMarkStart.IsEnabled = false;
                btnMarkEnd.IsEnabled = false;
                btnOpen.IsEnabled = false;
                btnSnap.IsEnabled = false;
                //btnCount.IsEnabled = false;
                selection.Disable();
                selection.prop_rect = last_selection_prop;
                selection.AbsFromProp();
                selection.HandlesFromAbs();
                selection.ShapeFromHandles();
                

                progress_stopwatch.Restart();
                for (int core = 0; core < log_proc_count; core++) {
                    object core_ = core;
                    var t = Task.Factory.StartNew(new Action<object>((o) => {
                        var frame_count = CountLoadsToQueue((int)o, log_proc_count, count_cancellation_source.Token);
                        frame_count_bag.Add(frame_count);
                    }), core_);
                    task_list.Add(t);
                }
                try {
                    await Task.WhenAll(task_list.ToArray());
                foreach (double elm in loading_ticks_queue) {
                    times.Add(elm);
                }
                int loading_frame_count = 0;
                foreach (int elm in frame_count_bag) {
                    loading_frame_count += elm;
                }
                sldrVideoTime.Ticks = times;
                foreach (Task t in task_list) {
                    t.Dispose();
                }

                master_stopwatch.Stop();

                
                lvList.Items.Insert(0, string.Format("Work over. Elapsed time: {0}", string.Format("{0:m\\:ss}", new TimeSpan(0, 0, 0, 0, (int)master_stopwatch.ElapsedMilliseconds))));
                lvList.Items.Insert(0, "Video processed (frames): " + run_frame_count);
                lvList.Items.Insert(0, "Processing ratio: " + (int)(run_length_msec / master_stopwatch.ElapsedMilliseconds));
                lvList.Items.Insert(0, "Loading frame count: " + loading_frame_count);
                lvList.Items.Insert(0, "Loading time: " + string.Format("{0:m\\:ss\\.fff}", new TimeSpan(0, 0, 0, 0, (int)(loading_frame_count * 1000 / framerate))));
                lvList.Items.Insert(0, "RTA time: " + string.Format("{0:h\\:mm\\:ss\\.fff}", new TimeSpan(0, 0, 0, 0, (int)run_length_msec)));
                lvList.Items.Insert(0, "Loadless time: " + string.Format("{0:h\\:mm\\:ss\\.fff}", new TimeSpan(0, 0, 0, 0, (int)((run_frame_count - loading_frame_count) * 1000 / framerate))));
                }
                catch (OperationCanceledException) {
                    count_cancellation_source = new System.Threading.CancellationTokenSource();
                    master_stopwatch.Stop();

                }
                finally {
                    GC.Collect();
                    is_working = false;

                    btnMarkStart.IsEnabled = true;
                    btnMarkEnd.IsEnabled = true;
                    btnOpen.IsEnabled = true;
                    btnSnap.IsEnabled = true;
                    btnCount.Content = "Count";
                    btnTest.IsEnabled = false;
                    selection.Enable();
                }
            }
        }

        private void btnMarkStart_Click(object sender, RoutedEventArgs e) {
            run_start_msec = mediaPlayer.Position.TotalMilliseconds;
            run_start_frame = (int)(run_start_msec * framerate / 1000);
            var time_start_span = new TimeSpan(0, 0, 0, 0, (int)run_start_msec);
            sldrVideoTime.SelectionStart = run_start_msec;
            //lvList.Items.Insert(0, "start time: " + string.Format("{0:h\\:mm\\:ss\\.fff}", time_start_span) + " ,frame: " + run_start_frame);
            btnMarkEnd.IsEnabled = false;
            UpdateRunLengths();
        }

        private void btnMarkEnd_Click(object sender, RoutedEventArgs e) {
            run_end_msec = mediaPlayer.Position.TotalMilliseconds;
            run_end_frame = (int)(run_end_msec * framerate / 1000);
            var time_end_span = new TimeSpan(0, 0, 0, 0, (int)run_end_msec);
            sldrVideoTime.SelectionEnd = run_end_msec;
            //lvList.Items.Insert(0, "end time: " + string.Format("{0:h\\:mm\\:ss\\.fff}", time_end_span) + " ,frame: " + run_end_frame);
            btnMarkStart.IsEnabled = false;
            UpdateRunLengths();
        }

        private void sldrVideoTime_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (!is_playing)
                mediaPlayer.Position = new System.TimeSpan(0, 0, 0, 0, (int)(QuantizeTime(sldrVideoTime.Value)));
            if (!is_working) {
                if (mediaPlayer.Position.TotalMilliseconds >= run_end_msec) btnMarkStart.IsEnabled = false; else btnMarkStart.IsEnabled = true;
                if (mediaPlayer.Position.TotalMilliseconds <= run_start_msec) btnMarkEnd.IsEnabled = false; else btnMarkEnd.IsEnabled = true;
            }
        }

        private void btnAbout_Click(object sender, RoutedEventArgs e) {
            var frm = new AboutWindow();
            frm.Owner = this;
            frm.ShowDialog();
        }

        private void progress_bar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            //var value = (int)(progress_bar.Value / 1000);
            int percent = (int)(progress_bar.Value * 100 / progress_bar.Maximum);
            if (percent > progress_old_value) {
                var time_msec = progress_stopwatch.ElapsedMilliseconds;
                //double rate = 1 / (double)time_msec;
                double eta_msec = (1 - (progress_bar.Value / progress_bar.Maximum)) *100 * time_msec;
                string eta = "Time left: " + string.Format("{0:h\\:mm\\:ss}", new TimeSpan(0, 0, 0, 0, (int)eta_msec));
                lbl_eta.Content = eta;
                progress_stopwatch.Restart();
                progress_old_value = percent;
            }
            if (progress_bar.Value >= progress_bar.Maximum) {
                progress_bar.Value = 0;
                progress_old_value = 0;
                lbl_eta.Content = "";
            }
            this.taskBarItemInfo1.ProgressValue = progress_bar.Value / progress_bar.Maximum;
        }

        private void mediaPlayer_MediaEnded(object sender, RoutedEventArgs e) {
            if (is_playing) {
                sldrVideoTime.Value = sldrVideoTime.Minimum;
                mediaPlayer.Stop();
                IsPlaying(false);
                btnPlay.Content = "Play";
            }
        }

        private void btnTest_Click(object sender, RoutedEventArgs e) {
            TestFrame();
        }

        private void mediaPlayer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            UpdateVideoBounds();
        }

        private void UpdateVideoBounds() {
            double ww = grid.ColumnDefinitions[0].ActualWidth + grid.ColumnDefinitions[1].ActualWidth + grid.ColumnDefinitions[2].ActualWidth;
            real_player_size = FitToRect(mediaPlayer.ActualWidth, mediaPlayer.ActualHeight, ww, grid.RowDefinitions[0].ActualHeight);
            canvas.Width = real_player_size.Width;
            canvas.Height = real_player_size.Height;
            canvas.UpdateLayout();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) {
        }
        private void canvas_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
        }
        private void mediaPlayer_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
        }
        private void mediaPlayer_PreviewMouseMove(object sender, MouseEventArgs e) {
        }

        private void mediaPlayer_MediaOpened(object sender, RoutedEventArgs e) {
            mediaPlayer.UpdateLayout();
            UpdateVideoBounds();
            if (selection == null) selection = new SelectionRect(canvas);
            selection.Reset();
            templ_preview.Source = null;
            btnCount.IsEnabled = false;
            btnTest.IsEnabled = false;
        }

        private void mediaPlayer_SizeChanged(object sender, SizeChangedEventArgs e) {
            UpdateVideoBounds();
            if (selection != null) {
                selection.AbsFromProp();
                selection.HandlesFromAbs();
                selection.ShapeFromHandles();
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) {
                btnMoveBack.Content = "<<";
                btnMoveForward.Content = ">>";
            }

        }

        private void Window_PreviewKeyUp(object sender, KeyEventArgs e) {
            if (Keyboard.IsKeyUp(Key.LeftShift) || Keyboard.IsKeyUp(Key.RightShift)) {
                btnMoveBack.Content = "<";
                btnMoveForward.Content = ">";
            }
        }
    }
}