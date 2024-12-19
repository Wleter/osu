// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics.UserInterface;
using osu.Game.Online.API;
using osu.Game.Online.Rooms;
using osu.Game.Rulesets;
using osu.Game.Scoring;
using osu.Game.Screens.OnlinePlay.Multiplayer;
using osu.Game.Screens.Ranking;

namespace osu.Game.Screens.OnlinePlay.Playlists
{
    public abstract partial class PlaylistItemResultsScreen : ResultsScreen
    {
        protected readonly long RoomId;
        protected readonly PlaylistItem PlaylistItem;

        protected LoadingSpinner LeftSpinner { get; private set; } = null!;
        protected LoadingSpinner CentreSpinner { get; private set; } = null!;
        protected LoadingSpinner RightSpinner { get; private set; } = null!;

        [Resolved]
        protected IAPIProvider API { get; set; } = null!;

        [Resolved]
        protected ScoreManager ScoreManager { get; private set; } = null!;

        [Resolved]
        protected RulesetStore Rulesets { get; private set; } = null!;

        public MultiplayerLeaderboardScoresProvider ScoresProvider { get; init; } = null!;

        protected PlaylistItemResultsScreen(ScoreInfo? score, long roomId, PlaylistItem playlistItem)
            : base(score)
        {
            RoomId = roomId;
            PlaylistItem = playlistItem;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            AddInternal(new Container
            {
                RelativeSizeAxes = Axes.Both,
                Padding = new MarginPadding { Bottom = TwoLayerButton.SIZE_EXTENDED.Y },
                Children = new Drawable[]
                {
                    LeftSpinner = new PanelListLoadingSpinner(ScorePanelList)
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.Centre,
                    },
                    CentreSpinner = new PanelListLoadingSpinner(ScorePanelList)
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        State = { Value = Score == null ? Visibility.Visible : Visibility.Hidden },
                    },
                    RightSpinner = new PanelListLoadingSpinner(ScorePanelList)
                    {
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.Centre,
                    },
                }
            });
        }

        protected override void Update()
        {
            base.Update();

            if (LastFetchCompleted)
            {
                APIRequest? nextPageRequest = null;

                if (ScorePanelList.IsScrolledToStart)
                    nextPageRequest = ScoresProvider.FetchNextPage(-1);
                else if (ScorePanelList.IsScrolledToEnd)
                    nextPageRequest = ScoresProvider.FetchNextPage(1);

                if (nextPageRequest != null)
                {
                    LastFetchCompleted = false;
                    API.Queue(nextPageRequest);
                }
            }
        }

        /// <summary>
        /// Transforms returned <see cref="MultiplayerScores"/> into <see cref="ScoreInfo"/>s, ensure the <see cref="ScorePanelList"/> is put into a sane state, and invokes a given success callback.
        /// </summary>
        /// <param name="callback">The callback to invoke with the final <see cref="ScoreInfo"/>s.</param>
        /// <param name="scores">The <see cref="MultiplayerScore"/>s that were retrieved from <see cref="APIRequest"/>s.</param>
        /// <param name="pivot">An optional pivot around which the scores were retrieved.</param>
        protected virtual ScoreInfo[] PerformSuccessCallback(Action<IEnumerable<ScoreInfo>> callback, List<MultiplayerScore> scores, MultiplayerScores? pivot = null)
        {
            var scoreInfos = scores.Select(s => s.CreateScoreInfo(ScoreManager, Rulesets, PlaylistItem, Beatmap.Value.BeatmapInfo)).OrderByTotalScore().ToArray();

            // Invoke callback to add the scores.
            callback.Invoke(scoreInfos);

            return scoreInfos;
        }

        private void hideLoadingSpinners(MultiplayerScores? pivot = null)
        {
            CentreSpinner.Hide();

            if (pivot == ScoresProvider.LowerScores)
                RightSpinner.Hide();
            else if (pivot == ScoresProvider.HigherScores)
                LeftSpinner.Hide();
        }

        private partial class PanelListLoadingSpinner : LoadingSpinner
        {
            private readonly ScorePanelList list;

            /// <summary>
            /// Creates a new <see cref="PanelListLoadingSpinner"/>.
            /// </summary>
            /// <param name="list">The list to track.</param>
            /// <param name="withBox">Whether the spinner should have a surrounding black box for visibility.</param>
            public PanelListLoadingSpinner(ScorePanelList list, bool withBox = true)
                : base(withBox)
            {
                this.list = list;
            }

            protected override void Update()
            {
                base.Update();

                float panelOffset = list.DrawWidth / 2 - ScorePanel.EXPANDED_WIDTH;

                if ((Anchor & Anchor.x0) > 0)
                    X = (float)(panelOffset - list.Current);
                else if ((Anchor & Anchor.x2) > 0)
                    X = (float)(list.ScrollableExtent - list.Current - panelOffset);
            }
        }
    }
}
