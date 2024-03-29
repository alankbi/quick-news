﻿using System;
using System.Collections.Generic;
using System.Drawing;
using CoreGraphics;
using Foundation;
using SafariServices;
using System.Threading.Tasks;

using UIKit;

namespace NewsApp
{
    public partial class ArticleViewController : UIViewController
    {
        private List<NewsArticle> articles;
        private int index;

        private UISwipeGestureRecognizer gestureLeft;
        private UISwipeGestureRecognizer gestureRight;

        //private UIColor testColor = UIColor.FromRGB(76, 217, 100); // green
        private UIColor testColor = UIColor.FromRGB(0, 101, 169); // blue

        private UIView[] articleDisplays;
        private bool[] clicked;

        private DateTime needsUpdate;

        private float Width = (float) UIScreen.MainScreen.Bounds.Width; // 375 iPhone 8
        private float Height = (float)UIScreen.MainScreen.Bounds.Height; // 667

        UINavigationBar bar;
        UILabel barText;
        UILabel time;
        UILabel similarArticles;

        UIButton leftButton;
        UIButton rightButton;

        UIButton link;
        UIView shade;

        public ArticleViewController(Cluster cluster, DateTime needsUpdate) : base("ArticleViewController", null)
        {
            this.articles = cluster.Articles;
            index = 0;
            this.needsUpdate = needsUpdate;

            articleDisplays = new UIView[articles.Count];
            clicked = new bool[articles.Count];

            gestureLeft = new UISwipeGestureRecognizer();
            gestureLeft.Direction = UISwipeGestureRecognizerDirection.Left;
            gestureLeft.AddTarget(() => HandleSwipe(index + 1));
            View.AddGestureRecognizer(gestureLeft);

            gestureRight = new UISwipeGestureRecognizer();
            gestureRight.Direction = UISwipeGestureRecognizerDirection.Right;
            gestureRight.AddTarget(() => HandleSwipe(index - 1));
            View.AddGestureRecognizer(gestureRight);
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            // Perform any additional setup after loading the view, typically from a nib.

            bar = new UINavigationBar(new CGRect(0, 0, Width, 45));
            bar.BarTintColor = testColor;
            View.AddSubview(bar);

            barText = new UILabel(new RectangleF(0, 6, Width, 45));
            barText.Text = "NewsApp";
            barText.TextAlignment = UITextAlignment.Center;
            barText.Font = UIFont.SystemFontOfSize(18);
            barText.TextColor = UIColor.White;
            barText.BackgroundColor = UIColor.Clear;
            View.AddSubview(barText);

            similarArticles = new UILabel(new RectangleF(0, Height - Height * 1 / 5, Width, Height * 2 / 15));
            similarArticles.Lines = 0;
            similarArticles.Text = "Similar Articles (1/" + articles.Count + ")";
            similarArticles.TextAlignment = UITextAlignment.Center;
            similarArticles.Font = UIFont.SystemFontOfSize(14 + (int)(Width / 25));
            similarArticles.AdjustsFontForContentSizeCategory = true;
            similarArticles.TranslatesAutoresizingMaskIntoConstraints = true;
            similarArticles.SizeToFit();
            similarArticles.Frame = new RectangleF((float)(Width / 2 - similarArticles.Frame.Width / 2 - 6), Height - Height * 1 / 5, (float)(similarArticles.Frame.Width + 12), (float)similarArticles.Frame.Height);
            similarArticles.TextColor = testColor;
            View.AddSubview(similarArticles);

            leftButton = UIButton.FromType(UIButtonType.System);
            leftButton.Frame = new RectangleF(0, (float)similarArticles.Frame.Top, (float)similarArticles.Frame.Left - 5, (float)similarArticles.Frame.Height);
            leftButton.SetTitle("<", UIControlState.Normal);
            leftButton.Font = UIFont.SystemFontOfSize(14 + (int)(Width / 25));
            leftButton.SetTitleColor(testColor, UIControlState.Normal);
            leftButton.SetTitleColor(UIColor.LightGray, UIControlState.Disabled);
            leftButton.HorizontalAlignment = UIControlContentHorizontalAlignment.Right;
            leftButton.Enabled = false;
            View.AddSubview(leftButton);

            rightButton = UIButton.FromType(UIButtonType.System);
            rightButton.Frame = new RectangleF((float)similarArticles.Frame.Right + 5, (float)similarArticles.Frame.Top, (float)(similarArticles.Frame.Left - 5), (float)similarArticles.Frame.Height);
            rightButton.SetTitle(">", UIControlState.Normal);
            rightButton.Font = UIFont.SystemFontOfSize(14 + (int)(Width / 25));
            rightButton.SetTitleColor(testColor, UIControlState.Normal);
            rightButton.SetTitleColor(UIColor.LightGray, UIControlState.Disabled);
            rightButton.HorizontalAlignment = UIControlContentHorizontalAlignment.Left;
            View.AddSubview(rightButton);

            link = UIButton.FromType(UIButtonType.System);
            link.Frame = new RectangleF(Width / 20, (float)bar.Frame.Bottom + Height / 10, Width - Width / 10, Height * 7 / 10 - (float)bar.Frame.Bottom);
            link.Layer.ShadowColor = UIColor.Gray.CGColor;
            link.Layer.Opacity = 1f;
            link.Layer.ShadowRadius = 5f;
            link.Layer.ShadowOffset = new SizeF(3f, 3f);
            link.Layer.MasksToBounds = false;
            View.AddSubview(link);

            shade = new UIView(new RectangleF(Width / 30, (float)bar.Frame.Bottom + Height / 50, Width - Width / 15, (float)(Height * 7 / 8 - bar.Frame.Bottom - Height / 50)));
            shade.Layer.BackgroundColor = UIColor.FromRGB(240, 240, 240).CGColor;
            shade.Layer.CornerRadius = 3;
            View.Add(shade);

            time = new UILabel(new RectangleF((float)shade.Frame.Left + 2, (float)shade.Frame.Bottom, Width, 50));
            time.Font = UIFont.SystemFontOfSize(6 + (int)(Width / 50));
            time.TextColor = UIColor.DarkGray;
            time.Text = "00:00:00 till next refresh_________";
            time.TranslatesAutoresizingMaskIntoConstraints = true;
            time.SizeToFit();
            View.AddSubview(time);

            UpdateTime();
            View.SendSubviewToBack(shade);

            leftButton.TouchUpInside += (sender, e) => 
            {
                HandleSwipe(index - 1);
            };

            rightButton.TouchUpInside += (sender, e) => 
            {
                HandleSwipe(index + 1);
            };

            link.TouchDown += (sender, e) =>
            {
                shade.Layer.BackgroundColor = UIColor.FromRGB(220, 220, 220).CGColor;
            };

            link.TouchDragInside += (sender, e) => 
            {
                shade.Layer.BackgroundColor = UIColor.FromRGB(240, 240, 240).CGColor;
            };

            link.TouchUpInside += (sender, e) => 
            {
                try
                {
                    var webView = new SFSafariViewController(new NSUrl(articles[index].Url));
                    PresentViewController(webView, true, null);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    var alert = new UIAlertView()
                    {
                        Title = "Cannot open link",
                        Message = "The link cannot be opened. "
                    };
                    alert.AddButton("OK");
                    alert.Show();
                }
                clicked[index] = true;
                shade.Layer.BackgroundColor = UIColor.FromRGB(240, 240, 240).CGColor;
            };

            for (int i = 0; i < articleDisplays.Length; i++)
            {
                articleDisplays[i] = InitializeArticleDisplays(articles[i]);
            }

            View.AddSubview(articleDisplays[index]);
            View.BringSubviewToFront(link);
        }

