// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators;
using osu.Game.Rulesets.Osu.Mods;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    /// <summary>
    /// Represents the skill required to memorise and hit every object in a map with the Flashlight mod enabled.
    /// </summary>
    public class Flashlight : AveragedDecaySkill
    {
        protected override double SkillMultiplier => 0.052;

        protected override double StrainDecayBase => 0.8;

        protected override double DiffMultiplicative => 0.6;

        protected override double CountFactor => 0.5;

        protected override double CountDecay => 2;

        private readonly bool hasHiddenMod;

        public Flashlight(Mod[] mods)
            : base(mods)
        {
            hasHiddenMod = mods.Any(m => m is OsuModHidden);
        }

        protected override double StrainValueOf(DifficultyHitObject current) => FlashlightEvaluator.EvaluateDifficultyOf(current, hasHiddenMod);
    }
}
