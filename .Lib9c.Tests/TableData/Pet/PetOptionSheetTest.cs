namespace Lib9c.Tests.TableData.Pet
{
    using Nekoyume.Model.Pet;
    using Nekoyume.TableData.Pet;
    using Xunit;

    public class PetOptionSheetTest
    {
        [Fact]
        public void Set()
        {
            const string content = @"pet_id,_pet_name,pet_level,option_type,option_value
1,D:CC 블랙캣,1,ReduceRequiredBlockByPercent,0
1,D:CC 블랙캣,2,ReduceRequiredBlockByValue,0.5
1,D:CC 블랙캣,3,IncreaseGreatSuccessRateByPercent,1
1,D:CC 블랙캣,4,IncreaseGreatSuccessRateByValue,1.5
1,D:CC 블랙캣,5,ReduceHourglassBlockByValue,2.0
1,D:CC 블랙캣,6,ReduceMaterialPurchaseCrystalCostByRate,2.5";

            var sheet = new PetOptionSheet();
            sheet.Set(content);

            Assert.Single(sheet);
            Assert.NotNull(sheet.First);
            var row = sheet.First;
            Assert.Equal(1, row.PetId);
            Assert.Equal(6, row.LevelOptionMap.Count);
            Assert.NotNull(row.LevelOptionMap[1]);
            var levelOption = row.LevelOptionMap[1];
            Assert.Equal(
                PetOptionType.ReduceRequiredBlockByPercent,
                levelOption.OptionType);
            Assert.Equal(0M, levelOption.OptionValue);
            Assert.NotNull(row.LevelOptionMap[2]);
            levelOption = row.LevelOptionMap[2];
            Assert.Equal(
                PetOptionType.ReduceRequiredBlockByValue,
                levelOption.OptionType);
            Assert.Equal(0.5M, levelOption.OptionValue);
            Assert.NotNull(row.LevelOptionMap[3]);
            levelOption = row.LevelOptionMap[3];
            Assert.Equal(
                PetOptionType.IncreaseGreatSuccessRateByPercent,
                levelOption.OptionType);
            Assert.Equal(1M, levelOption.OptionValue);
            Assert.NotNull(row.LevelOptionMap[4]);
            levelOption = row.LevelOptionMap[4];
            Assert.Equal(
                PetOptionType.IncreaseGreatSuccessRateByValue,
                levelOption.OptionType);
            Assert.Equal(1.5M, levelOption.OptionValue);
            Assert.NotNull(row.LevelOptionMap[5]);
            levelOption = row.LevelOptionMap[5];
            Assert.Equal(
                PetOptionType.ReduceHourglassBlockByValue,
                levelOption.OptionType);
            Assert.Equal(2.0M, levelOption.OptionValue);
            Assert.NotNull(row.LevelOptionMap[6]);
            levelOption = row.LevelOptionMap[6];
            Assert.Equal(
                PetOptionType.ReduceMaterialPurchaseCrystalCostByRate,
                levelOption.OptionType);
            Assert.Equal(2.5M, levelOption.OptionValue);
        }
    }
}
