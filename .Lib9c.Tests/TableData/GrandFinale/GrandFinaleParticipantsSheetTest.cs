namespace Lib9c.Tests.TableData.GrandFinale
{
    using System;
    using Libplanet;
    using Nekoyume.TableData.GrandFinale;
    using Xunit;

    public class GrandFinaleParticipantsSheetTest
    {
        public GrandFinaleParticipantsSheetTest()
        {
            if (!TableSheetsImporter.TryGetCsv(nameof(GrandFinaleParticipantsSheet), out _))
            {
                throw new Exception($"Not found sheet: {nameof(GrandFinaleParticipantsSheet)}");
            }
        }

        [Fact]
        public void SetToSheet()
        {
            const string tableContent = @"id,avatar_address
1,0xCF0C9e8885C6dF0fD468917302B646Ce098A6C84
1,0xd09536b122CCd262B6DFCD0b8D4AECf73a3669Cd
1,0x216696296B69a5402Aa7F3a3f05A6C31E44C90B9
1,0x9DD5334c26Fb04B6aDe7CD1047a3A39460B005e1
2,0xCF0C9e8885C6dF0fD468917302B646Ce098A6C84
2,0xd09536b122CCd262B6DFCD0b8D4AECf73a3669Cd
2,0x216696296B69a5402Aa7F3a3f05A6C31E44C90B9
2,0x9DD5334c26Fb04B6aDe7CD1047a3A39460B005e1";
            var sheet = new GrandFinaleParticipantsSheet();
            sheet.Set(tableContent);
            Assert.NotNull(sheet.First);
            Assert.Equal(1, sheet.First.GrandFinaleId);
            Assert.Equal(4, sheet.First.Participants.Count);

            var participants = sheet.First.Participants;
            Assert.Equal(new Address("0xCF0C9e8885C6dF0fD468917302B646Ce098A6C84"), participants[0]);
            Assert.Equal(new Address("0xd09536b122CCd262B6DFCD0b8D4AECf73a3669Cd"), participants[1]);
            Assert.Equal(new Address("0x216696296B69a5402Aa7F3a3f05A6C31E44C90B9"), participants[2]);
            Assert.Equal(new Address("0x9DD5334c26Fb04B6aDe7CD1047a3A39460B005e1"), participants[3]);

            var row2 = sheet[2];
            Assert.NotNull(row2);
            Assert.Equal(2, row2.GrandFinaleId);
            Assert.Equal(4, row2.Participants.Count);

            participants = row2.Participants;
            Assert.Equal(new Address("0xCF0C9e8885C6dF0fD468917302B646Ce098A6C84"), participants[0]);
            Assert.Equal(new Address("0xd09536b122CCd262B6DFCD0b8D4AECf73a3669Cd"), participants[1]);
            Assert.Equal(new Address("0x216696296B69a5402Aa7F3a3f05A6C31E44C90B9"), participants[2]);
            Assert.Equal(new Address("0x9DD5334c26Fb04B6aDe7CD1047a3A39460B005e1"), participants[3]);
        }
    }
}
