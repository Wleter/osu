// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.ComponentModel;
using System.Threading;
using osu.Framework.Allocation;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Leaderboards;
using osu.Game.Online.Rooms;

namespace osu.Game.Screens.OnlinePlay.Match.Components
{
    public partial class MatchLeaderboard : Leaderboard<MatchLeaderboardScope, APIUserScoreAggregate>
    {
        private readonly Room room;

        [Resolved(CanBeNull = true)]
        private IAPIProvider? api { get; set; }

        public MatchLeaderboard(Room room)
        {
            this.room = room;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            room.PropertyChanged += onRoomPropertyChanged;
            fetchInitialScores();
        }

        private void onRoomPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Room.RoomID))
                fetchInitialScores();
        }

        private void fetchInitialScores()
        {
            if (room.RoomID == null)
                return;

            SetScores(null);
            RefetchScores();
        }

        protected override bool IsOnlineScope => true;

        protected override void FetchScores(CancellationToken cancellationToken)
        {
            if (room.RoomID == null)
                return;

            var req = new GetRoomLeaderboardRequest(room.RoomID.Value);

            req.Success += r => Schedule(() =>
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                SetScores(r.Leaderboard, r.UserScore);
            });

            req.Failure += e => Schedule(() =>
            {
                if (e is OperationCanceledException || cancellationToken.IsCancellationRequested)
                    return;

                SetErrorState(LeaderboardState.NetworkFailure);
            });

            api?.Queue(req);
        }

        protected override LeaderboardScore CreateDrawableScore(APIUserScoreAggregate model, int index) => new MatchLeaderboardScore(model, index);

        protected override LeaderboardScore CreateDrawableTopScore(APIUserScoreAggregate model) => new MatchLeaderboardScore(model, model.Position, false);

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            room.PropertyChanged -= onRoomPropertyChanged;
        }
    }

    public enum MatchLeaderboardScope
    {
        Overall
    }
}
