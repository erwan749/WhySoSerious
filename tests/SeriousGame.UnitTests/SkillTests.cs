using Server.Domain;
using Server.Domain.Enums;

namespace SeriousGame.UnitTests;

public class SkillTests
{
    [Fact]
    public void LevelUp_AdvancesOneStep()
    {
        var skill = new Skill { Id = 1, Name = "C#" };
        var consultantSkill = new ConsultantSkill{ Skill = skill };

        consultantSkill.LevelUp();

        Assert.Equal(Level.Basic, consultantSkill.Level);
    }

    [Fact]
    public void LevelUp_AtExpert_StaysAtExpert()
    {
        var skill = new Skill { Id = 1, Name = "C#" };
        var consultantSkill = new ConsultantSkill{ Skill = skill };
        for (var i = 0; i < 10; i++) consultantSkill.LevelUp();

        Assert.Equal(Level.Expert, consultantSkill.Level);
    }
}
