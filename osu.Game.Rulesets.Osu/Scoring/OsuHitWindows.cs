﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Osu.Scoring
{
    public class OsuHitWindows : HitWindows
    {
        /// <summary>
        /// osu! ruleset has a fixed miss window regardless of difficulty settings.
        /// </summary>
        public const double MISS_WINDOW = 400;

        private static readonly DifficultyRange[] osu_ranges =
        {
            new DifficultyRange(HitResult.Great, 80, 50, 20),
            new DifficultyRange(HitResult.Ok, 140, 100, 60),
            new DifficultyRange(HitResult.Meh, 200, 150, 100),
            new DifficultyRange(HitResult.Miss, MISS_WINDOW, MISS_WINDOW, MISS_WINDOW),
        };

        public override bool IsHitResultAllowed(HitResult result)
        {
            switch (result)
            {
                case HitResult.Great:
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
                if (IsHitResultAllowed(result) && timeOffset < Math.Floor(WindowFor(result)))
                    return result;
            }

            return HitResult.None;
        }

        protected override DifficultyRange[] GetRanges() => osu_ranges;
    }
}
