// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.Leaderboards;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Scoring;
using Realms;

namespace osu.Game.Screens.Select.Leaderboards
{
    public record BeatmapScoresCriteria
    {
        public required BeatmapInfo BeatmapInfo;

        public required RulesetInfo Ruleset;

        public required IReadOnlyList<Mod>? RequestMods;
    }

    public abstract partial class BeatmapLeaderboardProvider : Component
    {
        public abstract bool RequireSupporter(bool filterMods);

        public Action<LeaderboardScores<ScoreInfo>>? OnScoresFetched;

        public abstract APIRequest? FetchScores(BeatmapScoresCriteria criteria, CancellationToken token);

        public static BeatmapLeaderboardProvider GetProviderForScope(BeatmapLeaderboardScope scope)
        {
            switch (scope)
            {
                case BeatmapLeaderboardScope.Local:
                    return new LocalBeatmapLeaderboardProvider();
                case BeatmapLeaderboardScope.Global:
                    return new GlobalBeatmapLeaderboardProvider();
                case BeatmapLeaderboardScope.Country:
                    return new CountryBeatmapLeaderboardProvider();
                case BeatmapLeaderboardScope.Friend:
                    return new FriendBeatmapLeaderboardProvider();
                default:
                    throw new ArgumentException();
            }
        }
    }

    public partial class LocalBeatmapLeaderboardProvider : BeatmapLeaderboardProvider, IDisposable
    {
        [Resolved]
        private RealmAccess realm { get; set; } = null!;
        private IDisposable? scoreSubscription;

        public override bool RequireSupporter(bool filterMods) => false;

        public override APIRequest? FetchScores(BeatmapScoresCriteria criteria, CancellationToken token)
        {
            Debug.Assert(criteria.BeatmapInfo != null);

            scoreSubscription?.Dispose();
            scoreSubscription = null;

            scoreSubscription = realm.RegisterForNotifications(r =>
                r.All<ScoreInfo>().Filter($"{nameof(ScoreInfo.BeatmapInfo)}.{nameof(BeatmapInfo.ID)} == $0"
                                          + $" AND {nameof(ScoreInfo.BeatmapInfo)}.{nameof(BeatmapInfo.Hash)} == {nameof(ScoreInfo.BeatmapHash)}"
                                          + $" AND {nameof(ScoreInfo.Ruleset)}.{nameof(RulesetInfo.ShortName)} == $1"
                                          + $" AND {nameof(ScoreInfo.DeletePending)} == false"
                    , criteria.BeatmapInfo.ID, criteria.Ruleset.ShortName), localScoresChanged);

            void localScoresChanged(IRealmCollection<ScoreInfo> sender, ChangeSet? changes)
            {
                if (token.IsCancellationRequested)
                    return;

                // This subscription may fire from changes to linked beatmaps, which we don't care about.
                // It's currently not possible for a score to be modified after insertion, so we can safely ignore callbacks with only modifications.
                if (changes?.HasCollectionChanges() == false)
                    return;

                var scores = sender.AsEnumerable();

                if (criteria.RequestMods == new Mod[] { new ModNoMod() })
                {
                    // we need to filter out all scores that have any mods to get all local nomod scores
                    scores = scores.Where(s => !s.Mods.Any());
                }
                else if (criteria.RequestMods != null)
                {
                    // otherwise find all the scores that have all of the currently selected mods (similar to how web applies mod filters)
                    // we're creating and using a string HashSet representation of selected mods so that it can be translated into the DB query itself
                    var selectedMods = criteria.RequestMods.Select(m => m.Acronym).ToHashSet();

                    scores = scores.Where(s => selectedMods.SetEquals(s.Mods.Select(m => m.Acronym)));
                }

                scores = scores.Detach()
                    .OrderByTotalScore();

                OnScoresFetched?.Invoke(new LeaderboardScores<ScoreInfo>
                {
                    Scores = scores.ToList(),
                });
            }

            return null;
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            scoreSubscription?.Dispose();
        }
    }

    public abstract partial class OnlineBeatmapLeaderboardProvider : BeatmapLeaderboardProvider
    {
        [Resolved]
        private RulesetStore rulesets { get; set; } = null!;

        private GetScoresRequest? scoreRetrievalRequest;

        protected abstract BeatmapLeaderboardScope Scope { get; }

        public override APIRequest? FetchScores(BeatmapScoresCriteria criteria, CancellationToken token)
        {
            scoreRetrievalRequest?.Cancel();
            scoreRetrievalRequest = null;

            var fetchBeatmapInfo = criteria.BeatmapInfo;
            Debug.Assert(fetchBeatmapInfo != null);

            var fetchRuleset = criteria.Ruleset ?? fetchBeatmapInfo.Ruleset;

            var newRequest = new GetScoresRequest(fetchBeatmapInfo, fetchRuleset, Scope, criteria.RequestMods);
            newRequest.Success += response => Schedule(() =>
            {
                // Request may have changed since fetch request.
                // Can't rely on request cancellation due to Schedule inside SetScores so let's play it safe.
                if (!newRequest.Equals(scoreRetrievalRequest))
                    return;

                var scores = response.Scores.Select(s => s.ToScoreInfo(rulesets, fetchBeatmapInfo))
                    .OrderByTotalScore()
                    .ToList();

                OnScoresFetched?.Invoke(new LeaderboardScores<ScoreInfo>
                {
                    Scores = scores,
                    UserScore = response.UserScore?.CreateScoreInfo(rulesets, fetchBeatmapInfo),
                });
            });

            return scoreRetrievalRequest = newRequest;
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            scoreRetrievalRequest?.Cancel();
        }
    }

    public partial class GlobalBeatmapLeaderboardProvider : OnlineBeatmapLeaderboardProvider
    {
        public override bool RequireSupporter(bool filterMods) => filterMods;

        protected override BeatmapLeaderboardScope Scope => BeatmapLeaderboardScope.Global;
    }

    public partial class CountryBeatmapLeaderboardProvider : OnlineBeatmapLeaderboardProvider
    {
        public override bool RequireSupporter(bool filterMods) => true;

        protected override BeatmapLeaderboardScope Scope => BeatmapLeaderboardScope.Country;
    }

    public partial class FriendBeatmapLeaderboardProvider : OnlineBeatmapLeaderboardProvider
    {
        public override bool RequireSupporter(bool filterMods) => true;

        protected override BeatmapLeaderboardScope Scope => BeatmapLeaderboardScope.Friend;
    }
}
