// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    /// <summary>
    /// Represents the skill required to correctly aim at every object in the map with a uniform CircleSize and normalized distances.
    /// </summary>
    public class Aim : AveragedDecaySkill
    {
        protected override double SkillMultiplier => 23.55;

        protected override double StrainDecayBase => 0.8;

        protected override double DiffMultiplicative => 0.6;

        protected override double CountFactor => 0.5;

        protected override double CountDecay => 2;

        private readonly bool withSliders;

        public Aim(Mod[] mods, bool withSliders)
            : base(mods)
        {
            this.withSliders = withSliders;
        }

        protected override double StrainValueOf(DifficultyHitObject current) => AimEvaluator.EvaluateDifficultyOf(current, withSliders);
    }
}
