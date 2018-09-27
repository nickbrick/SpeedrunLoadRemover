using System;
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

namespace WpfApp1 {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
        string video_path = "";
        System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
        static int log_proc_count = 0;
        bool is_playing = false;
        bool resume_on_up = false;
        bool is_working = false;
        ConcurrentQueue<double> loading_ticks_queue = new ConcurrentQueue<double>();
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
        Stopwatch progress_timer = new Stopwatch();
        int old_value = 0;


        public MainWindow() {
            InitializeComponent();


            //  DispatcherTimer setup
            dispatcherTimer.Tick += dispatcherTimer_Tick;

            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 30);
            dispatcherTimer.Start();
            sldrVideoTime.DataContext = this;
            Console.WriteLine("Number Of Logical Processors: {0}", Environment.ProcessorCount);
            log_proc_count = Environment.ProcessorCount - 1;
            sldrVideoTime.ApplyTemplate();
            System.Windows.Controls.Primitives.Thumb thumb = (sldrVideoTime.Template.FindName("PART_Track", sldrVideoTime) as System.Windows.Controls.Primitives.Track).Thumb;
            thumb.MouseEnter += new MouseEventHandler(thumb_MouseEnter);
            //GrabTemplate();
        }



        private void GrabTemplate() {
            VideoCapture cap = new VideoCapture(video_path);
            Mat templ = CvInvoke.Imread(@"C:\Users\Nick\Documents\sources\WpfApp1\videos\bscap0011.jpg");
            Mat edges = new Mat(templ.Rows, templ.Cols, DepthType.Default, 1);
            CvInvoke.Canny(templ, edges, 100, 200);

            CvInvoke.NamedWindow("image", NamedWindowType.AutoSize);
            CvInvoke.Imshow("image", edges);

            Mat result = new Mat();
            System.Drawing.Size size = edges.Size;
            /*
            for (int i = 0; i<10; i++) {
                //img = cap.QueryFrame();
                size = new System.Drawing.Size((int)(size.Width * 0.8), (int)(size.Height * 0.8));
                CvInvoke.Resize(edges, edges, size);
                CvInvoke.Imshow("image", edges);
                Thread.Sleep(1000);

            }
            */
        }

        private void ScaleInvariantMatchThisFrame() {
            var time = MediaPlayer.Position.TotalMilliseconds - 1000 / framerate;
            Mat img = new Mat(); Mat img_ = new Mat();
            VideoCapture cap = new VideoCapture(video_path);
            cap.SetCaptureProperty(CapProp.PosMsec, time);
            img_ = cap.QueryFrame();
            //templ_preview.Source = 
            CvInvoke.CvtColor(img_, img_, ColorConversion.Rgb2Gray);
            CvInvoke.MedianBlur(img_, img_, 3);

            //CvInvoke.AdaptiveThreshold(img_, img, 255,AdaptiveThresholdType.GaussianC, ThresholdType.Binary, 11,2);
            CvInvoke.MedianBlur(img_, img, 3);

            CvInvoke.NamedWindow("frame", NamedWindowType.AutoSize);
            CvInvoke.Imshow("frame", img);

            Mat templ = CvInvoke.Imread(@"C:\Users\Nick\Documents\sources\WpfApp1\videos\cleanload.png");
            List<System.Drawing.Size> sizes = new List<System.Drawing.Size>();
            var initial_size = new System.Drawing.Size(templ.Cols, templ.Rows);
            var final_size = new System.Drawing.Size(img.Cols, img.Rows);
            int num_steps = 10;
            int step = (img.Rows - templ.Rows) / num_steps;
            List<Mat> templs = new List<Mat>();
            for (int i = 0; i < num_steps; i++) {
                //templs.Add(new Mat());
            }
            templs.Add(templ);
            for (int i = 0; i < num_steps; i++) {
                templs.Add(new Mat());

                int rows = templ.Rows + step * (i + 1);
                int cols = rows * templ.Cols / templ.Rows;

                if (cols > img.Cols) {
                    cols = img.Cols;
                    rows = cols * templ.Rows / templ.Cols;
                    CvInvoke.Resize(templ, templs.ElementAt(i + 1), new System.Drawing.Size(cols, rows));
                    //templs.Remove(templs.Last());
                    break;
                }
                //Mat retempl = (Mat)templ.Clone();
                //templs.Add(retempl);
                CvInvoke.Resize(templ, templs.ElementAt(i + 1), new System.Drawing.Size(cols, rows));


            }
            List<Mat> edgeses = new List<Mat>();
            foreach (Mat t in templs) {
                Mat edges = new Mat(t.Rows, t.Cols, DepthType.Default, 1);
                //CvInvoke.Threshold(t, edges, 0.8, 1, ThresholdType.Binary);
                CvInvoke.CvtColor(t, t, ColorConversion.Rgb2Gray);
                //CvInvoke.MedianBlur(t, t, 3);

                //CvInvoke.AdaptiveThreshold(t, t, 255, AdaptiveThresholdType.GaussianC, ThresholdType.Binary, 11, 2);
                CvInvoke.MedianBlur(t, edges, 3);

                //CvInvoke.Canny(t, edges, 100, 200);
                edgeses.Add(edges);
            }

            foreach (Mat t in edgeses) {
                int i = edgeses.IndexOf(t);


                CvInvoke.NamedWindow("templ" + i.ToString(), NamedWindowType.AutoSize);
                CvInvoke.Imshow("templ" + i.ToString(), t);

                Mat frame = new Mat();
                Mat mask = new Mat();
                double minVal = 0; double maxVal = 0;
                System.Drawing.Point minLoc = new System.Drawing.Point(); System.Drawing.Point maxLoc = new System.Drawing.Point();
                Mat asd = new Mat();


                int result_cols = img.Cols - t.Cols + 1;
                int result_rows = img.Rows - t.Rows + 1;
                Mat result = new Mat();
                result.Create(result_cols, result_rows, DepthType.Cv32F, 3);
                CvInvoke.MatchTemplate(img, t, result, TemplateMatchingType.Sqdiff);
                System.Drawing.Point matchLoc;
                CvInvoke.MinMaxLoc(result, ref minVal, ref maxVal, ref minLoc, ref maxLoc, asd);
                CvInvoke.Normalize(result, result, 0, 1, NormType.MinMax, DepthType.Cv32F, mask);
                matchLoc = minLoc;
                Debug.WriteLine(string.Format("size {0}:{2}x{3}, error {1}", i, minVal / 10000000, t.Cols, t.Rows));
            }
        }

        private void PreviewTemplate() {
            var time = MediaPlayer.Position.TotalMilliseconds - 1000 / framerate;
            VideoCapture cap = new VideoCapture(video_path);
            cap.SetCaptureProperty(CapProp.PosMsec, (int)time);
            template = cap.QueryFrame();
            cap.Dispose();
            System.Drawing.Bitmap bm = template.Bitmap;
            System.Windows.Media.Imaging.BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
            bm.GetHbitmap(),
            IntPtr.Zero,
            System.Windows.Int32Rect.Empty,
            System.Windows.Media.Imaging.BitmapSizeOptions.FromWidthAndHeight(bm.Width, bm.Height));
            ImageBrush ib = new ImageBrush(bs);
            templ_preview.Source = ib.ImageSource;
        }

        private void InitVideo() {
            MediaPlayer.Source = new Uri(video_path);
            sldrVideoTime.IsEnabled = true;
            btnPlay.IsEnabled = true;
            btnMoveBack.IsEnabled = true;
            btnMoveForward.IsEnabled = true;
            btnSnap.IsEnabled = true;
            btnMarkStart.IsEnabled = true;
            btnMarkEnd.IsEnabled = true;

            VideoCapture cap = new VideoCapture(video_path);
            MediaPlayer.Play();
            MediaPlayer.Stop();
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
            var duration = string.Format("{0:h\\:mm\\:ss\\.fff}", new TimeSpan(0, 0, 0, 0, (int)(video_frame_count * 1000 / framerate)));
            if (width + height + framerate != 0) {
                lvList.Items.Insert(0, string.Format("Video loaded: Total runtime {0}, {1}x{2}@{3}", duration, width, height, (int)Math.Round(framerate)));
                progress_bar.Maximum = video_frame_count;
                sldrVideoTime.Maximum = video_length_msec;
                sldrVideoTime.Value = 0;
                sldrVideoTime.Ticks = new DoubleCollection();
                sldrVideoTime.SelectionStart = run_start_msec;
                sldrVideoTime.SelectionEnd = run_end_msec;

            }
        }

        private void UpdateRunLengths() {
            run_length_msec = video_length_msec - (video_length_msec - run_end_msec) - run_start_msec;
            run_frame_count = video_frame_count - (video_frame_count - run_end_frame) - run_start_frame;
            lvList.Items.Insert(0, string.Format("Run length: {0}", string.Format("{0:h\\:mm\\:ss\\.fff}", new TimeSpan(0, 0, 0, 0, (int)Math.Round(run_length_msec)))));
            //lvList.Items.Insert(0, "run_frame_count: " + run_frame_count);
            progress_bar.Maximum = run_frame_count;

        }

        private int CountLoadsToQueue(int core_idx, int core_count) {

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();


            VideoCapture cap = new VideoCapture(video_path);
            Mat frame = new Mat();
            //Mat templ = CvInvoke.Imread(@"C:\Users\Nick\Documents\sources\WpfApp1\videos\jsrf.png");
            Mat templ = template.Clone();
            CvInvoke.Resize(templ, templ, new System.Drawing.Size(templ.Cols / 8, templ.Rows / 8));
            //CvInvoke.NamedWindow("image", NamedWindowType.AutoSize);
            //CvInvoke.NamedWindow("result", NamedWindowType.AutoSize);
            Mat mask = new Mat();
            double minVal = 0; double maxVal = 0;
            System.Drawing.Point minLoc = new System.Drawing.Point(); System.Drawing.Point maxLoc = new System.Drawing.Point();
            Mat asd = new Mat();
            Mat img; Mat result = new Mat();
            Mat img_display = new Mat();
            int loading_frames = 0;
            bool is_last_frame_loading = false;




            var frame_size = new System.Drawing.Size((int)cap.GetCaptureProperty(CapProp.FrameWidth) / 8, (int)cap.GetCaptureProperty(CapProp.FrameHeight) / 8);

            int chunk_length = run_frame_count / core_count;
            int start_frame = run_start_frame + chunk_length * core_idx;
            if (core_idx == core_count - 1) chunk_length = run_end_frame - start_frame;
            Debug.WriteLine("core {0} of {1}: start frame {2}, length {3}.", core_idx, core_count, start_frame, chunk_length);
            cap.SetCaptureProperty(CapProp.PosFrames, start_frame);
            int last_load_start = 0;
            for (int i = start_frame; i < start_frame + chunk_length /*cap.GetCaptureProperty(CapProp.FrameCount)*/; i++) {
                img = cap.QueryFrame();
                CvInvoke.Resize(img, img, frame_size);

                /// Create the result matrix
                int result_cols = img.Cols - templ.Cols + 1;
                int result_rows = img.Rows - templ.Rows + 1;

                result.Create(result_cols, result_rows, DepthType.Cv32F, 3);

                /// Do the Matching and Normalize
                CvInvoke.MatchTemplate(img, templ, result, TemplateMatchingType.Sqdiff);

                /// Localizing the best match with minMaxLoc
                System.Drawing.Point matchLoc;
                CvInvoke.MinMaxLoc(result, ref minVal, ref maxVal, ref minLoc, ref maxLoc, asd);
                CvInvoke.Normalize(result, result, 0, 1, NormType.MinMax, DepthType.Cv32F, mask);

                /// For SQDIFF and SQDIFF_NORMED, the best matches are lower values. For all the other methods, the higher the better
                //if (match_method == CV_TM_SQDIFF || match_method == CV_TM_SQDIFF_NORMED) { matchLoc = minLoc; }
                //else { matchLoc = maxLoc; }
                matchLoc = minLoc;

                if (minVal < Math.Pow(10, 7) * 5) { // is a match
                    loading_frames++;
                    if (!is_last_frame_loading) {
                        //loading_windows.Add(Math.Floor(cap.GetCaptureProperty((int)CapProp.PosMsec) / 1000));
                        var tick = Math.Floor(cap.GetCaptureProperty((int)CapProp.PosMsec) / 1000);
                        loading_ticks_queue.Enqueue(tick);
                        is_last_frame_loading = true;
                        last_load_start = (int)tick;
                    }
                }
                else {
                    if (is_last_frame_loading) {
                        //loading_windows.Add(Math.Floor(cap.GetCaptureProperty((int)CapProp.PosMsec) / 1000));
                        for (var sec = last_load_start + 1; sec < Math.Floor(cap.GetCaptureProperty((int)CapProp.PosMsec) / 1000); sec++) { loading_ticks_queue.Enqueue(sec); }
                        is_last_frame_loading = false;
                    }
                }
                progress_bar.Dispatcher.Invoke((Action)(() => progress_bar.Value += 1));

            }


            stopWatch.Stop();
            /*
            Debug.WriteLine("elapsed time (s): " + stopWatch.Elapsed.TotalSeconds);
            Debug.WriteLine("video processed (s): " + cap.GetCaptureProperty((int)CapProp.PosMsec) / 1000);
            Debug.WriteLine("video processed (frames): " + cap.GetCaptureProperty(CapProp.PosFrames));
            Debug.WriteLine("processing ratio: " + cap.GetCaptureProperty((int)CapProp.PosMsec) / stopWatch.Elapsed.TotalMilliseconds);
            Debug.WriteLine("loading frame count: " + loading_frames);
            Debug.WriteLine("loading time (s): " + loading_frames / framerate);
            */



            return loading_frames;
        }


        private void IsPlaying(bool flag) {
            is_playing = flag;
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e) {
            if (is_playing) {
                btnPlay.Content = "Play";
                IsPlaying(false);
                MediaPlayer.Pause();
            }
            else {
                btnPlay.Content = "Pause";
                IsPlaying(true);
                MediaPlayer.Play();
            }

        }

        private void btnMoveBack_Click(object sender, RoutedEventArgs e) {
            MediaPlayer.Pause();
            IsPlaying(false);
            btnPlay.Content = "Play";
            MediaPlayer.Position -= TimeSpan.FromMilliseconds(1000 / framerate);
            sldrVideoTime.Value = MediaPlayer.Position.TotalMilliseconds;

        }

        private void btnMoveForward_Click(object sender, RoutedEventArgs e) {
            MediaPlayer.Pause();
            IsPlaying(false);
            btnPlay.Content = "Play";
            MediaPlayer.Position += TimeSpan.FromMilliseconds(1000 / framerate);
            sldrVideoTime.Value = MediaPlayer.Position.TotalMilliseconds;
        }

        private void btnMoveForward_MouseDown(object sender, MouseButtonEventArgs e) {
            //btnMoveForward.RaiseEvent(e);
        }
        private static string MakeValidFileName(string name) {
            string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
        }
        private async void btnOpen_Click(object sender, RoutedEventArgs e) {
            // Configure open file dialog box 
            IsPlaying(false);
            btnPlay.Content = "Play";
            MediaPlayer.Stop();
            dialog.Filter = "Movie Files|*.mp4;*.mpg;*.avi;*.movl*.wmv";
            dialog.FilterIndex = 1;
            dialog.Title = "Select video file or paste YouTube video ID";
            dialog.FileName = "File or YT ID"; // Default file name 
            dialog.FileName = "C_VheAwZBuQ"; // Default file name 

            
            dialog.CheckFileExists = false;
            //dialog.DefaultExt = ".WMV"; // Default file extension 
            //dialog.Filter = "WMV file (.wm)|*.wmv"; // Filter files by extension  

            // Show open file dialog box 
            Nullable<bool> result = dialog.ShowDialog();

            // Process open file dialog box results  
            if (result == true) {
                Debug.WriteLine(dialog.FileName);
                if (!System.IO.File.Exists(dialog.FileName)) { //no file exists, try youtube
                    var video_fake_path = dialog.FileName.Split("/.\\".ToCharArray());
                    var video_id = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
                    var url = "https://youtu.be/" + video_id;
                    //var yt = VideoLibrary.YouTube.Default;
                    var t = Task.Factory.StartNew(new Action(() => {
                        try {
                            using (var service = VideoLibrary.Client.For(VideoLibrary.YouTube.Default)) {
                                using (var video = service.GetVideo(url)) {
                                    video_path = System.IO.Path.GetDirectoryName(dialog.FileName) + "\\" + MakeValidFileName(video.FullName);
                                    lvList.Dispatcher.Invoke((Action)(() => lvList.Items.Insert(0, "Downloading " + video.Title+ "...")));
                                    using (var outFile = System.IO.File.OpenWrite(video_path)) {
                                        using (var ps = new CGS.ProgressStream(outFile)) {
                                            var streamLength = (long)video.StreamLength();
                                            progress_bar.Dispatcher.Invoke((Action)(() => progress_bar.Maximum = streamLength));
                                            ps.BytesMoved += (sender_, args) => {
                                                //var percentage = args.StreamPosition * 100 / streamLength;
                                                progress_bar.Dispatcher.Invoke((Action)(() => progress_bar.Value = args.StreamPosition));

                                               //Debug.WriteLine($"{percentage}% of video downloaded");
                                            };

                                            video.Stream().CopyTo(ps);
                                        }
                                    }
                                     //= yt.GetVideo(url);
                                    Debug.WriteLine(video.FullName);
                                    Debug.WriteLine(MakeValidFileName(video.FullName));
                                } 

                                    //var bytes = await video.GetBytesAsync();
                                    //System.IO.File.WriteAllBytes(video_path, bytes);
                            }
                        }
                    catch (System.Net.Http.HttpRequestException) {
                            lvList.Dispatcher.Invoke((Action)(() => lvList.Items.Insert(0, "Unable to download video. Please try to download it manually and open from disk.")));
                        }
                        catch (System.InvalidOperationException) {
                            lvList.Dispatcher.Invoke((Action)(() => lvList.Items.Insert(0, "Bad filename or YouTube ID.")));
                        }
                    finally {
                            video_path = dialog.FileName;

                        }

                    }));
                    await Task.WhenAll(t);
                }
                else { //file supposedly exists
                    video_path = dialog.FileName;
                    if (new System.IO.FileInfo(video_path).Length > 0)  { //file or youtube really exists
                        InitVideo();
                    }
                }
            }


        }
        private void dispatcherTimer_Tick(object sender, EventArgs e) {
            if (is_playing) sldrVideoTime.Value = MediaPlayer.Position.TotalMilliseconds;
        }
        //public async Task<IConversionResult> MethodName() {
        //    IConversionResult result = await Conversion.Snapshot(dialog.FileName, dialog.FileName + ".png", TimeSpan.FromMilliseconds(MediaPlayer.Position.TotalMilliseconds - (1000 / framerate))).Start();
        //    return result;
        //}

        private void btnSnap_Click(object sender, RoutedEventArgs e) {
            //ScaleInvariantMatchThisFrame();
            //await MethodName();
            PreviewTemplate();
            btnCount.IsEnabled = true;


        }

        private void MediaPlayer_MediaOpened(object sender, RoutedEventArgs e) {
            //if (MediaPlayer.NaturalDuration.HasTimeSpan) {
            //    sldrVideoTime.Maximum = MediaPlayer.NaturalDuration.TimeSpan.TotalMilliseconds;
            //}
        }

        private void sldrVideoTime_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            //Debug.WriteLine(sldrVideoTime.Value);
            if (resume_on_up) {
                IsPlaying(true);
                MediaPlayer.Play();
            }
            //dispatcherTimer.IsEnabled = true;


        }

        private void sldrVideoTime_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {

            MediaPlayer.Pause();
            IsPlaying(false);

            //dispatcherTimer.IsEnabled = false;

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
                MediaPlayer.Pause();
                MediaPlayer.Position = new System.TimeSpan(0, 0, 0, 0, (int)sldrVideoTime.Value);

            }
        }

        private async void btnCount_Click(object sender, RoutedEventArgs e) {
            Stopwatch master_stopwatch = new Stopwatch();
            master_stopwatch.Start();
            is_working = true;
            List<Task> task_list = new List<Task>();
            ConcurrentBag<int> frame_count_bag = new ConcurrentBag<int>();

            sldrVideoTime.Ticks = new DoubleCollection(new double[] { 10 });
            DoubleCollection times = new DoubleCollection();
            btnMarkStart.IsEnabled = false;
            btnMarkEnd.IsEnabled = false;
            btnOpen.IsEnabled = false;
            btnSnap.IsEnabled = false;
            btnCount.IsEnabled = false;
            progress_timer.Restart();



            for (int core = 0; core < log_proc_count; core++) {
                object core_ = core;
                var t = Task.Factory.StartNew(new Action<object>((o) => {
                    var frame_count = CountLoadsToQueue((int)o, log_proc_count);
                    frame_count_bag.Add(frame_count);
                }), core_);
                task_list.Add(t);
            }

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
            GC.Collect();
            master_stopwatch.Stop();

            is_working = false;

            btnMarkStart.IsEnabled = true;
            btnMarkEnd.IsEnabled = true;
            btnOpen.IsEnabled = true;
            btnSnap.IsEnabled = true;
            btnCount.IsEnabled = true;
            lvList.Items.Insert(0, string.Format("Work over. Elapsed time: {0}", string.Format("{0:m\\:ss}", new TimeSpan(0, 0, 0, 0, (int)master_stopwatch.ElapsedMilliseconds))));
            lvList.Items.Insert(0, "Video processed (frames): " + run_frame_count);
            lvList.Items.Insert(0, "Processing ratio: " + (int)(run_length_msec / master_stopwatch.ElapsedMilliseconds));
            lvList.Items.Insert(0, "Loading frame count: " + loading_frame_count);
            lvList.Items.Insert(0, "Loading time: " + string.Format("{0:m\\:ss\\.fff}", new TimeSpan(0, 0, 0, 0, (int)(loading_frame_count * 1000 / framerate))));
            lvList.Items.Insert(0, "RTA time: " + string.Format("{0:h\\:mm\\:ss\\.fff}", new TimeSpan(0, 0, 0, 0, (int)run_length_msec)));
            lvList.Items.Insert(0, "Loadless time: " + string.Format("{0:h\\:mm\\:ss\\.fff}", new TimeSpan(0, 0, 0, 0, (int)((run_frame_count - loading_frame_count) * 1000 / framerate))));
        }

        private void btnMarkStart_Click(object sender, RoutedEventArgs e) {
            run_start_msec = (int)MediaPlayer.Position.TotalMilliseconds;
            run_start_frame = (int)(run_start_msec * framerate / 1000);
            var time_start_span = new TimeSpan(0, 0, 0, 0, (int)run_start_msec);
            sldrVideoTime.SelectionStart = run_start_msec;
            //lvList.Items.Insert(0, "start time: " + string.Format("{0:h\\:mm\\:ss\\.fff}", time_start_span) + " ,frame: " + run_start_frame);
            btnMarkEnd.IsEnabled = false;
            UpdateRunLengths();
        }

        private void btnMarkEnd_Click(object sender, RoutedEventArgs e) {
            run_end_msec = (int)MediaPlayer.Position.TotalMilliseconds;
            run_end_frame = (int)(run_end_msec * framerate / 1000);
            var time_end_span = new TimeSpan(0, 0, 0, 0, (int)run_end_msec);
            sldrVideoTime.SelectionEnd = run_end_msec;
            //lvList.Items.Insert(0, "end time: " + string.Format("{0:h\\:mm\\:ss\\.fff}", time_end_span) + " ,frame: " + run_end_frame);
            btnMarkStart.IsEnabled = false;
            UpdateRunLengths();
        }

        private void sldrVideoTime_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (!is_playing)
                MediaPlayer.Position = new System.TimeSpan(0, 0, 0, 0, (int)sldrVideoTime.Value);
            if (!is_working) {
                if (MediaPlayer.Position.TotalMilliseconds >= run_end_msec) btnMarkStart.IsEnabled = false; else btnMarkStart.IsEnabled = true;
                if (MediaPlayer.Position.TotalMilliseconds <= run_start_msec) btnMarkEnd.IsEnabled = false; else btnMarkEnd.IsEnabled = true;
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

            if (percent > old_value) {
                var time_msec = progress_timer.ElapsedMilliseconds;
                //double rate = 1 / (double)time_msec;
                double eta_msec = (1 - (progress_bar.Value / progress_bar.Maximum)) *100 * time_msec;
                string eta = "Time left: " + string.Format("{0:h\\:mm\\:ss}", new TimeSpan(0, 0, 0, 0, (int)eta_msec));
                lbl_eta.Content = eta;
                progress_timer.Restart();
                old_value = percent;

            }
            if (progress_bar.Value >= progress_bar.Maximum) {
                progress_bar.Value = 0;
                old_value = 0;
                lbl_eta.Content = "";
            }
        }

        private void MediaPlayer_MediaEnded(object sender, RoutedEventArgs e) {
            sldrVideoTime.Value = 0;
            MediaPlayer.Stop();
            IsPlaying(false);
            btnPlay.Content = "Play";

        }
    }
}