// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using osu.Framework.Allocation;
using osu.Game.Online.API;
using osu.Game.Online.Leaderboards;
using osu.Game.Online.Rooms;

namespace osu.Game.Screens.OnlinePlay.Multiplayer
{
    public partial class MultiplayerLeaderboardScoresProvider : LeaderboardScoresProvider<MultiplayerLeaderboardScope, MultiplayerScore>
    {
        private readonly long roomId;
        private readonly PlaylistItem playlistItem;

        public MultiplayerScores? HigherScores { get; private set; }
        public MultiplayerScores? LowerScores { get; private set; }

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        public MultiplayerLeaderboardScoresProvider(long roomId, PlaylistItem playlistItem)
        {
            this.roomId = roomId;
            this.playlistItem = playlistItem;
        }

        public override bool IsOnlineScope => true;

        protected override APIRequest? FetchScores(CancellationToken cancellationToken)
        {
            // This performs two requests:
            // 1. A request to show the relevant score (and scores around).
            // 2. If that fails, a request to index the room starting from the highest score.

            var userScoreReq = createScoreRequest(Scope);

            userScoreReq.Success += userScore =>
            {
                var allScores = new List<MultiplayerScore> { userScore };

                // Other scores could have arrived between score submission and entering the results screen. Ensure the local player score position is up to date.
                if (UserScore != null)
                    UserScore.Position = userScore.Position;

                if (userScore.ScoresAround?.Higher != null)
                {
                    allScores.AddRange(userScore.ScoresAround.Higher.Scores);
                    HigherScores = userScore.ScoresAround.Higher;

                    Debug.Assert(userScore.Position != null);
                    setPositions(HigherScores, userScore.Position.Value, -1);
                }

                if (userScore.ScoresAround?.Lower != null)
                {
                    allScores.AddRange(userScore.ScoresAround.Lower.Scores);
                    LowerScores = userScore.ScoresAround.Lower;

                    Debug.Assert(userScore.Position != null);
                    setPositions(LowerScores, userScore.Position.Value, 1);
                }

                SetScores(allScores);
            };

            // On failure, fallback to a normal index.
            userScoreReq.Failure += _ => api.Queue(createIndexRequest());

            return userScoreReq;
        }

        public APIRequest? FetchNextPage(int direction)
        {
            Debug.Assert(direction == 1 || direction == -1);

            MultiplayerScores? pivot = direction == -1 ? HigherScores : LowerScores;

            if (pivot?.Cursor == null)
                return null;

            return createIndexRequest(pivot);
        }

        private APIRequest<MultiplayerScore> createScoreRequest(MultiplayerLeaderboardScope scope)
        {
            switch (scope)
            {
                case MultiplayerLeaderboardScope.PlaylistItemUserScope:
                    return new ShowPlaylistUserScoreRequest(roomId, playlistItem.ID, api.LocalUser.Value.Id);
                case MultiplayerLeaderboardScope.PlaylistItemScope:
                    return new ShowPlaylistScoreRequest(roomId, playlistItem.ID, api.LocalUser.Value.Id);
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Applies positions to all <see cref="MultiplayerScore"/>s referenced to a given pivot.
        /// </summary>
        /// <param name="scores">The <see cref="MultiplayerScores"/> to set positions on.</param>
        /// <param name="pivot">The pivot.</param>
        /// <param name="increment">The amount to increment the pivot position by for each <see cref="MultiplayerScore"/> in <paramref name="scores"/>.</param>
        private void setPositions(MultiplayerScores scores, MultiplayerScores? pivot, int increment)
            => setPositions(scores, pivot?.Scores[^1].Position ?? 0, increment);

        /// <summary>
        /// Applies positions to all <see cref="MultiplayerScore"/>s referenced to a given pivot.
        /// </summary>
        /// <param name="scores">The <see cref="MultiplayerScores"/> to set positions on.</param>
        /// <param name="pivotPosition">The pivot position.</param>
        /// <param name="increment">The amount to increment the pivot position by for each <see cref="MultiplayerScore"/> in <paramref name="scores"/>.</param>
        private void setPositions(MultiplayerScores scores, int pivotPosition, int increment)
        {
            foreach (var s in scores.Scores)
            {
                pivotPosition += increment;
                s.Position = pivotPosition;
            }
        }

        /// <summary>
        /// Creates a <see cref="IndexPlaylistScoresRequest"/> with an optional score pivot.
        /// </summary>
        /// <remarks>Does not queue the request.</remarks>
        /// <param name="pivot">An optional score pivot to retrieve scores around. Can be null to retrieve scores from the highest score.</param>
        /// <returns>The indexing <see cref="APIRequest"/>.</returns>
        private APIRequest createIndexRequest(MultiplayerScores? pivot = null)
        {
            var indexReq = pivot != null
                ? new IndexPlaylistScoresRequest(roomId, playlistItem.ID, pivot.Cursor, pivot.Params)
                : new IndexPlaylistScoresRequest(roomId, playlistItem.ID);

            indexReq.Success += r =>
            {
                if (pivot == LowerScores)
                {
                    LowerScores = r;
                    setPositions(r, pivot, 1);
                }
                else
                {
                    HigherScores = r;
                    setPositions(r, pivot, -1);
                }

                SetScores(r.Scores);
            };

            indexReq.Failure += _ => SetState(LeaderboardState.NoScores); // todo!

            return indexReq;
        }
    }

    public enum MultiplayerLeaderboardScope
    {
        PlaylistItemUserScope,
        PlaylistItemScope,
    }
}