        private UIView InitializeArticleDisplays(NewsArticle article)
        {
            var tempView = new UIView();

            UILabel articleTitle;
            UIImageView articleImage;
            UILabel articleUrl;
            UILabel articleSource;
            UITextView articleDescription; // UILabels don't align to the top

            articleImage = new UIImageView(new RectangleF(Width / 20, (float)bar.Frame.Bottom + Height / 10, Width - Width / 10, Height / 3));
            articleImage.Image = FromUrl(article.UrlToImage);
            tempView.AddSubview(articleImage);

            articleSource = new UILabel(new RectangleF(Width / 20, (float)articleImage.Frame.Top - 70, Width - Width / 10, 70));
            articleSource.Text = article.Source.Name;
            articleSource.Font = UIFont.SystemFontOfSize(15 + (int)(Width / 20));
            articleSource.AdjustsFontSizeToFitWidth = true;
            articleSource.AdjustsFontForContentSizeCategory = true;
            articleSource.TranslatesAutoresizingMaskIntoConstraints = true;
            articleSource.SizeToFit();
            articleSource.Frame = new RectangleF(Width / 20, (float)(articleImage.Frame.Top - articleSource.Frame.Height), Width - Width / 10, (float)articleSource.Frame.Height);
            articleSource.TextColor = UIColor.Black;
            articleSource.BackgroundColor = UIColor.FromRGBA(0, 0, 0, 30);
            tempView.AddSubview(articleSource);

            articleUrl = new UILabel(new RectangleF(Width / 20, (float)articleImage.Frame.Bottom, Width - Width / 10, Height / 10));
            articleUrl.Lines = 1;
            articleUrl.Text = article.Url;
            articleUrl.Font = UIFont.SystemFontOfSize(6 + (int)(Width / 50));
            articleUrl.TranslatesAutoresizingMaskIntoConstraints = true;
            articleUrl.SizeToFit();
            articleUrl.Frame = new RectangleF(Width / 20, (float)articleImage.Frame.Bottom, Width - Width / 10, (float)articleUrl.Frame.Height);
            articleUrl.TextColor = UIColor.DarkGray;
            articleUrl.BackgroundColor = UIColor.FromRGBA(0, 0, 0, 30);
            tempView.AddSubview(articleUrl);

            articleTitle = new UILabel(new RectangleF(Width / 20, (float)articleUrl.Frame.Bottom + Height / 30, Width - Width / 10, Height / 4));
            articleTitle.Lines = 3;
            articleTitle.Text = article.Title;
            articleTitle.Font = UIFont.SystemFontOfSize(13 + (int)(Width / 35));
            articleTitle.TranslatesAutoresizingMaskIntoConstraints = true;
            articleTitle.SizeToFit();
            articleTitle.Frame = new RectangleF(Width / 20, (float)articleUrl.Frame.Bottom + Height / 30, Width - Width / 10, (float)articleTitle.Frame.Height);
            articleTitle.TextColor = UIColor.Black;
            articleTitle.Layer.CornerRadius = 3;
            articleTitle.BackgroundColor = UIColor.Clear;
            tempView.AddSubview(articleTitle);


            articleDescription = new UITextView(new RectangleF(Width / 20, (float)articleTitle.Frame.Bottom, Width - Width / 10, (float)(Height * 4 / 5 - articleTitle.Frame.Bottom)));
            articleDescription.Text = article.Description;
            articleDescription.Font = UIFont.SystemFontOfSize(10 + (int)(Width / 50));
            articleDescription.TextColor = UIColor.Black;
            articleDescription.BackgroundColor = UIColor.Clear; 
            articleDescription.Editable = false;
            articleDescription.Selectable = false;
            articleDescription.ScrollEnabled = false;
            articleDescription.TextContainer.LineBreakMode = UILineBreakMode.TailTruncation;
            tempView.AddSubview(articleDescription);

            return tempView;
        }

