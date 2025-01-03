// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;

namespace osu.Game.Online.Leaderboards
{
    public record LeaderboardScores<TScoreInfo>
    {
        public List<TScoreInfo> Scores = new List<TScoreInfo>();

        public TScoreInfo? UserScore;
    }
}
