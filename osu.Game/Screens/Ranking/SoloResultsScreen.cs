// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Bindables;
using osu.Game.Scoring;

namespace osu.Game.Screens.Ranking
{
    public partial class SoloResultsScreen : ResultsScreen
    {
        public SoloResultsScreen(ScoreInfo score)
            : base(score)
        {
        }

        public readonly IBindableList<ScoreInfo> LeaderboardScores = new BindableList<ScoreInfo>();

        protected override void LoadComplete()
        {
            base.LoadComplete();

            LeaderboardScores.BindCollectionChanged((_, _) => Scheduler.AddOnce(onScoresChanged), true);
        }

        private void onScoresChanged() => FetchScoresCallback(LeaderboardScores.Where(s => !s.Equals(Score)));
    }
}
