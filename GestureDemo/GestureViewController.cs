using System;
using System.Collections.Generic;
using System.Linq;
using CoreGraphics;
using UIKit;

namespace GestureDemo
{
    public class GestureViewController : UIViewController
    {
        #region Constants

        private static readonly CGSize CanvasSize = new CGSize(1000, 1000);
        private static readonly CGSize ElementSize = new CGSize(50, 50);
        private static readonly UIColor ScrollViewBackgroundColor = UIColor.LightGray;
        private static readonly UIColor CanvasBackgroundColor = UIColor.White;
        private static readonly UIColor ElementColor = UIColor.Blue;
        private static readonly UIColor SelectedBorderColor = UIColor.Red;
        private const int SelectedBorderWidth = 2;

        #endregion

        #region View fields

        private UIScrollView _scrollView;
        private UIView _canvasView;

        #endregion

        #region Gesture state fields

        private readonly HashSet<UIView> _selectedElements = new HashSet<UIView>();

        #endregion

        #region View controller overrides

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            _canvasView = new UIView
            {
                BackgroundColor = CanvasBackgroundColor,
                Bounds = new CGRect(CGPoint.Empty, CanvasSize),
            };

            _scrollView = new UIScrollView()
            {
                ContentSize = CanvasSize,
                ViewForZoomingInScrollView = sv => _canvasView,
                BackgroundColor = ScrollViewBackgroundColor,
            };

            _scrollView.AddSubview(_canvasView);

            View.AddSubview(_scrollView);

            AddGestures();

            AddInitialElements();
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            _scrollView.Frame = View.Bounds;
            _canvasView.Center = new CGPoint(CanvasSize.Width / 2, CanvasSize.Height / 2);
        }

        #endregion

        #region Gestures

        private void AddGestures()
        {
            // Tap to select
            var tapGesture = new UITapGestureRecognizer(HandleTap);
            _canvasView.AddGestureRecognizer(tapGesture);

            // Move an element
            var elementDragGesture = new UIPanGestureRecognizer(HandlePan);
            _canvasView.AddGestureRecognizer(elementDragGesture);

            elementDragGesture.ShouldBegin = g =>
            {
                var locationInCanvas = g.LocationInView(_canvasView);
                var touchedElement = ElementUnderPoint(locationInCanvas);

                return touchedElement != null && IsElementSelected(touchedElement);
            };

            var selectLongPressGesture = new UILongPressGestureRecognizer(HandleLongPress) { MinimumPressDuration = 0.1 };
            _canvasView.AddGestureRecognizer(selectLongPressGesture);

            selectLongPressGesture.ShouldReceiveTouch = (g, touch) =>
            {
                var locationInCanvas = touch.LocationInView(_canvasView);
                var touchedElement = ElementUnderPoint(locationInCanvas);

                return touchedElement != null && !IsElementSelected(touchedElement);
            };

            selectLongPressGesture.ShouldRecognizeSimultaneously = (g1, g2) => g2 == elementDragGesture;
        }

        private void HandleTap(UITapGestureRecognizer gesture)
        {
            var locationInCanvas = gesture.LocationInView(_canvasView);
            var touchedElement = ElementUnderPoint(locationInCanvas);
            bool didTouchElement = touchedElement != null;

            ClearSelection();

            if (didTouchElement)
            {
                SetElementSelected(touchedElement, selected: true);
            }
        }

        private void HandlePan(UIPanGestureRecognizer gesture)
        {
            switch (gesture.State)
            {
                case UIGestureRecognizerState.Began:
                    break;
                case UIGestureRecognizerState.Changed:
                    HandlePanChanged(gesture);
                    break;
                case UIGestureRecognizerState.Ended:
                    HandlePanEnded(gesture);
                    CleanupAfterPan();
                    break;
                case UIGestureRecognizerState.Cancelled:
                    CleanupAfterPan();
                    break;
            }
        }

        private void HandlePanChanged(UIPanGestureRecognizer gesture)
        {
            var translation = gesture.TranslationInView(_canvasView);
            var transform = CGAffineTransform.MakeTranslation(translation.X, translation.Y);

            foreach (var view in _selectedElements)
            {
                view.Transform = transform;
            }
        }

        private void HandlePanEnded(UIPanGestureRecognizer gesture)
        {
            var translation = gesture.TranslationInView(_canvasView);

            foreach (var view in _selectedElements)
            {
                var center = view.Center;
                center.X += translation.X;
                center.Y += translation.Y;

                view.Center = center;
            }
        }

        private void CleanupAfterPan()
        {
            var identityTransform = CGAffineTransform.MakeIdentity();
            foreach (var view in _selectedElements)
            {
                view.Transform = identityTransform;
            }
        }

        private void HandleLongPress(UILongPressGestureRecognizer gesture)
        {
            if (gesture.State == UIGestureRecognizerState.Began)
            {
                var locationInCanvas = gesture.LocationInView(_canvasView);
                var touchedElement = ElementUnderPoint(locationInCanvas);

                ClearSelection();
                SetElementSelected(touchedElement, selected: true);
            }
        }

        #endregion

        #region Element and selection management

        private void AddInitialElements()
        {
            const int NumberOfElements = 4;
            nfloat HorizontalSpacing = ElementSize.Width / 2;
            nfloat VerticalSpacing = ElementSize.Height / 2;

            var location = new CGPoint(HorizontalSpacing * 2, VerticalSpacing * 2);
            for (int i = 0; i != NumberOfElements; ++i)
            {
                DropNewElement(location);

                location.X += ElementSize.Width + HorizontalSpacing;
                location.Y += ElementSize.Height + VerticalSpacing;
            }

            ClearSelection();
        }

        private void DropNewElement(CGPoint locationInCanvas)
        {
            var newElement = new UIView
            {
                Bounds = new CGRect(CGPoint.Empty, ElementSize),
                BackgroundColor = ElementColor,
            };
            newElement.Layer.BorderColor = SelectedBorderColor.CGColor;

            newElement.Center = locationInCanvas;

            _canvasView.AddSubview(newElement);

            SetElementSelected(newElement, selected: true);
        }

        private bool IsElementSelected(UIView view)
        {
            return _selectedElements.Contains(view);
        }

        private bool TryClearSelection()
        {
            bool anySelected = _selectedElements.Count != 0;

            if (anySelected)
            {
                ClearSelection();
            }

            return anySelected;
        }

        private void ClearSelection()
        {
            foreach (var view in _selectedElements.ToList())
            {
                SetElementSelected(view, selected: false);
            }
        }

        private void SetElementSelected(UIView view, bool selected)
        {
            if (selected)
            {
                _selectedElements.Add(view);
            }
            else
            {
                _selectedElements.Remove(view);
            }

            var borderWidth = selected ? SelectedBorderWidth : 0;
            view.Layer.BorderWidth = borderWidth;
        }

        private UIView ElementUnderPoint(CGPoint locationInCanvas)
        {
            var hitTestView = _canvasView.HitTest(locationInCanvas, null);
            if (hitTestView == _canvasView)
            {
                return null;
            }

            return hitTestView;
        }

        #endregion

        #region Utility

        private void CancelScrollGesture()
        {
            // Cancel the gesture by disabling and re-enabling the gesture.
            // This is documented behavior.
            _scrollView.PanGestureRecognizer.Enabled = false;
            _scrollView.PanGestureRecognizer.Enabled = true;
        }

        #endregion
    }
}

