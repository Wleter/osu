// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Framework.Localisation;
using osu.Framework.Utils;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.UI;
using osuTK;
using osu.Framework.Input.StateChanges;
using osu.Framework.Input;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Game.Beatmaps;
using osuTK.Graphics;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Rulesets.Osu.UI;

namespace osu.Game.Rulesets.Osu.Mods
{
    public class OsuModFPS : Mod, IUpdatableByPlayfield, IApplicableToDrawableRuleset<OsuHitObject>
    {
        public override string Name => "FPS";
        public override string Acronym => "FPS";
        public override ModType Type => ModType.Conversion;
        public override LocalisableString Description => "Make everything 3D.";
        public override double ScoreMultiplier => 1;
        private PassThroughInputManager inputManager = null!;
        

        [SettingSource("Maximum distance", "How far can objects be.", 0)]
        public BindableFloat MaximumDistance { get; } = new BindableFloat(5f)
        {
            Precision = 1f,
            MinValue = 1f,
            MaxValue = 20f,
        };
        

        private Circle cursor = null!;
        public void ApplyToDrawableRuleset(DrawableRuleset<OsuHitObject> drawableRuleset)
        {
            drawableRuleset.Overlays.Add(cursor = new Circle()
            {
                Colour = Color4.Red,
                Size = new Vector2(20),
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            });
            inputManager = ((DrawableOsuRuleset)drawableRuleset).KeyBindingInputManager;
        }

        public void Update(Playfield playfield)
        {
            Vector2 mousePosition = inputManager.CurrentState.Mouse.Position;
            Vector2 positionAdjust = playfield.OriginPosition - playfield.Parent.ToLocalSpace(mousePosition);

            new MousePositionAbsoluteInput { Position = playfield.ToScreenSpace(playfield.OriginPosition - positionAdjust) }.Apply(inputManager.CurrentState, inputManager);
            playfield.Cursor.Alpha = 0;
            playfield.Position = positionAdjust;

            playfield.Position = new Vector2(
                    (float)Interpolation.DampContinuously(playfield.Position.X, positionAdjust.X, 2, Math.Abs(playfield.Clock.ElapsedFrameTime)),
                    (float)Interpolation.DampContinuously(playfield.Position.Y, positionAdjust.Y, 2, Math.Abs(playfield.Clock.ElapsedFrameTime))
                );
        }
    }
}