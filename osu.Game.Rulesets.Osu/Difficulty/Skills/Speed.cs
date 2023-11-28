// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Skills;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    /// <summary>
    /// Represents the skill required to press keys with regards to keeping up with the speed at which objects need to be hit.
    /// </summary>
    public class Speed : AveragedDecaySkill
    {
        protected override double SkillMultiplier => 1375;

        protected override double StrainDecayBase => 0.9;

        protected override double DiffMultiplicative => 0.6;

        protected override double CountFactor => 0.5;

        protected override double CountDecay => 2;

        private double currentStrain;
        private double currentRhythm;

        private readonly List<double> objectStrains = new List<double>();

        public Speed(Mod[] mods)
            : base(mods)
        {
        }

        protected override double StrainValueAt(DifficultyHitObject current)
        {
            currentStrain *= StrainDecayBase;
            currentStrain += SkillMultiplier * StrainValueOf(current);

            currentRhythm = RhythmEvaluator.EvaluateDifficultyOf(current);

            double totalStrain = currentStrain * currentRhythm;
            return totalStrain;
        }

        public double RelevantNoteCount()
        {
            if (objectStrains.Count == 0)
                return 0;

            double maxStrain = objectStrains.Max();

            if (maxStrain == 0)
                return 0;

            return objectStrains.Sum(strain => 1.0 / (1.0 + Math.Exp(-(strain / maxStrain * 12.0 - 6.0))));
        }

        protected override double StrainValueOf(DifficultyHitObject current) => SpeedEvaluator.EvaluateDifficultyOf(current);
    }
}
