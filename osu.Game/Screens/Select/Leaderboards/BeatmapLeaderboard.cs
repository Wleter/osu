// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Game.Beatmaps;
using osu.Game.Extensions;
using osu.Game.Online.API;
using osu.Game.Online.Leaderboards;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Scoring;

namespace osu.Game.Screens.Select.Leaderboards
{
    public partial class BeatmapLeaderboard : Leaderboard<BeatmapLeaderboardScope, ScoreInfo>
    {
        public Action<ScoreInfo>? ScoreSelected;

        private BeatmapLeaderboardProvider? scoresProvider;
        public BeatmapLeaderboardProvider? ScoresProvider
        {
            get => scoresProvider;
            set
            {
                scoresProvider = value;

                if (scoresProvider != null)
                    scoresProvider.OnScoresFetched += (u) => SetScores(u.Scores, u.UserScore);

                RefetchScores();
            }
        }

        private BeatmapInfo? beatmapInfo;

        public BeatmapInfo? BeatmapInfo
        {
            get => beatmapInfo;
            set
            {
                if (beatmapInfo == null && value == null)
                    return;

                if (beatmapInfo?.Equals(value) == true)
                    return;

                beatmapInfo = value;

                // Refetch is scheduled, which can cause scores to be outdated if the leaderboard is not currently updating.
                // As scores are potentially used by other components, clear them eagerly to ensure a more correct state.
                SetScores(null);

                RefetchScores();
            }
        }

        private bool filterMods;

        /// <summary>
        /// Whether to apply the game's currently selected mods as a filter when retrieving scores.
        /// </summary>
        public bool FilterMods
        {
            get => filterMods;
            set
            {
                if (value == filterMods)
                    return;

                filterMods = value;

                RefetchScores();
            }
        }

        [Resolved]
        private IBindable<RulesetInfo> ruleset { get; set; } = null!;

        [Resolved]
        private IBindable<IReadOnlyList<Mod>> mods { get; set; } = null!;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            ruleset.ValueChanged += _ => RefetchScores();
            mods.ValueChanged += _ =>
            {
                if (filterMods)
                    RefetchScores();
            };
        }

        protected override bool IsOnlineScope => ScoresProvider is OnlineBeatmapLeaderboardProvider;

        protected override APIRequest? FetchScores(CancellationToken cancellationToken)
        {
            var fetchBeatmapInfo = BeatmapInfo;

            if (fetchBeatmapInfo == null || ScoresProvider == null)
            {
                SetErrorState(LeaderboardState.NoneSelected);
                return null;
            }

            if (!api.LocalUser.Value.IsSupporter && ScoresProvider.RequireSupporter(filterMods))
            {
                SetErrorState(LeaderboardState.NotSupporter);
                return null;
            }

            var fetchRuleset = ruleset.Value ?? fetchBeatmapInfo.Ruleset;

            if (IsOnlineScope)
            {
                if (!api.IsLoggedIn)
                {
                    SetErrorState(LeaderboardState.NotLoggedIn);
                    return null;
                }

                if (!fetchRuleset.IsLegacyRuleset())
                {
                    SetErrorState(LeaderboardState.RulesetUnavailable);
                    return null;
                }

                if (fetchBeatmapInfo.OnlineID <= 0 || fetchBeatmapInfo.Status <= BeatmapOnlineStatus.Pending)
                {
                    SetErrorState(LeaderboardState.BeatmapUnavailable);
                    return null;
                }
            }

            IReadOnlyList<Mod>? requestMods = null;

            if (filterMods && !mods.Value.Any())
                // add nomod for the request
                requestMods = new Mod[] { new ModNoMod() };
            else if (filterMods)
                requestMods = mods.Value;

            var criteria = new BeatmapScoresCriteria
            {
                BeatmapInfo = fetchBeatmapInfo,
                Ruleset = fetchRuleset,
                RequestMods = requestMods
            };

            return ScoresProvider.FetchScores(criteria, cancellationToken);
        }

        protected override LeaderboardScore CreateDrawableScore(ScoreInfo model, int index) => new LeaderboardScore(model, index, IsOnlineScope)
        {
            Action = () => ScoreSelected?.Invoke(model)
        };

        protected override LeaderboardScore CreateDrawableTopScore(ScoreInfo model) => new LeaderboardScore(model, model.Position, false)
        {
            Action = () => ScoreSelected?.Invoke(model)
        };
    }
}