        private UIImage FromUrl(string uri)
        {
            if (uri == null) return null;
            using (var url = new NSUrl(uri))
            {
                using (var data = NSData.FromUrl(url))
                {
                    if (data == null)
                    {
                        return null;
                    }
                    return UIImage.LoadFromData(data);
                }
            }
        }

        private void HandleSwipe(int newIndex)
        {
            if (newIndex >= articles.Count || newIndex < 0)
            {
                return;
            }

            UIView.Transition(articleDisplays[index], articleDisplays[newIndex], .3f, UIViewAnimationOptions.TransitionFlipFromTop, null);
            index = newIndex;

            similarArticles.Text = "Similar Articles (" + (index + 1) + "/" + articles.Count + ")";
            similarArticles.SetNeedsDisplay();

            leftButton.Enabled = true;
            rightButton.Enabled = true;

            if (index == articles.Count - 1)
            {
                rightButton.Enabled = false;
            } 
            else if (index == 0)
            {
                leftButton.Enabled = false;
            }

            View.BringSubviewToFront(link);
        }

        public async void UpdateTime()
        {
            int secondsPassed = (int)(needsUpdate - DateTime.Now).TotalSeconds;
            while (secondsPassed >= 0) 
            {
                await Task.Delay(1000);
                time.Text = TimeSpan.FromSeconds(secondsPassed).ToString(@"hh\:mm\:ss") + " till next refresh";
                time.SetNeedsDisplay();
                secondsPassed = (int)(needsUpdate - DateTime.Now).TotalSeconds;
            }
            time.Text = "Relaunch app to refresh articles";
            time.SetNeedsDisplay();
        }

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            // Release any cached data, images, etc that aren't in use.
        }

    }
}

