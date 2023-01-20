using System;
using System.Runtime.InteropServices;
using ARKit;
using CoreGraphics;
using CoreImage;
using UIKit;

namespace LidarSample
{
    public partial class ViewController : UIViewController
    {
        ARSCNView _arscnView;

        public ViewController (IntPtr handle) : base (handle)
        {
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            _arscnView = new ARSCNView();
            this.Add(_arscnView);
            _arscnView.TranslatesAutoresizingMaskIntoConstraints = false;
            NSLayoutConstraint.ActivateConstraints(new[] {
                _arscnView.TopAnchor.ConstraintEqualTo(_arscnView.Superview.TopAnchor),
                _arscnView.LeftAnchor.ConstraintEqualTo(_arscnView.Superview.LeftAnchor),
                _arscnView.RightAnchor.ConstraintEqualTo(_arscnView.Superview.RightAnchor),
                _arscnView.BottomAnchor.ConstraintEqualTo(_arscnView.Superview.BottomAnchor),
            });

            var imageView = new UIImageView();
            this.Add(imageView);
            imageView.TranslatesAutoresizingMaskIntoConstraints = false;
            NSLayoutConstraint.ActivateConstraints(new[] {
                imageView.TopAnchor.ConstraintEqualTo(imageView.Superview.TopAnchor),
                imageView.LeftAnchor.ConstraintEqualTo(imageView.Superview.LeftAnchor),
                imageView.RightAnchor.ConstraintEqualTo(imageView.Superview.RightAnchor),
                imageView.BottomAnchor.ConstraintEqualTo(imageView.Superview.BottomAnchor),
            });

            var showDepthButton = new UIButton();
            showDepthButton.SetTitle("深度情報表示", UIControlState.Normal);
            showDepthButton.Configuration = UIButtonConfiguration.FilledButtonConfiguration;
            this.Add(showDepthButton);
            showDepthButton.TranslatesAutoresizingMaskIntoConstraints = false;
            NSLayoutConstraint.ActivateConstraints(new[] {
                showDepthButton.BottomAnchor.ConstraintEqualTo(showDepthButton.Superview.BottomAnchor, -50),
                showDepthButton.CenterXAnchor.ConstraintEqualTo(showDepthButton.Superview.CenterXAnchor),
            });

            var captured = true;
            showDepthButton.ExclusiveTouch = true;
            showDepthButton.TouchUpInside += (sender, e) =>
            {
                showDepthButton.Enabled = false;
                if (captured)
                {
                    showDepthButton.SetTitle("キャプチャ開始", UIControlState.Normal);

                    var depthMap = _arscnView.Session.CurrentFrame.SceneDepth.DepthMap;
                    using (var ciImage = new CIImage(depthMap))
                    using (var context = new CIContext())
                    using (var cgImage = context.CreateCGImage(ciImage, new CGRect(0, 0, depthMap.Width, depthMap.Height)))
                    {
                        imageView.Image = UIImage.FromImage(cgImage);
                    }

                    depthMap.Lock(CoreVideo.CVPixelBufferLock.ReadOnly);
                    var depthBuffer = new float[depthMap.Width * depthMap.Height];
                    Marshal.Copy(depthMap.BaseAddress, depthBuffer, 0, depthBuffer.Length);
                    depthMap.Unlock(CoreVideo.CVPixelBufferLock.ReadOnly);

                    for (var y = 0; y < depthMap.Height; y++)
                    {
                        for (var x = 0; x < depthMap.Width; x++)
                        {
                            var distance = depthBuffer[y * depthMap.Width + x];
                            System.Diagnostics.Debug.WriteLine($"Depth : x = {x}, y = {y}, distance = {distance}");
                        }
                    }

                    _arscnView.Session.Pause();
                    captured = false;
                }
                else
                {
                    showDepthButton.SetTitle("深度情報表示", UIControlState.Normal);
                    if (imageView.Image != null)
                    {
                        imageView.Image.Dispose();
                        imageView.Image = null;
                    }
                    StartSession();
                    captured = true;
                }
                showDepthButton.Enabled = true;
            };
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            StartSession();
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);
            _arscnView.Session.Pause();
        }

        void StartSession()
        {
            var config = new ARWorldTrackingConfiguration();
            config.FrameSemantics = ARFrameSemantics.SceneDepth;
            _arscnView.Session.Run(config, new ARSessionRunOptions());
        }
    }
}
