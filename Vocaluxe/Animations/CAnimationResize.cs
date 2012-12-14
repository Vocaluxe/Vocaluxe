using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Xml.XPath;

using Vocaluxe.Base;
using Vocaluxe.Lib.Draw;

namespace Vocaluxe.Menu.Animations
{
    public class CAnimationResize : CAnimationFramework
    {
        public SRectF FinalRect;
        public SRectF CurrentRect;
        public SRectF OriginalRect;

        public CAnimationResize()
        {
        }

        public override void Init()
        {
            Type = EAnimationType.Resize;
        }

        public override bool LoadAnimation(string item, XPathNavigator navigator)
        {
            //Load normal animation-options
            _AnimationLoaded &= base.LoadAnimation(item, navigator);

            //Load specific animation-options
            _AnimationLoaded &= CHelper.TryGetFloatValueFromXML(item + "/X", navigator, ref FinalRect.X);
            _AnimationLoaded &= CHelper.TryGetFloatValueFromXML(item + "/Y", navigator, ref FinalRect.Y);
            _AnimationLoaded &= CHelper.TryGetFloatValueFromXML(item + "/W", navigator, ref FinalRect.W);
            _AnimationLoaded &= CHelper.TryGetFloatValueFromXML(item + "/H", navigator, ref FinalRect.H);

            return _AnimationLoaded;
        }

        public override void setRect(SRectF rect)
        {
            OriginalRect = rect;
        }

        public override SRectF getRect()
        {
            return CurrentRect;
        }

        public override void StartAnimation()
        {
            base.StartAnimation();

            CurrentRect = OriginalRect;
        }

        public override void Update()
        {
            //Update CurrentRect
            if (!ResetMode)
            {
                CLog.LogError((CurrentRect.X + ((FinalRect.X - CurrentRect.X) / Speed * Timer.ElapsedMilliseconds)).ToString("#.#####"));
                CurrentRect.X = CurrentRect.X + ((FinalRect.X - CurrentRect.X) / Speed * Timer.ElapsedMilliseconds);
                CurrentRect.Y = CurrentRect.Y + ((FinalRect.Y - CurrentRect.Y) / Speed * Timer.ElapsedMilliseconds);
                CurrentRect.H = CurrentRect.H + ((FinalRect.H - CurrentRect.H) / Speed * Timer.ElapsedMilliseconds);
                CurrentRect.W = CurrentRect.W + ((FinalRect.W - CurrentRect.W) / Speed * Timer.ElapsedMilliseconds);
            }
            else
            {
                CurrentRect.X = CurrentRect.X + ((OriginalRect.X - CurrentRect.X) / Speed * Timer.ElapsedMilliseconds);
                CurrentRect.Y = CurrentRect.Y + ((OriginalRect.Y - CurrentRect.Y) / Speed * Timer.ElapsedMilliseconds);
                CurrentRect.H = CurrentRect.H + ((OriginalRect.H - CurrentRect.H) / Speed * Timer.ElapsedMilliseconds);
                CurrentRect.W = CurrentRect.W + ((OriginalRect.W - CurrentRect.W) / Speed * Timer.ElapsedMilliseconds);
            }

            if (CurrentRect.X == FinalRect.X && CurrentRect.Y == FinalRect.Y && CurrentRect.H == FinalRect.H && CurrentRect.W == FinalRect.W)
            {
                switch (Repeat)
                {
                    case EAnimationRepeat.Repeat:
                        StopAnimation();
                        CurrentRect = OriginalRect;
                        StartAnimation();
                        break;

                    case EAnimationRepeat.RepeatWithReset:
                        ResetAnimation();
                        break;

                    case EAnimationRepeat.Reset:
                        if (!ResetMode)
                            ResetAnimation();
                        else
                            StopAnimation();
                        break;

                    case EAnimationRepeat.None:
                        StopAnimation();
                        break;
                }
            }
        }
    }
}
