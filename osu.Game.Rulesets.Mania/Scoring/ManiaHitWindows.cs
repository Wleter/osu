// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Scoring;
using System;

namespace osu.Game.Rulesets.Mania.Scoring
{
    public class ManiaHitWindows : HitWindows
    {
        public override bool IsHitResultAllowed(HitResult result)
        {
            switch (result)
            {
                case HitResult.Perfect:
                case HitResult.Great:
                case HitResult.Good:
                case HitResult.Ok:
                case HitResult.Meh:
                case HitResult.Miss:
                    return true;
            }

            return false;
        }

        public override HitResult ResultFor(double timeOffset)
        {
            timeOffset = Math.Abs(Math.Round(timeOffset));

            for (var result = HitResult.Perfect; result >= HitResult.Miss; --result)
            {
                if (IsHitResultAllowed(result) && timeOffset <= Math.Floor(WindowFor(result)))
                    return result;
            }

            return HitResult.None;
        }
    }
}
