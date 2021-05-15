using System.Threading.Tasks;

namespace Ledybot
{
    public static class GTSCharacterSprites
    {
        private static uint MyStatus = 0x33012818;
        private static uint SAVCharacterSpriteStart => MyStatus + 0x54;
        private static uint SavCharacterSpriteSize = 0x24;

        private static uint EntryStartOffset = 0x32992690;
        private static uint GtsEntryAmount = 0x329921A4;

        private static uint CharacterSpriteStart = 0x6C;
        private static uint CharacterSpriteSize = 0x100;

        /// <summary>
        /// Pre-Dumped Good Character Sprite
        /// </summary>
        public static byte[] dummyCharacterSpriteDesign = {
           0x0C, 0x04, 0x00, 0x0D, 0x00, 0x00, 0x48, 0x02, 0xFA, 0x7C, 0x11, 0x00,
           0xA8, 0x2C, 0x06, 0x00, 0x04, 0x15, 0x02, 0x00, 0x00, 0x00, 0x00, 0x40,
           0x00, 0x00, 0xEB, 0x42, 0x36, 0xFA, 0x0E, 0x3D, 0x62, 0xA8, 0x3B, 0xC0,
            };

        /// <summary>
        /// Dumps your Current Character Sprite on your Trainer Card
        /// </summary>
        /// <returns></returns>
        public static async Task<byte[]> DumpCharacterSpriteFromSave()
        {
            await Program.helper.waitNTRread(SAVCharacterSpriteStart, SavCharacterSpriteSize);
            return Program.helper.lastArray;
        }
        /// <summary>
        /// Get the Entry Point of the First GTS Entry
        /// </summary>
        /// <returns>Offset from Entry 1</returns>
        private static async Task<uint> GetFirstCharacterSpriteOffset()
        {
            await Program.helper.waitNTRread(EntryStartOffset);
            return (Program.helper.lastRead + CharacterSpriteStart);
        }

        private static async Task<int> GetPageSize()
        {
            await Program.helper.waitNTRread(GtsEntryAmount);
            return (int)Program.helper.lastRead;
        }

        /// <summary>
        /// Overwrites the first two Character sprites in the GTS List to avoid Game Crashes or Freezes if they were manipulated.
        /// </summary>
        /// <returns></returns>
        public static async Task OverwriteCharacterSprites(bool selfDump = false)
        {
            var dummy = dummyCharacterSpriteDesign;
            if (selfDump)
                dummy = await DumpCharacterSpriteFromSave();

            var first = await GetFirstCharacterSpriteOffset();
            var second = first + CharacterSpriteSize;
            for (int i = 0; i < 5; i++)
            {
              Program.helper.waitNTRwrite(first, dummy, Program.helper.pid);
              Program.helper.waitNTRwrite(second, dummy, Program.helper.pid);
            }
        }

        /// <summary>
        /// Untested Method, also not needed since Bot only sees the first two Sprites and jumps via RAM to the right entry.
        /// Overwrites all Character Sprites on the GTS List with a Dummy Sprite to avoid Crashes or Freezes if they were manipulated
        /// </summary>
        /// <returns></returns>
        public static async Task OverwriteAllCharacterSprites()
        {
            var pageSize = await GetPageSize();
            var first = await GetFirstCharacterSpriteOffset();
            for (int i = 0; i < 5; i++)
                for (int index = 0; index < pageSize; index++)
                    Program.helper.waitNTRwrite((first + (uint)(index * 0x100)), dummyCharacterSpriteDesign, Program.helper.pid);
        }

    }
}
