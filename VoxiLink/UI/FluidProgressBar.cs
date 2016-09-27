using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace VoxityClient.UI
{
    /// <summary>
    /// Interaction logic for FluidProgressBar.xaml
    /// </summary>
    public partial class FluidProgressBar : UserControl, IDisposable
    {
        #region Internal class

        private class KeyFrameDetails
        {
            public KeyTime KeyFrameTime { get; set; }
            public List<DoubleKeyFrame> KeyFrames { get; set; }
        }

        #endregion

        #region Fields

        Dictionary<int, KeyFrameDetails> keyFrameMap = null;
        Dictionary<int, KeyFrameDetails> opKeyFrameMap = null;
        //KeyTime keyA = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0));
        //KeyTime keyB = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.5));
        //KeyTime keyC = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2.0));
        //KeyTime keyD = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2.5));
        Storyboard sb;
        bool isStoryboardRunning;

    #endregion

    #region Dependency Properties

    #endregion

    #region Construction / Initialization

    /// <summary>
    /// Ctor
    /// </summary>
    public FluidProgressBar()
        {
            InitializeComponent();

            keyFrameMap = new Dictionary<int, KeyFrameDetails>();
            opKeyFrameMap = new Dictionary<int, KeyFrameDetails>();

            GetKeyFramesFromStoryboard();

            this.SizeChanged += new SizeChangedEventHandler(OnSizeChanged);
            this.Loaded += new RoutedEventHandler(OnLoaded);
            this.IsVisibleChanged += new DependencyPropertyChangedEventHandler(OnIsVisibleChanged);
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the Loaded event
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">EventArgs</param>
        void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // Update the key frames
            UpdateKeyFrames();
            // Start the animation
            StartFluidAnimation();
        }

        /// <summary>
        /// Handles the SizeChanged event
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">EventArgs</param>
        void OnSizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            // Restart the animation
            RestartStoryboardAnimation();
        }

        /// <summary>
        /// Handles the IsVisibleChanged event
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">EventArgs</param>
        void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.Visibility == Visibility.Visible)
            {
                UpdateKeyFrames();
                StartFluidAnimation();
            }
            else
            {
                StopFluidAnimation();
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Starts the animation
        /// </summary>
        private void StartFluidAnimation()
        {
            if ((sb != null) && (!isStoryboardRunning))
            {
                sb.Begin();
                isStoryboardRunning = true;
            }
        }

        /// <summary>
        /// Stops the animation
        /// </summary>
        private void StopFluidAnimation()
        {
            if ((sb != null) && (isStoryboardRunning))
            {
                // Move the timeline to the end and stop the animation
                sb.SeekAlignedToLastTick(TimeSpan.FromSeconds(0));
                sb.Stop();
                isStoryboardRunning = false;
            }
        }

        /// <summary>
        /// Stops the animation, updates the keyframes and starts the animation
        /// </summary>
        private void RestartStoryboardAnimation()
        {
            StopFluidAnimation();
            UpdateKeyFrames();
            StartFluidAnimation();
        }

        /// <summary>
        /// Obtains the keyframes for each animation in the storyboard so that
        /// they can be updated when required.
        /// </summary>
        private void GetKeyFramesFromStoryboard()
        {
            sb = (Storyboard)this.Resources["FluidStoryboard"];
            if (sb != null)
            {
                foreach (Timeline timeline in sb.Children)
                {
                    DoubleAnimationUsingKeyFrames dakeys = timeline as DoubleAnimationUsingKeyFrames;
                    if (dakeys != null)
                    {
                        string targetName = Storyboard.GetTargetName(dakeys);
                        ProcessDoubleAnimationWithKeys(dakeys,
                                 !targetName.StartsWith("Trans"));
                    }
                }
            }
        }

        /// <summary>
        /// Gets the keyframes in the given animation and stores them in a map
        /// </summary>
        /// <param name="dakeys">Animation containg keyframes</param>
        /// <param name="isOpacityAnim">Flag to indicate whether
        ///   the animation targets the opacity or the translate transform</param>
        private void ProcessDoubleAnimationWithKeys(DoubleAnimationUsingKeyFrames dakeys, bool isOpacityAnim = false)
        {
            // Get all the keyframes in the instance.
            for (int i = 0; i < dakeys.KeyFrames.Count; i++)
            {
                DoubleKeyFrame frame = dakeys.KeyFrames[i];

                Dictionary<int, KeyFrameDetails> targetMap = null;

                if (isOpacityAnim)
                {
                    targetMap = opKeyFrameMap;
                }
                else
                {
                    targetMap = keyFrameMap;
                }

                if (!targetMap.ContainsKey(i))
                {
                    targetMap[i] = new KeyFrameDetails() { KeyFrames = new List<DoubleKeyFrame>() };
                }

                // Update the keyframe time and add it to the map
                targetMap[i].KeyFrameTime = frame.KeyTime;
                targetMap[i].KeyFrames.Add(frame);
            }
        }

        /// <summary>
        /// Update the key value of each keyframe based on the current width of the FluidProgressBar
        /// </summary>
        private void UpdateKeyFrames()
        {
            // Get the current width of the FluidProgressBar
            double width = this.ActualWidth;
            // Update the values only if the current width is greater than Zero and is visible
            if ((width > 0.0) && (this.Visibility == System.Windows.Visibility.Visible))
            {
                double Point0 = -10;
                double PointA = width * KeyFrameA;
                double PointB = width * KeyFrameB;
                double PointC = width + 10;
                // Update the keyframes stored in the map
                UpdateKeyFrame(0, Point0);
                UpdateKeyFrame(1, PointA);
                UpdateKeyFrame(2, PointB);
                UpdateKeyFrame(3, PointC);
            }
        }

        /// <summary>
        /// Update the key value of the keyframes stored in the map
        /// </summary>
        /// <param name="key">Key of the dictionary</param>
        /// <param name="newValue">New value
        ///         to be given to the key value of the keyframes</param>
        private void UpdateKeyFrame(int key, double newValue)
        {
            if (keyFrameMap.ContainsKey(key))
            {
                foreach (var frame in keyFrameMap[key].KeyFrames)
                {
                    if (frame is LinearDoubleKeyFrame)
                    {
                        frame.SetValue(LinearDoubleKeyFrame.ValueProperty, newValue);
                    }
                    else if (frame is EasingDoubleKeyFrame)
                    {
                        frame.SetValue(EasingDoubleKeyFrame.ValueProperty, newValue);
                    }
                }
            }
        }

        /// <summary>
        /// Updates the duration of each of the keyframes stored in the map
        /// </summary>
        /// <param name="key">Key of the dictionary</param>
        /// <param name="newValue">New value to be given
        ///           to the duration value of the keyframes</param>
        private void UpdateKeyTimes(int key, Duration newDuration)
        {
            switch (key)
            {
                case 1:
                    UpdateKeyTime(1, newDuration);
                    UpdateKeyTime(2, newDuration + DurationB);
                    UpdateKeyTime(3, newDuration + DurationB + DurationC);
                    break;

                case 2:
                    UpdateKeyTime(2, DurationA + newDuration);
                    UpdateKeyTime(3, DurationA + newDuration + DurationC);
                    break;

                case 3:
                    UpdateKeyTime(3, DurationA + DurationB + newDuration);
                    break;

                default:
                    break;
            }

            // Update the opacity animation duration based on the complete duration
            // of the animation
            UpdateOpacityKeyTime(1, DurationA + DurationB + DurationC);
        }

        /// <summary>
        /// Updates the duration of each of the keyframes stored in the map
        /// </summary>
        /// <param name="key">Key of the dictionary</param>
        /// <param name="newDuration">New value to be given
        ///              to the duration value of the keyframes</param>
        private void UpdateKeyTime(int key, Duration newDuration)
        {
            if (keyFrameMap.ContainsKey(key))
            {
                KeyTime newKeyTime = KeyTime.FromTimeSpan(newDuration.TimeSpan);
                keyFrameMap[key].KeyFrameTime = newKeyTime;

                foreach (var frame in keyFrameMap[key].KeyFrames)
                {
                    if (frame is LinearDoubleKeyFrame)
                    {
                        frame.SetValue(LinearDoubleKeyFrame.KeyTimeProperty, newKeyTime);
                    }
                    else if (frame is EasingDoubleKeyFrame)
                    {
                        frame.SetValue(EasingDoubleKeyFrame.KeyTimeProperty, newKeyTime);
                    }
                }
            }
        }

        /// <summary>
        /// Updates the duration of the second keyframe of all the opacity animations
        /// </summary>
        /// <param name="key">Key of the dictionary</param>
        /// <param name="newDuration">New value to be given
        ///         to the duration value of the keyframes</param>
        private void UpdateOpacityKeyTime(int key, Duration newDuration)
        {
            if (opKeyFrameMap.ContainsKey(key))
            {
                KeyTime newKeyTime = KeyTime.FromTimeSpan(newDuration.TimeSpan);
                opKeyFrameMap[key].KeyFrameTime = newKeyTime;

                foreach (var frame in opKeyFrameMap[key].KeyFrames)
                {
                    if (frame is DiscreteDoubleKeyFrame)
                    {
                        frame.SetValue(DiscreteDoubleKeyFrame.KeyTimeProperty, newKeyTime);
                    }
                }
            }
        }

        /// <summary>
        /// Updates the delay between consecutive timelines
        /// </summary>
        /// <param name="newDelay">Delay duration</param>
        private void UpdateTimelineDelay(Duration newDelay)
        {
            Duration nextDelay = new Duration(TimeSpan.FromSeconds(0));

            if (sb != null)
            {
                for (int i = 0; i < sb.Children.Count; i++)
                {
                    // The first five animations are for translation
                    // The next five animations are for opacity
                    if (i == 5)
                        nextDelay = newDelay;
                    else
                        nextDelay += newDelay;


                    DoubleAnimationUsingKeyFrames timeline = sb.Children[i] as DoubleAnimationUsingKeyFrames;
                    if (timeline != null)
                    {
                        timeline.SetValue(DoubleAnimationUsingKeyFrames.BeginTimeProperty, nextDelay.TimeSpan);
                    }
                }
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Releases all resources used by an instance of the FluidProgressBar class.
        /// </summary>
        /// <remarks>
        /// This method calls the virtual Dispose(bool) method, passing in 'true', and then suppresses 
        /// finalization of the instance.
        /// </remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged resources before an instance of the FluidProgressBar
        ///         class is reclaimed by garbage collection.
        /// </summary>
        /// <remarks>
        /// NOTE: Leave out the finalizer altogether if this class doesn't own unmanaged resources itself, 
        /// but leave the other methods exactly as they are.
        /// This method releases unmanaged resources by calling the virtual Dispose(bool), passing in 'false'.
        /// </remarks>
        ~FluidProgressBar()
        {
            Dispose(false);
        }

        /// <summary>
        /// Releases the unmanaged resources used by an instance
        ///      of the FluidProgressBar class and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">'true' to release both managed
        ///      and unmanaged resources; 'false' to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources here
                this.SizeChanged -= OnSizeChanged;
                this.Loaded -= OnLoaded;
                this.IsVisibleChanged -= OnIsVisibleChanged;
            }

            // free native resources if there are any.
        }

        #endregion
    }
}
