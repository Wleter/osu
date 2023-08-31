// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Taiko.Scoring
{
    public class TaikoHitWindows : HitWindows
    {
        private static readonly DifficultyRange[] taiko_ranges =
        {
            new DifficultyRange(HitResult.Great, 50, 35, 20),
            new DifficultyRange(HitResult.Ok, 120, 80, 50),
            new DifficultyRange(HitResult.Miss, 135, 95, 70),
        };

        public override bool IsHitResultAllowed(HitResult result)
        {
            switch (result)
            {
                case HitResult.Great:
                case HitResult.Ok:
                case HitResult.Miss:
                    return true;
            }

            return false;
        }

        public override HitResult ResultFor(double timeOffset)
        {
            timeOffset = Math.Abs(Math.Round(timeOffset));

            for (var result = HitResult.Perfect; result > HitResult.Miss; --result)
            {
                if (IsHitResultAllowed(result) && timeOffset < Math.Floor(WindowFor(result)))
                    return result;
            }

            if (IsHitResultAllowed(HitResult.Miss) && timeOffset <= Math.Floor(WindowFor(HitResult.Miss)))
                return HitResult.Miss;

            return HitResult.None;
        }

        protected override DifficultyRange[] GetRanges() => taiko_ranges;
    }
}
