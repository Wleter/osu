// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Game.Storyboards;
using osu.Game.Storyboards.Drawables;

namespace osu.Game.Graphics.Containers
{
    /// <summary>
    /// A container that handles <see cref="Storyboard"/> loading, as well as applies user-specified visual settings to it.
    /// </summary>
    public class DimmableStoryboardContainer : UserDimContainer
    {
        private readonly Storyboard storyboard;
        private DrawableStoryboard drawableStoryboard;

        public DimmableStoryboardContainer(Storyboard storyboard)
        {
            this.storyboard = storyboard;

            // Storyboards current do not get used in scenarios without user dim, so default to enabled here.
            EnableUserDim.Default = true;
            EnableUserDim.Value = true;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            initializeStoryboard(false);
        }

        protected override void LoadComplete()
        {
            ShowStoryboard.BindValueChanged(_ => initializeStoryboard(true), true);
            base.LoadComplete();
        }

        protected override bool ShowDimContent => ShowStoryboard.Value && UserDimLevel.Value < 1;

        private void initializeStoryboard(bool async)
        {
            if (drawableStoryboard != null)
                return;

            if (!ShowStoryboard.Value)
                return;

            drawableStoryboard = storyboard.CreateDrawable();
            drawableStoryboard.Masking = true;

            if (async)
                LoadComponentAsync(drawableStoryboard, Add);
            else
                Add(drawableStoryboard);
        }
    }
}
