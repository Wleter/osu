// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Rulesets.Difficulty.Skills
{
    public class SkillConnection
    {
        private AveragedSkill source;
        private AveragedSkill target;
        private double weightConnection;

        public SkillConnection(AveragedSkill source, AveragedSkill target, double weightConnection)
        {
            this.source = source;
            this.target = target;
            this.weightConnection = weightConnection;
        }

        public void ConnectSkill()
        {
            target.OtherSkillsContribution += source.DifficultyValue() * weightConnection;
        }
    }
}
